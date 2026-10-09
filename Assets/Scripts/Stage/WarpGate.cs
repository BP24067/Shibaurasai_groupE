using UnityEngine;

/// <summary>
/// ワープゲートの出現と接近。
/// ゴールの appearBeforeGoal m 手前になったらゲートを表示し、「ゴールまでの残り距離」だけ前方（+Z）に置く。
/// こうすると、進んだ距離がゴールに届いた瞬間に、ちょうど宇宙船（goalZ の位置）がゲートをくぐる。
///
/// 常に有効な親オブジェクト（WarpGate）に付け、visual には表示を切り替える子（GateModel）を指定する。
/// （非表示のオブジェクトに付けると Update が動かず、二度と表示できなくなるため）
/// </summary>
public class WarpGate : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [Tooltip("表示を切り替えるゲートのモデル（子オブジェクト）")]
    [SerializeField] GameObject visual;
    [Tooltip("ゴールの何 m 手前からゲートを表示するか")]
    [SerializeField] float appearBeforeGoal = 150f;
    [Tooltip("宇宙船の Z 位置（ゲートをくぐる位置）")]
    [SerializeField] float goalZ = 0f;
    [Tooltip("ゲートを Z 軸まわりに回す速さ（度/秒）。0 で回転なし")]
    [SerializeField] float spinSpeed = 10f;

    /// <summary>ゲートが表示されているか。</summary>
    public bool IsVisible => visual != null && visual.activeSelf;

    /// <summary>ゴール（ゲート）までの残り距離。</summary>
    public float RemainingDistance => gameManager != null ? Mathf.Max(gameManager.GoalDistance - gameManager.Distance, 0f) : 0f;

    void OnEnable()
    {
        if (gameManager != null) gameManager.OnStateChanged += HandleStateChanged;
    }

    void OnDisable()
    {
        if (gameManager != null) gameManager.OnStateChanged -= HandleStateChanged;
    }

    void Start()
    {
        if (gameManager == null) Debug.LogError("[WarpGate] GameManager が設定されていません。");
        if (visual == null) Debug.LogError("[WarpGate] Visual（ゲートのモデル）が設定されていません。");
        else if (visual == gameObject) Debug.LogError("[WarpGate] Visual に自分自身は指定できません。子オブジェクトを指定してください。");
        SetVisible(false);
    }

    void Update()
    {
        if (gameManager == null || visual == null) return;
        if (gameManager.State == GameState.Ready) return;

        float remaining = RemainingDistance;
        if (!IsVisible && remaining <= appearBeforeGoal) SetVisible(true);
        if (!IsVisible) return;

        Vector3 position = transform.position;
        position.z = goalZ + remaining;
        transform.position = position;

        if (spinSpeed != 0f) visual.transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime, Space.World);
    }

    void HandleStateChanged(GameState state)
    {
        if (state == GameState.Ready) SetVisible(false);
    }

    void SetVisible(bool visible)
    {
        if (visual != null && visual != gameObject) visual.SetActive(visible);
    }
}
