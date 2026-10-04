using System;
using UnityEngine;

/// <summary>
/// ゲームの進行を管理する。
///   Ready   : キャリブレーション → 声（Level が startThreshold 以上）を startHoldTime 秒続けるとスタート
///   Playing : 声の大きさに応じて速度を変え、進んだ距離が goalDistance に届いたらゴール
///   Goal    : 距離の加算を止める。Ready へ戻すのは ResultUI / IdleResetter が ResetToReady() を呼ぶ
///
/// 他のスクリプトへの通知:
///   OnStateChanged … 状態が変わったとき（MessageUI, AudioManager, WarpGate, WarpSequence, ResultUI など）
///   OnHit          … 隕石に当たったとき（CameraShake, HitEffect, AudioManager など）
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] MicInput mic;
    [SerializeField] MicCalibration calibration;

    [Header("速度")]
    [Tooltip("声を出していないときの速度（プレイ中に止まらないように 0 より大きくする）")]
    [SerializeField] float minSpeed = 5f;
    [SerializeField] float maxSpeed = 40f;
    [Tooltip("1秒あたりに変化できる速度の量（大きいほど反応が速い）")]
    [SerializeField] float accel = 30f;
    [Tooltip("隕石に当たったときに速度に掛ける倍率")]
    [SerializeField, Range(0f, 1f)] float hitSlowdown = 0.5f;

    [Header("進行")]
    [SerializeField] float goalDistance = 1500f;
    [Tooltip("Ready 中、この Level 以上の声を startHoldTime 秒続けるとスタート")]
    [SerializeField, Range(0f, 1f)] float startThreshold = 0.5f;
    [SerializeField] float startHoldTime = 0.3f;

    public GameState State { get; private set; } = GameState.Ready;
    public float Speed { get; private set; }
    public float Distance { get; private set; }
    public float ElapsedTime { get; private set; }

    public float MinSpeed => minSpeed;
    public float MaxSpeed => maxSpeed;
    public float GoalDistance => goalDistance;

    /// <summary>ゴールまでの進み具合（0〜1）。</summary>
    public float Progress => goalDistance > 0f ? Mathf.Clamp01(Distance / goalDistance) : 0f;

    public bool IsCalibrating => calibration != null && calibration.IsCalibrating;

    public event Action<GameState> OnStateChanged;
    public event Action OnHit;

    float startHoldTimer;

    float Level => mic != null ? mic.Level : 0f;

    void Start()
    {
        if (mic == null) Debug.LogError("[GameManager] Mic が設定されていません。Inspector で MicInput をつないでください。");
        ResetToReady();
    }

    void Update()
    {
        switch (State)
        {
            case GameState.Ready:
                UpdateReady();
                break;
            case GameState.Playing:
                UpdatePlaying();
                break;
        }
    }

    void UpdateReady()
    {
        if (IsCalibrating)
        {
            startHoldTimer = 0f;
            return;
        }

        startHoldTimer = Level >= startThreshold ? startHoldTimer + Time.deltaTime : 0f;
        if (startHoldTimer >= startHoldTime) SetState(GameState.Playing);
    }

    void UpdatePlaying()
    {
        float targetSpeed = Mathf.Lerp(minSpeed, maxSpeed, Level);
        Speed = Mathf.MoveTowards(Speed, targetSpeed, accel * Time.deltaTime);
        Distance += Speed * Time.deltaTime;
        ElapsedTime += Time.deltaTime;

        if (Distance >= goalDistance)
        {
            Distance = goalDistance;
            SetState(GameState.Goal);
        }
    }

    /// <summary>初期状態（Ready）に戻し、キャリブレーションをやり直す。ResultUI / IdleResetter から呼ぶ。</summary>
    public void ResetToReady()
    {
        Speed = 0f;
        Distance = 0f;
        ElapsedTime = 0f;
        startHoldTimer = 0f;
        if (calibration != null) calibration.StartCalibration();
        SetState(GameState.Ready, force: true);
    }

    /// <summary>隕石に当たったことを通知する。ShipCollision から呼ぶ。</summary>
    public void NotifyHit()
    {
        if (State != GameState.Playing) return;
        Speed *= hitSlowdown;
        OnHit?.Invoke();
    }

    void SetState(GameState next, bool force = false)
    {
        if (State == next && !force) return;
        State = next;
        Debug.Log($"[GameManager] 状態: {next}");
        OnStateChanged?.Invoke(next);
    }
}
