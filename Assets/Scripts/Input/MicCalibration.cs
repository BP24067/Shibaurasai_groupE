using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 周囲の騒音を一定時間測り、MicInput の noiseFloorDb を「平均騒音 + marginDb」に設定する。
/// これで会場のざわつきだけでは宇宙船が加速しないようにする。
/// 測定中はプレイヤーに声を出さないよう案内すること（MessageUI などで）。
/// </summary>
public class MicCalibration : MonoBehaviour
{
    [SerializeField] MicInput mic;
    [SerializeField] float duration = 2f;
    [SerializeField] float marginDb = 6f;
    [Tooltip("録音開始直後の無音を測らないための待ち時間（秒）")]
    [SerializeField] float warmup = 0.3f;
    [Tooltip("静かすぎる環境でしきい値が下がりすぎないための下限")]
    [SerializeField] float minFloorDb = -70f;
    [Tooltip("GameManager ができるまでの動作確認用。GameManager から Ready 時に呼ぶようになったら外す。")]
    [SerializeField] bool calibrateOnStart = true;

    public bool IsCalibrating { get; private set; }

    /// <summary>測定の進み具合（0〜1）。</summary>
    public float Progress { get; private set; }

    /// <summary>直近の測定で得た周囲の平均音量（dB）。未測定なら NaN。</summary>
    public float LastAmbientDb { get; private set; } = float.NaN;

    /// <summary>測定が終わったときに通知する。</summary>
    public event Action Finished;

    void Start()
    {
        if (calibrateOnStart) StartCalibration();
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
        yield return new WaitForSeconds(warmup);

        // dB のまま平均すると静かな瞬間に引っ張られるので、パワー（エネルギー）で平均する
        double powerSum = 0;
        int count = 0;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            powerSum += Math.Pow(10.0, mic.CurrentDb / 10.0);
            count++;
            elapsed += Time.deltaTime;
            Progress = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        float ambientDb = count > 0 ? 10f * Mathf.Log10((float)(powerSum / count)) : mic.CurrentDb;
        LastAmbientDb = ambientDb;
        mic.NoiseFloorDb = Mathf.Max(ambientDb + marginDb, minFloorDb);

        Debug.Log($"[MicCalibration] 周囲 {ambientDb:F1} dB → しきい値 {mic.NoiseFloorDb:F1} dB（最大 {mic.MaxDb:F1} dB）");
        if (mic.MaxDb - mic.NoiseFloorDb < 10f)
        {
            Debug.LogWarning("[MicCalibration] 周囲がうるさく、声で加速できる幅が狭くなっています。マイクを口に近づけるか、MaxDb を上げてください。");
        }

        Progress = 1f;
        IsCalibrating = false;
        Finished?.Invoke();
    }
}
