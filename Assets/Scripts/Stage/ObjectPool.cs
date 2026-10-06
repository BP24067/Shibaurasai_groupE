using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 隕石の使い回し（オブジェクトプール）。
/// 毎回 Instantiate / Destroy すると処理が重くなるので、最初にまとめて作っておき、
/// 使うときは表示（SetActive(true)）、使い終わったら非表示にして取っておく。
/// 足りなくなったら自動で追加で作る。
///
/// MeteorSpawner と同じオブジェクトに付ける。作った隕石はこのオブジェクトの子になる。
/// </summary>
public class ObjectPool : MonoBehaviour
{
    [Tooltip("Project の Assets/Prefabs にある隕石の Prefab")]
    [SerializeField] Meteor prefab;
    [Tooltip("最初に作っておく数（同時に画面に出る最大数より少し多めに）")]
    [SerializeField] int initialCount = 30;

    readonly Stack<Meteor> inactive = new Stack<Meteor>();

    public Meteor Prefab => prefab;

    void Awake()
    {
        if (prefab == null)
        {
            Debug.LogError("[ObjectPool] Prefab が設定されていません。");
            return;
        }
        for (int i = 0; i < initialCount; i++)
        {
            Meteor meteor = Create();
            meteor.gameObject.SetActive(false);
            inactive.Push(meteor);
        }
    }

    /// <summary>隕石を 1 つ借りる。指定した位置・向きで表示された状態で返す。</summary>
    public Meteor Get(Vector3 position, Quaternion rotation)
    {
        Meteor meteor = inactive.Count > 0 ? inactive.Pop() : Create();
        meteor.transform.SetPositionAndRotation(position, rotation);
        meteor.gameObject.SetActive(true);
        return meteor;
    }

    /// <summary>使い終わった隕石を返す（非表示にして取っておく）。</summary>
    public void Release(Meteor meteor)
    {
        if (meteor == null || !meteor.gameObject.activeSelf) return;  // 二重に返したときの対策
        meteor.gameObject.SetActive(false);
        inactive.Push(meteor);
    }

    Meteor Create() => Instantiate(prefab, transform);
}
