using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// マイクの声量を 0〜1 の Level に変換する。
/// 直近の音声サンプルの RMS → dB を求め、noiseFloorDb〜maxDb の範囲で正規化し、スムージングする。
///
/// あわせて声の「音色」を -1〜+1 の Timbre として求める（宇宙船の左右の操作に使う）。
///   高い帯域（いー・しー）と低い帯域（おー・うー）のエネルギー比（dB）を求め、
///   timbreCenterDb を中心に ±timbreRangeDb で -1〜+1 に変換する。
///   「おー」→ -1（左）、「あー」→ 0 付近（まっすぐ）、「いー」→ +1（右）。
///   声が小さいとき（timbreMinLevel 未満）は 0 に戻る。
///
/// keyboardFallback が有効なら（マイク無しでの開発用）
///   スペースキー：Level = 1、← / → キー：Timbre = -1 / +1 として扱う。
/// </summary>
public class MicInput : MonoBehaviour
{
    [SerializeField] MicDeviceSelector deviceSelector;
    [SerializeField] int sampleRate = 44100;
    [SerializeField] int sampleWindow = 1024;
    [Tooltip("これ以下の音量は Level 0 扱い。MicCalibration が周囲の騒音から上書きする。")]
    [SerializeField] float noiseFloorDb = -50f;
    [Tooltip("これ以上の音量は Level 1 扱い。大声を出したときの dB を目安に設定する。")]
    [SerializeField] float maxDb = -10f;
    [Tooltip("声が大きくなるときの追従の速さ（大きいほど速い）")]
    [SerializeField] float attack = 12f;
    [Tooltip("声が小さくなるときの追従の速さ（小さいほどゆっくり下がる）")]
    [SerializeField] float release = 4f;
    [SerializeField] bool keyboardFallback = true;

    [Header("音色（左右の操作）")]
    [Tooltip("低い帯域（Hz）。「おー」「うー」の成分が多い")]
    [SerializeField] Vector2 lowBandHz = new Vector2(150f, 1200f);
    [Tooltip("高い帯域（Hz）。「いー」「しー」の成分が多い")]
    [SerializeField] Vector2 highBandHz = new Vector2(1800f, 6000f);
    [Tooltip("「おー」と「いー」の中間の値（dB）。DebugPanel の I / O キーで実際の声に合わせられる")]
    [SerializeField] float timbreCenterDb = -17f;
    [Tooltip("中間からこの dB 離れると左右いっぱい（±1）になる")]
    [SerializeField] float timbreRangeDb = 7f;
    [Tooltip("声の大きさ（スムージング前）がこれ以上のときだけ音色を判定する")]
    [SerializeField, Range(0f, 1f)] float timbreMinLevel = 0.15f;
    [Tooltip("音色の変化への追従の速さ（大きいほど速い）")]
    [SerializeField] float timbreSmoothing = 8f;

    const float SilentDb = -80f;

    AudioClip clip;
    string device;
    float[] buffer;
    float[] fftRe, fftIm, window;

    /// <summary>スムージング後の声量（0〜1）。ゲーム側はこれを使う。</summary>
    public float Level { get; private set; }

    /// <summary>直近の音量（dB）。デバッグ表示やキャリブレーションに使う。</summary>
    public float CurrentDb { get; private set; } = SilentDb;

    public string DeviceName => device;
    public bool IsRecording => clip != null;

    public float NoiseFloorDb
    {
        get => noiseFloorDb;
        set => noiseFloorDb = Mathf.Min(value, maxDb - 1f);
    }

    public float MaxDb
    {
        get => maxDb;
        set => maxDb = Mathf.Max(value, noiseFloorDb + 1f);
    }

    /// <summary>スムージング後の音色（-1＝おー・左 〜 0＝あー・まっすぐ 〜 +1＝いー・右）。</summary>
    public float Timbre { get; private set; }

    /// <summary>直近の音色の生の値（高い帯域÷低い帯域のエネルギー比, dB）。声が出ていないときは NaN。</summary>
    public float TimbreDb { get; private set; } = float.NaN;

    /// <summary>今、音色を判定できるだけの声が出ているか。</summary>
    public bool IsVoiced { get; private set; }

    public float TimbreCenterDb
    {
        get => timbreCenterDb;
        set => timbreCenterDb = value;
    }

    public float TimbreRangeDb
    {
        get => timbreRangeDb;
        set => timbreRangeDb = Mathf.Max(1f, value);
    }

    void OnEnable()
    {
        if (deviceSelector != null) deviceSelector.DeviceChanged += StartRecording;
    }

    void OnDisable()
    {
        if (deviceSelector != null) deviceSelector.DeviceChanged -= StartRecording;
        StopRecording();
    }

    void Start()
    {
        buffer = new float[sampleWindow];

        // FFT は 2 のべき乗の長さが必要なので、sampleWindow 以下で最大の 2 のべき乗にする
        int fftSize = Mathf.ClosestPowerOfTwo(sampleWindow);
        if (fftSize > sampleWindow) fftSize /= 2;
        fftRe = new float[fftSize];
        fftIm = new float[fftSize];
        window = new float[fftSize];
        for (int i = 0; i < fftSize; i++) window[i] = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * i / (fftSize - 1));   // ハン窓

