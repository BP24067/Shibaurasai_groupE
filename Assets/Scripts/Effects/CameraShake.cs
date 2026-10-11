using System.Collections;
using UnityEngine;

/// <summary>
/// GameManagerのOnHitイベントを受け取り、カメラを揺らす（画面シェイク）スクリプト
/// </summary>
public class CameraShake : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] GameManager gameManager;

    [Header("シェイク設定")]
    [Tooltip("揺れる時間（秒）")]
    [SerializeField] float duration = 0.3f;
    
    [Tooltip("揺れの強さ（振幅）")]
    [SerializeField] float magnitude = 0.5f;

    Vector3 originalPosition;
    Coroutine shakeCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalPosition = transform.localPosition;

        if(gameManager == null)
        {
            Debug.LogError("[CameraShake] GameManagerが設定されていません。");
        }    
    }

    void OnEnable()
    {
        if(gameManager != null)
        {
            //GameManagerのOnHitイベントを購読
            gameManager.OnHit += HandleHit;
        }
    }

    void OnDisable()
    {
        if(gameManager != null)
        {
            //イベント解除（メモリリーク・エラー防止）
            gameManager.OnHit -= HandleHit;
        }
    }

    void HandleHit()
    {
        //すでに揺れている最中に連続ヒットした場合は一度停止してリセット
        if(shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        shakeCoroutine = StartCoroutine(Shake());
    }

    IEnumerator Shake()
    {
        float elapsed = 0f;

        while(elapsed <duration)
        {
            //ランダムな位置オフセットを計算
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;

            //初期位置にオフセットを加算
            transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null; //1フレーム待機
        }

        //揺れが終わったら必ず元の位置に戻す
        transform.localPosition = originalPosition;
        shakeCoroutine = null;
    }
}
