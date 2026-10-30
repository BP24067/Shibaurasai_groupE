using UnityEngine;

/// <summary>
/// 声で宇宙船を動かす。
///   上下：声の大きさ（MicInput.Level）。声を出すほど上へ（maxY）、黙ると下へ（minY）。
///         加速も声なので「上に逃げるほど速くなる」。
///   左右：声の音色（MicInput.Timbre）。「いー」で右へ、「おー」で左へ、「あー」や無声ではまっすぐ（中央）。
///   Playing 中だけ動き、Ready では中央（restX, restY）に戻る。Goal 中は WarpSequence の演出に任せて何もしない。
///   上下に動くと機首を上げ下げし、左右に動くと機体を傾ける（visual だけを傾けるので当たり判定は傾かない）。
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

    [Header("上下の動き")]
    [Tooltip("目標の高さに追いつくまでのおおよその時間（秒）。小さいほどキビキビ動く")]
    [SerializeField] float smoothTime = 0.25f;
    [Tooltip("上下の最高速度（m/秒）")]
    [SerializeField] float maxVerticalSpeed = 8f;
    [Tooltip("上下の速さ 1m/秒 あたりに機首を傾ける角度")]
    [SerializeField] float tiltPerSpeed = 4f;
    [Tooltip("機首を傾ける最大角度")]
    [SerializeField] float maxTilt = 20f;

    [Header("左右（声の音色）")]
    [Tooltip("音色で左右に動かす（オフにすると上下だけになる）")]
    [SerializeField] bool moveHorizontally = true;
    [Tooltip("左右に動ける範囲（±m）。「いー」で +（右）、「おー」で -（左）")]
    [SerializeField] float rangeX = 4f;
    [Tooltip("中央の位置（Ready 中もここに戻る）")]
    [SerializeField] float restX = 0f;
    [Tooltip("目標の左右位置に追いつくまでのおおよその時間（秒）")]
    [SerializeField] float smoothTimeX = 0.3f;
    [Tooltip("左右の最高速度（m/秒）")]
    [SerializeField] float maxHorizontalSpeed = 10f;
    [Tooltip("左右の速さ 1m/秒 あたりに機体を傾ける（ロール）角度")]
    [SerializeField] float rollPerSpeed = 5f;
    [Tooltip("機体を傾ける最大角度")]
    [SerializeField] float maxRoll = 30f;

    float verticalVelocity;
    float horizontalVelocity;
    Quaternion visualBaseRotation = Quaternion.identity;

    /// <summary>今の上下の速さ（m/秒）。上向きが正。</summary>
    public float VerticalVelocity => verticalVelocity;

    /// <summary>今の左右の速さ（m/秒）。右向きが正。</summary>
    public float HorizontalVelocity => horizontalVelocity;

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

        float targetY, targetX;
        switch (gameManager.State)
        {
            case GameState.Playing:
                float level = mic != null ? mic.Level : 0f;
                float timbre = mic != null && moveHorizontally ? mic.Timbre : 0f;
                if (gameManager.ControlsReversed) timbre = -timbre;   // 紫の隕石に当たると左右が逆になる
                targetY = Mathf.Lerp(minY, maxY, level);
                targetX = restX + timbre * rangeX;
                break;
            case GameState.Ready:
                targetY = restY;
                targetX = restX;
                break;
            default:
                // Goal 中は WarpSequence が宇宙船を動かすので触らない
                verticalVelocity = 0f;
                horizontalVelocity = 0f;
                ApplyTilt();
                return;
        }

        Vector3 position = transform.localPosition;
        position.y = Mathf.SmoothDamp(position.y, targetY, ref verticalVelocity, smoothTime, maxVerticalSpeed);
        position.x = Mathf.SmoothDamp(position.x, targetX, ref horizontalVelocity, smoothTimeX, maxHorizontalSpeed);
        transform.localPosition = position;
        ApplyTilt();
    }

    void ApplyTilt()
    {
        if (visual == null) return;
        // +Z が前なので、X 軸まわりにマイナス回転すると機首が上がる
        float pitch = Mathf.Clamp(-verticalVelocity * tiltPerSpeed, -maxTilt, maxTilt);
        // Z 軸まわりにマイナス回転すると右の翼が下がる（右へ曲がるときの傾き）
        float roll = Mathf.Clamp(-horizontalVelocity * rollPerSpeed, -maxRoll, maxRoll);
        Quaternion target = visualBaseRotation * Quaternion.Euler(pitch, 0f, roll);
        visual.localRotation = Quaternion.Slerp(visual.localRotation, target, 1f - Mathf.Exp(-10f * Time.deltaTime));
    }
}
