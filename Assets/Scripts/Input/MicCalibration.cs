using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 周囲の騒音を一定時間測り、MicInput の noiseFloorDb（これ以下の音は Level 0）を設定する。
/// これで会場のざわつきだけでは宇宙船が加速しないようにする。
/// 測定中はプレイヤーに声を出さないよう案内すること（MessageUI などで）。
///
/// 測り方（話し声や効果音がまぎれ込んでも、しきい値が上がりすぎないようにしている）:
///   1. warmup 秒待つ（録音開始直後の無音や、Ready に戻すときに出した声の余韻を避ける）
///   2. duration 秒のあいだ、毎フレームの音量（dB）を集める
///   3. 平均ではなく中央値を「周囲の音量」とする（声などの大きな音が混じっても引っ張られない）
///   4. 中央値と下位 10% の差を「ゆれ」とし、しきい値 = 中央値 + max(marginDb, ゆれ × spreadFactor)
///      （ざわつきが大きい会場ほど、余裕を大きく取る）
///   5. 中央値より voiceDb 以上大きい音が maxVoiceRatio を超えていたら「声が入った」とみなし、測り直す
///      （最大 maxRetries 回。最後まで声が入ったときは、いちばん静かだった回の結果を使う）
/// </summary>
public class MicCalibration : MonoBehaviour
{
    [SerializeField] MicInput mic;
    [Tooltip("1 回の測定の長さ（秒）")]
    [SerializeField] float duration = 2f;
    [Tooltip("周囲の音量（中央値）にこの dB 以上の余裕を足してしきい値にする")]
    [SerializeField] float marginDb = 6f;
    [Tooltip("周囲の音のゆれ（中央値と下位 10% の差）に掛ける倍率。ゆれが大きいほどしきい値が上がる")]
    [SerializeField] float spreadFactor = 2f;
    [Tooltip("録音開始直後の無音や、Ready に戻すときに出した声の余韻を測らないための待ち時間（秒）")]
    [SerializeField] float warmup = 0.5f;
    [Tooltip("中央値よりこの dB 以上大きい音を「声が入った」とみなす")]
    [SerializeField] float voiceDb = 10f;
    [Tooltip("「声が入った」音の割合がこれを超えたら測り直す")]
    [SerializeField, Range(0f, 1f)] float maxVoiceRatio = 0.2f;
    [Tooltip("声が入ったときに測り直す最大回数")]
    [SerializeField] int maxRetries = 2;
    [Tooltip("静かすぎる環境でしきい値が下がりすぎないための下限")]
    [SerializeField] float minFloorDb = -70f;
    [Tooltip("GameManager ができるまでの動作確認用。GameManager から Ready 時に呼ぶようになったら外す。")]
    [SerializeField] bool calibrateOnStart = true;

    readonly List<float> samples = new List<float>();

    public bool IsCalibrating { get; private set; }

    /// <summary>測定の進み具合（0〜1）。測り直すときは 0 から始まる。</summary>
    public float Progress { get; private set; }

    /// <summary>直近の測定で得た周囲の音量（中央値, dB）。未測定なら NaN。</summary>
    public float LastAmbientDb { get; private set; } = float.NaN;

    /// <summary>直近の測定で得た周囲の音のゆれ（中央値と下位 10% の差, dB）。未測定なら NaN。</summary>
    public float LastSpreadDb { get; private set; } = float.NaN;

    /// <summary>直近の測定で何回測ったか（1 なら測り直しなし）。</summary>
    public int LastAttempts { get; private set; }

    /// <summary>測定が終わったときに通知する。</summary>
    public event Action Finished;

    void Start()
    {
        if (calibrateOnStart) StartCalibration();
    }

    void OnDisable()
    {
        // 測定中に無効になっても、GameManager が「測定中」のまま止まらないようにする
        IsCalibrating = false;
    }

    public void StartCalibration()
    {
        StopAllCoroutines();
        StartCoroutine(Calibrate());
    }

    IEnumerator Calibrate()
    {
        IsCalibrating = true;
        Progress = 0f;

        if (mic == null)
        {
            Debug.LogError("[MicCalibration] Mic が設定されていません。");
            IsCalibrating = false;
            yield break;
        }

        float chosenMedian = float.NaN, chosenSpread = 0f, chosenVoiceRatio = 0f;
        int attempts = 0;
        for (int attempt = 1; attempt <= maxRetries + 1; attempt++)
        {
            attempts = attempt;
            Progress = 0f;
            yield return new WaitForSecondsRealtime(warmup);

            samples.Clear();
            float elapsed = 0f;
            while (elapsed < duration)
            {
                samples.Add(mic.CurrentDb);
                elapsed += Time.unscaledDeltaTime;
                Progress = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            if (samples.Count == 0) break;

            samples.Sort();
            float median = Percentile(0.5f);
            float spread = median - Percentile(0.1f);
            float voiceRatio = RatioAbove(median + voiceDb);

            // 声が入っていなければこの回で決定。入っていたら、いちばん静かだった回を覚えて測り直す
            bool clean = voiceRatio <= maxVoiceRatio;
            if (clean || float.IsNaN(chosenMedian) || median < chosenMedian)
            {
                chosenMedian = median;
                chosenSpread = spread;
                chosenVoiceRatio = voiceRatio;
            }
            if (clean) break;

            Debug.Log($"[MicCalibration] 測定中に大きな音が入りました（{voiceRatio * 100f:F0}%）。" +
                      (attempt <= maxRetries ? "測り直します。" : "いちばん静かだった回の結果を使います。"));
        }

        float ambientDb = float.IsNaN(chosenMedian) ? mic.CurrentDb : chosenMedian;
        LastAmbientDb = ambientDb;
        LastSpreadDb = chosenSpread;
        LastAttempts = attempts;
        mic.NoiseFloorDb = Mathf.Max(ambientDb + Mathf.Max(marginDb, chosenSpread * spreadFactor), minFloorDb);

        Debug.Log($"[MicCalibration] 周囲 {ambientDb:F1} dB（ゆれ {chosenSpread:F1} dB, 大きな音 {chosenVoiceRatio * 100f:F0}%, {attempts} 回）" +
                  $" → しきい値 {mic.NoiseFloorDb:F1} dB（最大 {mic.MaxDb:F1} dB）");
        if (mic.MaxDb - mic.NoiseFloorDb < 10f)
        {
            Debug.LogWarning("[MicCalibration] 周囲がうるさく、声で加速できる幅が狭くなっています。マイクを口に近づけるか、MaxDb を上げてください。");
        }

        Progress = 1f;
        IsCalibrating = false;
        Finished?.Invoke();
    }

    // samples（昇順に並べ替え済み）の p（0〜1）の位置の値
    float Percentile(float p)
    {
        float index = p * (samples.Count - 1);
        int lower = Mathf.FloorToInt(index);
        int upper = Mathf.Min(lower + 1, samples.Count - 1);
        return Mathf.Lerp(samples[lower], samples[upper], index - lower);
    }

    // samples のうち threshold より大きい音の割合
    float RatioAbove(float threshold)
    {
        int count = 0;
        for (int i = samples.Count - 1; i >= 0 && samples[i] > threshold; i--) count++;
        return (float)count / samples.Count;
    }
}
