using UnityEngine;

/// <summary>
/// 声の大きさ（MicInput.Level）で宇宙船を上下に動かす。
///   声を出すほど上へ（maxY）、黙ると下へ（minY）。加速も声なので「上に逃げるほど速くなる」。
///   Playing 中だけ動き、Ready では真ん中（restY）に戻る。Goal 中は WarpSequence の演出に任せて何もしない。
///   上昇中は機首を少し上げ、下降中は下げる（visual だけを傾けるので当たり判定は傾かない）。
///
/// Ship（宇宙船の親オブジェクト）に付ける。visual には子の ShipModel を指定する。
/// </summary>
public class ShipMover : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] GameManager gameManager;
    [SerializeField] MicInput mic;
    [Tooltip("傾ける見た目のモデル（子の ShipModel）。空なら傾けない")]
    [SerializeField] Transform visual;

    [Header("上下の範囲（Ship の親から見た Y）")]
    [Tooltip("声を出していないときの高さ")]
    [SerializeField] float minY = -2f;
    [Tooltip("最大の声を出したときの高さ")]
    [SerializeField] float maxY = 2.8f;
    [Tooltip("Ready 中に戻る高さ")]
    [SerializeField] float restY = 0f;

    [Header("動き")]
    [Tooltip("目標の高さに追いつくまでのおおよその時間（秒）。小さいほどキビキビ動く")]
    [SerializeField] float smoothTime = 0.25f;
    [Tooltip("上下の最高速度（m/秒）")]
    [SerializeField] float maxVerticalSpeed = 8f;
    [Tooltip("上下の速さ 1m/秒 あたりに機首を傾ける角度")]
    [SerializeField] float tiltPerSpeed = 4f;
    [Tooltip("機首を傾ける最大角度")]
    [SerializeField] float maxTilt = 20f;

    float verticalVelocity;
    Quaternion visualBaseRotation = Quaternion.identity;

    /// <summary>今の上下の速さ（m/秒）。上向きが正。</summary>
    public float VerticalVelocity => verticalVelocity;

    void Awake()
    {
        if (visual != null) visualBaseRotation = visual.localRotation;
    }

    void Start()
    {
        if (gameManager == null) Debug.LogError("[ShipMover] GameManager が設定されていません。");
        if (mic == null) Debug.LogError("[ShipMover] Mic が設定されていません。");
    }

    void Update()
    {
        if (gameManager == null) return;

        float targetY;
        switch (gameManager.State)
        {
            case GameState.Playing:
                float level = mic != null ? mic.Level : 0f;
                targetY = Mathf.Lerp(minY, maxY, level);
                break;
            case GameState.Ready:
                targetY = restY;
                break;
            default:
                // Goal 中は WarpSequence が宇宙船を動かすので触らない
                verticalVelocity = 0f;
                ApplyTilt();
                return;
        }

        Vector3 position = transform.localPosition;
        position.y = Mathf.SmoothDamp(position.y, targetY, ref verticalVelocity, smoothTime, maxVerticalSpeed);
        transform.localPosition = position;
        ApplyTilt();
    }

    void ApplyTilt()
    {
        if (visual == null) return;
        // +Z が前なので、X 軸まわりにマイナス回転すると機首が上がる
        float pitch = Mathf.Clamp(-verticalVelocity * tiltPerSpeed, -maxTilt, maxTilt);
        Quaternion target = visualBaseRotation * Quaternion.Euler(pitch, 0f, 0f);
        visual.localRotation = Quaternion.Slerp(visual.localRotation, target, 1f - Mathf.Exp(-10f * Time.deltaTime));
    }
}
