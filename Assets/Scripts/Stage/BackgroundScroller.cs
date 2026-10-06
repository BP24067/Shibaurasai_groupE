using UnityEngine;

/// <summary>
/// GameManagerのSpeedに応じて背景のテクスチャをスクロール
/// 視差効果で奥行きを出すスクリプト
/// </summary>


public class BackgroundScroller : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] GameManager gameManager;
    [SerializeField] Renderer targetRenderer; //スクロールさせるQuad,ImageのRenderer

    [Header("スクロール設定")]
    [Toolip("スクロール速度の倍率（奥のレイヤーは小さく、手前のレイヤーは大きく設定）")]
    [SerializeField] float scrollSpeedMultiplier = 0.1f;

    [Toolip("スクロールさせる方向（Y:-1で上から下、1で下から上）")]
    [SerializeField] Vector2 scrollDirection = new Vector2(0f, -1f);

    Material material;
    Vector2 currentOffset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if(targetRenderer != null)
        {
            material = targetRenderer.material;
        }
        else
        {
            Debug.LogError("[BackgroundScroller] Rendererが設定されていません。");
        }

        if(gameManager == null)
        {
            Debug.LogError("[BackgroundScroller] GameManagerが設定されていません。");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(material == null || gameManager == null) return;

        //GameManagerの現在の速度を取得
        float speed = gameManager.Speed;

        //速度・倍率・方向を加味してオフセット加算
        currentOffset += scrollDirection.normalized * (speed * scrollSpeedMultiplier * Time.deltaTime);

        //テクスチャのUVオフセットを更新（1を超えたらリセットして数値が大きくなりすぎるのを防ぐ）
        currentOffset.x %= 1f;
        currentOffset.y %= 1f;

        material.mainTextureOffset = currentOffset;
    }
}
