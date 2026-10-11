using UnityEngine;

/// <summary>
/// 声を出さずに通過する区間（「余裕があれば」の機能）。
///   startDistance 〜 endDistance の間は、声で加速しなくなり（GameManager.HoldSpeed）、今の速度のまま進む。
///   区間中に voiceThreshold 以上の声を出すと、ブレーキがかかって減速する。
///   → 区間の手前でしっかり加速しておき、区間中は静かにするのが速く進むコツ。
///
/// [Stage]/SilentZone（空のオブジェクト）に付ける。
/// メッセージは日本語なので、MessageUI の TextMeshPro に日本語フォントが必要。
/// </summary>
public class SilentZone : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] GameManager gameManager;
    [SerializeField] MicInput mic;
    [Tooltip("区間の案内を表示する MessageUI（空なら表示しない）")]
    [SerializeField] MessageUI message;

    [Header("区間（スタートからの距離 m）")]
    [SerializeField] float startDistance = 700f;
    [SerializeField] float endDistance = 850f;
    [Tooltip("区間の何 m 手前から予告を出すか")]
    [SerializeField] float noticeDistance = 80f;

    [Header("判定")]
    [Tooltip("この Level 以上の声を「声を出した」とみなす")]
    [SerializeField, Range(0f, 1f)] float voiceThreshold = 0.3f;
    [Tooltip("声を出している間、1 秒あたりに下げる速度")]
    [SerializeField] float brakePerSecond = 25f;

    [Header("メッセージ")]
    [SerializeField] string noticeText = "この先 しずかに通過！";
    [SerializeField] string zoneText = "しーっ… 声を出さないで！";
    [SerializeField] string loudText = "声が出ています！ 減速中…";
    [SerializeField] string clearText = "通過！ 声を出して加速！";
    [Tooltip("通過メッセージを表示しておく秒数")]
    [SerializeField] float clearTextDuration = 1.5f;

    enum Phase { Before, Notice, InZone, After }

    Phase phase;
    bool wasLoud;
    float clearTimer;

    /// <summary>今、区間の中にいるか。</summary>
    public bool IsInZone => phase == Phase.InZone;

    void OnEnable()
    {
        if (gameManager != null) gameManager.OnStateChanged += HandleStateChanged;
    }

    void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnStateChanged -= HandleStateChanged;
            if (IsInZone) gameManager.HoldSpeed = false;
        }
    }

    void Start()
    {
        if (gameManager == null) Debug.LogError("[SilentZone] GameManager が設定されていません。");
        if (mic == null) Debug.LogError("[SilentZone] Mic が設定されていません。");
        if (endDistance <= startDistance) Debug.LogWarning("[SilentZone] End Distance は Start Distance より大きくしてください。");
        if (gameManager != null && endDistance >= gameManager.GoalDistance)
            Debug.LogWarning("[SilentZone] 区間がゴールより先まで続いています。End Distance を Goal Distance より小さくしてください。");
    }

    void Update()
    {
        if (gameManager == null || gameManager.State != GameState.Playing) return;

        float distance = gameManager.Distance;
        switch (phase)
        {
            case Phase.Before:
                if (distance >= startDistance - noticeDistance)
                {
                    phase = Phase.Notice;
                    Show(noticeText);
                }
                break;

            case Phase.Notice:
                if (distance >= startDistance)
                {
                    phase = Phase.InZone;
                    gameManager.HoldSpeed = true;
                    wasLoud = false;
                    Show(zoneText);
                }
                break;

            case Phase.InZone:
                bool loud = mic != null && mic.Level >= voiceThreshold;
                if (loud) gameManager.Brake(brakePerSecond);
                if (loud != wasLoud)
                {
                    Show(loud ? loudText : zoneText);
                    wasLoud = loud;
                }

                if (distance >= endDistance)
                {
                    phase = Phase.After;
                    gameManager.HoldSpeed = false;
                    clearTimer = clearTextDuration;
                    Show(clearText);
                }
                break;

            case Phase.After:
                if (clearTimer > 0f)
                {
                    clearTimer -= Time.deltaTime;
                    if (clearTimer <= 0f) Show("");
                }
                break;
        }
    }

    void HandleStateChanged(GameState state)
    {
        if (state != GameState.Ready) return;
        phase = Phase.Before;
        wasLoud = false;
        clearTimer = 0f;
    }

    void Show(string text)
    {
        if (message != null) message.Show(text);
    }
}
