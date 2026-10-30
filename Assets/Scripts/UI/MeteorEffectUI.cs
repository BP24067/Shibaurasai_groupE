using TMPro;
using UnityEngine;

/// <summary>
/// 色つき隕石に当たったときに、何が起きたかを画面に短く表示する（GameManager.OnMeteorHit）。
///   金 … 「BOOST!!」（あわせて pickupSe を鳴らす）
///   赤 … 「大減速!!」
///   紫 … 「左右反転!!」と残り秒数（左右が逆の間ずっと表示）
///   灰 … 何も表示しない（今までどおり）
/// Playing 以外になったら消す。
///
/// Canvas の子（MeteorEffect）に付け、text には同じオブジェクトの TextMeshPro を指定する。
/// </summary>
public class MeteorEffectUI : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] TextMeshProUGUI text;
    [Tooltip("金の隕石を取ったときに鳴らす（空なら鳴らさない）")]
    [SerializeField] Effects.AudioManager audioManager;
    [SerializeField] AudioClip pickupSe;
    [Tooltip("表示しておく秒数（紫は左右が逆の間ずっと表示）")]
    [SerializeField] float showTime = 1.2f;

    [Header("色")]
    [SerializeField] Color goldColor = new Color(1f, 0.82f, 0.25f);
    [SerializeField] Color redColor = new Color(1f, 0.3f, 0.25f);
    [SerializeField] Color purpleColor = new Color(0.8f, 0.45f, 1f);

    float hideTimer;

    void OnEnable()
    {
        if (gameManager == null) return;
        gameManager.OnMeteorHit += HandleMeteorHit;
        gameManager.OnStateChanged += HandleStateChanged;
    }

    void OnDisable()
    {
        if (gameManager == null) return;
        gameManager.OnMeteorHit -= HandleMeteorHit;
        gameManager.OnStateChanged -= HandleStateChanged;
    }

    void Start()
    {
        if (gameManager == null) Debug.LogError("[MeteorEffectUI] GameManager が設定されていません。");
        Hide();
    }

    void Update()
    {
        if (text == null || gameManager == null) return;

        // 紫：左右が逆の間は残り秒数を出し続ける
        if (gameManager.ControlsReversed)
        {
            Show($"左右反転!!  {Mathf.CeilToInt(gameManager.ReverseTimeLeft)}\n<size=55%>「いー」で左 ・ 「おー」で右</size>", purpleColor);
            hideTimer = 0f;
            return;
        }

        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f) Hide();
        }
        else if (text.text.Length > 0)
        {
            Hide();   // 左右反転が終わったとき
        }
    }

    void HandleMeteorHit(MeteorKind kind)
    {
        switch (kind)
        {
            case MeteorKind.Gold:
                Show("BOOST!!", goldColor);
                hideTimer = showTime;
                if (audioManager != null) audioManager.PlaySe(pickupSe);
                break;
            case MeteorKind.Red:
                Show("大減速!!", redColor);
                hideTimer = showTime;
                break;
            // 紫は Update で表示する。灰は何も出さない
        }
    }

    void HandleStateChanged(GameState state)
    {
        if (state != GameState.Playing) Hide();
    }

    void Show(string message, Color color)
    {
        if (text == null) return;
        text.text = message;
        text.color = color;
    }

    void Hide()
    {
        hideTimer = 0f;
        if (text != null) text.text = "";
    }
}
