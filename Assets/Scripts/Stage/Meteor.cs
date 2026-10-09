using UnityEngine;

/// <summary>
/// 隕石。GameManager の Speed で手前（-Z 方向）へ流れ、despawnZ より手前に来たら MeteorSpawner に回収してもらう。
/// 隕石の Prefab に付ける（シーンには置かない）。MeteorSpawner が生成時に Init() で参照を渡す。
///
/// Prefab 側で設定しておくこと:
///   ・Collider（Sphere Collider など）
///   ・Tag「Meteor」（ShipCollision が Tag で隕石を判定する）
/// </summary>
[RequireComponent(typeof(Collider))]
public class Meteor : MonoBehaviour
{
    [Tooltip("この Z より手前（カメラの後ろ）に来たら回収する")]
    [SerializeField] float despawnZ = -10f;
    [Tooltip("回転の速さ（度/秒）")]
    [SerializeField] float spinSpeed = 30f;

    GameManager gameManager;
    MeteorSpawner spawner;
    Vector3 spinAxis;

    /// <summary>MeteorSpawner が生成直後に呼ぶ。</summary>
    public void Init(GameManager gameManager, MeteorSpawner spawner)
    {
        this.gameManager = gameManager;
        this.spawner = spawner;
        spinAxis = Random.onUnitSphere;
    }

    void Update()
    {
        if (gameManager == null) return;

        transform.position += Vector3.back * (gameManager.Speed * Time.deltaTime);
        transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.World);

        if (transform.position.z < despawnZ) Despawn();
    }

    /// <summary>この隕石を回収する（画面の後ろに出たとき、宇宙船に当たったとき）。</summary>
    public void Despawn()
    {
        if (spawner != null) spawner.Despawn(this);
        else Destroy(gameObject);
    }
}
