using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// マイクの声量を 0〜1 の Level に変換する。
/// 直近の音声サンプルの RMS → dB を求め、noiseFloorDb〜maxDb の範囲で正規化し、スムージングする。
/// keyboardFallback が有効なら、スペースキーを押している間は Level = 1 として扱う（マイク無しでの開発用）。
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

    const float SilentDb = -80f;

    AudioClip clip;
    string device;
    float[] buffer;

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
        StartRecording(deviceSelector != null ? deviceSelector.CurrentDevice : null);
    }

    void Update()
    {
        float target = 0f;

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
            }
        }

        if (keyboardFallback && Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
        {
            target = 1f;
        }

        // 上がるときは速く、下がるときはゆっくり
        float speed = target > Level ? attack : release;
        Level = Mathf.Lerp(Level, target, 1f - Mathf.Exp(-speed * Time.deltaTime));
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