        StartRecording(deviceSelector != null ? deviceSelector.CurrentDevice : null);
    }

    void Update()
    {
        float target = 0f;
        float timbreTarget = 0f;
        IsVoiced = false;

        if (clip != null)
        {
            if (!Microphone.IsRecording(device))
            {
                Debug.LogWarning($"[MicInput] 録音が止まりました（マイクが抜けた可能性があります）: {device}");
                StopRecording();
            }
            else
            {
                CurrentDb = ReadDb();
                target = Mathf.InverseLerp(noiseFloorDb, maxDb, CurrentDb);

                // 音色は、今この瞬間に十分な声が出ているときだけ判定する（ReadDb で buffer に入った音を使う）
                if (target >= timbreMinLevel)
                {
                    IsVoiced = true;
                    TimbreDb = ComputeTimbreDb();
                    timbreTarget = Mathf.Clamp((TimbreDb - timbreCenterDb) / timbreRangeDb, -1f, 1f);
                }
                else
                {
                    TimbreDb = float.NaN;
                }
            }
        }

        if (keyboardFallback && Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.isPressed) target = 1f;
            if (Keyboard.current.leftArrowKey.isPressed) timbreTarget = -1f;
            if (Keyboard.current.rightArrowKey.isPressed) timbreTarget = 1f;
        }

        // 上がるときは速く、下がるときはゆっくり
        float speed = target > Level ? attack : release;
        Level = Mathf.Lerp(Level, target, 1f - Mathf.Exp(-speed * Time.deltaTime));
        Timbre = Mathf.Lerp(Timbre, timbreTarget, 1f - Mathf.Exp(-timbreSmoothing * Time.deltaTime));
    }

    /// <summary>buffer の末尾 fftSize 個の音から、高い帯域と低い帯域のエネルギー比（dB）を求める。</summary>
    float ComputeTimbreDb()
    {
        int n = fftRe.Length;
        int offset = buffer.Length - n;
        for (int i = 0; i < n; i++)
        {
            fftRe[i] = buffer[offset + i] * window[i];
            fftIm[i] = 0f;
        }
        Fft(fftRe, fftIm);

        float binHz = (float)clip.frequency / n;
        double low = BandEnergy(lowBandHz, binHz);
        double high = BandEnergy(highBandHz, binHz);
        return 10f * Mathf.Log10((float)((high + 1e-12) / (low + 1e-12)));
    }

    double BandEnergy(Vector2 bandHz, float binHz)
    {
        int from = Mathf.Max(1, Mathf.CeilToInt(bandHz.x / binHz));
        int to = Mathf.Min(fftRe.Length / 2 - 1, Mathf.FloorToInt(bandHz.y / binHz));
        double sum = 0;
        for (int k = from; k <= to; k++) sum += fftRe[k] * fftRe[k] + fftIm[k] * fftIm[k];
        return sum;
    }

    /// <summary>高速フーリエ変換（その場で計算する基数 2 の FFT）。re / im の長さは 2 のべき乗。</summary>
    static void Fft(float[] re, float[] im)
    {
        int n = re.Length;
        for (int i = 1, j = 0; i < n; i++)   // ビット反転の並べ替え
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i < j)
            {
                (re[i], re[j]) = (re[j], re[i]);
                (im[i], im[j]) = (im[j], im[i]);
            }
        }
        for (int len = 2; len <= n; len <<= 1)
        {
            float angle = -2f * Mathf.PI / len;
            float wRe = Mathf.Cos(angle), wIm = Mathf.Sin(angle);
            for (int i = 0; i < n; i += len)
            {
                float curRe = 1f, curIm = 0f;
                for (int k = 0; k < len / 2; k++)
                {
                    int a = i + k, b = i + k + len / 2;
                    float tRe = re[b] * curRe - im[b] * curIm;
                    float tIm = re[b] * curIm + im[b] * curRe;
                    re[b] = re[a] - tRe;
                    im[b] = im[a] - tIm;
                    re[a] += tRe;
                    im[a] += tIm;
                    float nextRe = curRe * wRe - curIm * wIm;
                    curIm = curRe * wIm + curIm * wRe;
                    curRe = nextRe;
                }
            }
        }
    }

    void StartRecording(string deviceName)
    {
        StopRecording();

        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[MicInput] マイクが見つかりません。" + (keyboardFallback ? "スペースキーで代用できます。" : ""));
            return;
        }

        device = string.IsNullOrEmpty(deviceName) ? Microphone.devices[0] : deviceName;

        int rate = sampleRate;
        Microphone.GetDeviceCaps(device, out int minRate, out int maxRate);
        if (maxRate > 0) rate = Mathf.Clamp(rate, minRate, maxRate);  // 0,0 は「どのレートでも可」

        clip = Microphone.Start(device, true, 1, rate);
        if (clip == null)
        {
            Debug.LogError($"[MicInput] 録音を開始できませんでした: {device}（Windows のマイクのプライバシー設定を確認）");
            return;
        }
        Debug.Log($"[MicInput] 録音開始: {device} ({rate} Hz)");
    }

    void StopRecording()
    {
        if (clip == null) return;
        Microphone.End(device);
        clip = null;
        CurrentDb = SilentDb;
    }

    float ReadDb()
    {
        // 録音位置の直前 sampleWindow 個を読む。ループ録音の先頭付近では末尾側から読む（GetData は折り返して読む）
        int position = Microphone.GetPosition(device) - sampleWindow;
        if (position < 0) position += clip.samples;
        if (!clip.GetData(buffer, position)) return CurrentDb;

        float sum = 0f;
        for (int i = 0; i < buffer.Length; i++) sum += buffer[i] * buffer[i];
        float rms = Mathf.Sqrt(sum / buffer.Length);
        return Mathf.Max(20f * Mathf.Log10(rms + 1e-7f), SilentDb);
    }
}
