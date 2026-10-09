using UnityEngine;

/// <summary>
/// 宇宙船の当たり判定。Tag が「Meteor」のオブジェクトに触れたら GameManager.NotifyHit() で減速を通知する。
/// Ship（宇宙船の親オブジェクト）に付ける。
///
/// 必要な設定（このスクリプトを Inspector で追加したときに自動で設定される）:
///   ・Collider … Is Trigger をオン（Capsule Collider などに替えて機体の形に合わせる）
///   ・Rigidbody … Is Kinematic をオン、Use Gravity をオフ（物理で落ちたり押されたりしないように）
/// Tag「Meteor」は Tags and Layers で事前に作っておくこと。
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class ShipCollision : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] string meteorTag = "Meteor";
    [Tooltip("当たったあと、この秒数は次の当たりを無視する（連続ヒット防止）")]
    [SerializeField] float invincibleTime = 0.5f;
    [Tooltip("当たった隕石を消す（オフにするとすり抜けて飛んでいく）")]
    [SerializeField] bool despawnMeteorOnHit = true;

    float lastHitTime = float.NegativeInfinity;

    // Inspector でこのスクリプトを追加したとき（または ⋮ → Reset）に自動で呼ばれる
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
        var body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    void Start()
    {
        if (gameManager == null) Debug.LogError("[ShipCollision] GameManager が設定されていません。");
        if (!GetComponent<Collider>().isTrigger) Debug.LogWarning("[ShipCollision] Collider の Is Trigger がオフです。");
        if (!GetComponent<Rigidbody>().isKinematic) Debug.LogWarning("[ShipCollision] Rigidbody の Is Kinematic がオフです。");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(meteorTag)) return;
        if (Time.time - lastHitTime < invincibleTime) return;
        lastHitTime = Time.time;

        if (gameManager != null) gameManager.NotifyHit();

        if (despawnMeteorOnHit)
        {
            var meteor = other.GetComponentInParent<Meteor>();
            if (meteor != null) meteor.Despawn();
        }
    }
}
