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
///   OnHit          … 隕石に当たったとき（CameraShake, HitEffect, AudioManager など）。金の隕石では呼ばない
///   OnMeteorHit    … 隕石に当たったとき、その種類（色）と一緒に（MeteorEffectUI など）。金の隕石でも呼ぶ
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
    [SerializeField, Range(0f, 1f)] float hitSlowdown = 0.3f;
    [Tooltip("隕石に当たってから、この秒数は加速できない（減速を感じやすくする）")]
    [SerializeField] float hitRecoveryTime = 1f;

    [Header("色つき隕石（MeteorKind）")]
    [Tooltip("赤：速度に掛ける倍率")]
    [SerializeField, Range(0f, 1f)] float redSlowdown = 0.1f;
    [Tooltip("赤：当たってから加速できない秒数")]
    [SerializeField] float redRecoveryTime = 2f;
    [Tooltip("金：ブースト中の速度（maxSpeed に掛ける倍率）")]
    [SerializeField] float boostSpeedMultiplier = 1.5f;
    [Tooltip("金：ブーストが続く秒数")]
    [SerializeField] float boostTime = 1.5f;
    [Tooltip("紫：左右の操作が逆になる秒数")]
    [SerializeField] float reverseTime = 3f;

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

    /// <summary>
    /// true の間は声の大きさで速度を変えず、今の速度を保つ（SilentZone 用）。
    /// この間、速度は Brake() でだけ下がる。ResetToReady() で false に戻る。
    /// </summary>
    public bool HoldSpeed { get; set; }

    /// <summary>紫の隕石に当たって、左右の操作が逆になっているか（ShipMover が使う）。</summary>
    public bool ControlsReversed => reverseTimer > 0f;

    /// <summary>左右が逆になっている残り秒数。</summary>
    public float ReverseTimeLeft => Mathf.Max(0f, reverseTimer);

    /// <summary>金の隕石を取って、ブースト中か。</summary>
    public bool IsBoosting => boostTimer > 0f;

    public event Action<GameState> OnStateChanged;
    public event Action OnHit;
    public event Action<MeteorKind> OnMeteorHit;

    float startHoldTimer;
    float hitRecoveryTimer;
    float boostTimer;
    float reverseTimer;

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
        if (hitRecoveryTimer > 0f) hitRecoveryTimer -= Time.deltaTime;
        if (boostTimer > 0f) boostTimer -= Time.deltaTime;
        if (reverseTimer > 0f) reverseTimer -= Time.deltaTime;

        if (!HoldSpeed)
        {
            float targetSpeed = Mathf.Lerp(minSpeed, maxSpeed, Level);
            if (boostTimer > 0f) targetSpeed = maxSpeed * boostSpeedMultiplier;
            // 当たった直後は加速させない（声を小さくしたときの減速はそのまま）
            if (hitRecoveryTimer > 0f) targetSpeed = Mathf.Min(targetSpeed, Speed);
            Speed = Mathf.MoveTowards(Speed, targetSpeed, accel * Time.deltaTime);
        }
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
        hitRecoveryTimer = 0f;
        boostTimer = 0f;
        reverseTimer = 0f;
        HoldSpeed = false;
        if (calibration != null) calibration.StartCalibration();
        SetState(GameState.Ready, force: true);
    }

    /// <summary>隕石に当たったことを通知する。ShipCollision から呼ぶ。種類（色）によって作用が変わる。</summary>
    public void NotifyHit(MeteorKind kind = MeteorKind.Normal)
    {
        if (State != GameState.Playing) return;

        switch (kind)
        {
            case MeteorKind.Gold:
                // ブースト：一気に加速し、当たった後の失速も消す。良い隕石なので OnHit（揺れ・衝突音）は出さない
                boostTimer = boostTime;
                hitRecoveryTimer = 0f;
                Speed = Mathf.Max(Speed, maxSpeed * boostSpeedMultiplier);
                OnMeteorHit?.Invoke(kind);
                return;

            case MeteorKind.Red:
                Speed = Mathf.Max(minSpeed, Speed * redSlowdown);   // minSpeed より遅くはしない（止まって見えないように）
                hitRecoveryTimer = redRecoveryTime;
                boostTimer = 0f;
                break;

            case MeteorKind.Purple:
                // 減速はせず、左右の操作だけ逆にする
                reverseTimer = reverseTime;
                break;

            default:
                Speed = Mathf.Max(minSpeed, Speed * hitSlowdown);
                hitRecoveryTimer = hitRecoveryTime;
                boostTimer = 0f;
                break;
        }
        OnHit?.Invoke();
        OnMeteorHit?.Invoke(kind);
    }

    /// <summary>速度を毎秒 amountPerSecond ずつ下げる（minSpeed より下がらない）。SilentZone が毎フレーム呼ぶ。</summary>
    public void Brake(float amountPerSecond)
    {
        if (State != GameState.Playing) return;
        Speed = Mathf.Max(minSpeed, Speed - amountPerSecond * Time.deltaTime);
    }

    void SetState(GameState next, bool force = false)
    {
        if (State == next && !force) return;
        State = next;
        Debug.Log($"[GameManager] 状態: {next}");
        OnStateChanged?.Invoke(next);
    }
}
