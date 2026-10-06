using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 隕石を奥（spawnZ）に生成する。生成の間隔は時間ではなく「進んだ距離」で決めるため、
/// 速く進むほどたくさんの隕石が流れてきてスピード感が出る。
///
///   Ready   になったら … 残っている隕石をすべて消す
///   Playing になったら … 最初から何個か並べておき（prewarm）、以降は距離に応じて生成
///   ゴールの stopBeforeGoal m 手前からは生成しない（ワープゲート周りを空ける）
///
/// MeteorSpawner オブジェクトは宇宙船と同じ X, Y に置く（通常は 0, 0, 0）。生成範囲はその X, Y を中心にする。
/// 隕石は同じオブジェクトに付けた ObjectPool から借りて、使い終わったら返す。
/// </summary>
public class MeteorSpawner : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] GameManager gameManager;
    [Tooltip("隕石を貸し出す ObjectPool（通常は同じオブジェクトに付いているもの）")]
    [SerializeField] ObjectPool pool;

    [Header("出現位置")]
    [Tooltip("隕石を出す奥行き（Z）")]
    [SerializeField] float spawnZ = 120f;
    [Tooltip("左右に散らばる範囲（±）")]
    [SerializeField] float rangeX = 6f;
    [Tooltip("上下に散らばる範囲（±）")]
    [SerializeField] float rangeY = 3f;
    [Tooltip("宇宙船の正面付近に出す確率（0〜1）。大きいほど当たりやすい")]
    [SerializeField, Range(0f, 1f)] float centerBias = 0.25f;
    [Tooltip("「正面付近」とみなす半径")]
    [SerializeField] float centerRadius = 1.5f;

    [Header("出現間隔（難易度）")]
    [Tooltip("スタート直後、何 m 進むごとに 1 個出すか")]
    [SerializeField] float everyDistanceAtStart = 10f;
    [Tooltip("ゴール直前、何 m 進むごとに 1 個出すか（小さいほど密になる）")]
    [SerializeField] float everyDistanceAtGoal = 5f;
    [Tooltip("ゴールの何 m 手前から隕石を出さないか")]
    [SerializeField] float stopBeforeGoal = 100f;
    [Tooltip("スタート時に、最初から並べておく隕石の数")]
    [SerializeField] int prewarmCount = 6;
    [Tooltip("最初から並べるときの、手前側の Z")]
    [SerializeField] float prewarmMinZ = 40f;

    [Header("見た目")]
    [Tooltip("Prefab の大きさに掛ける倍率の範囲")]
    [SerializeField] float minScale = 0.6f;
    [SerializeField] float maxScale = 1.6f;

    const float MinSpawnInterval = 0.5f;  // 間隔が 0 以下になって無限ループしないための下限

    readonly List<Meteor> activeMeteors = new List<Meteor>();
    float nextSpawnDistance;

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
        if (gameManager == null) Debug.LogError("[MeteorSpawner] GameManager が設定されていません。");
        if (pool == null) Debug.LogError("[MeteorSpawner] Pool が設定されていません。");
    }

    void Update()
    {
        if (gameManager == null || pool == null) return;
        if (gameManager.State != GameState.Playing) return;
        if (gameManager.Distance > gameManager.GoalDistance - stopBeforeGoal) return;

        while (gameManager.Distance >= nextSpawnDistance)
        {
            // 1 フレームで進みすぎた分だけ手前に出し、隕石どうしの間隔をそろえる
            Spawn(spawnZ - (gameManager.Distance - nextSpawnDistance));
            nextSpawnDistance += CurrentInterval();
        }
    }

    void HandleStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Ready:
                DespawnAll();
                break;

            case GameState.Playing:
                nextSpawnDistance = gameManager.Distance + CurrentInterval();
                for (int i = 0; i < prewarmCount; i++) Spawn(Random.Range(prewarmMinZ, spawnZ));
                break;
        }
    }

    float CurrentInterval()
    {
        float interval = Mathf.Lerp(everyDistanceAtStart, everyDistanceAtGoal, gameManager.Progress);
        return Mathf.Max(interval, MinSpawnInterval);
    }

    void Spawn(float z)
    {
        if (pool == null || pool.Prefab == null) return;

        Vector2 offset = Random.value < centerBias
            ? Random.insideUnitCircle * centerRadius
            : new Vector2(Random.Range(-rangeX, rangeX), Random.Range(-rangeY, rangeY));
        var position = new Vector3(transform.position.x + offset.x, transform.position.y + offset.y, z);

        Meteor meteor = pool.Get(position, Random.rotation);
        meteor.transform.localScale = pool.Prefab.transform.localScale * Random.Range(minScale, maxScale);
        meteor.Init(gameManager, this);
        activeMeteors.Add(meteor);
    }

    /// <summary>隕石を回収してプールに返す（Meteor.Despawn() から呼ばれる）。</summary>
    public void Despawn(Meteor meteor)
    {
        if (!activeMeteors.Remove(meteor)) return;  // すでに回収済みなら何もしない
        pool.Release(meteor);
    }

    void DespawnAll()
    {
        foreach (var meteor in activeMeteors)
        {
            if (meteor != null) pool.Release(meteor);
        }
        activeMeteors.Clear();
    }

    // Scene 画面で MeteorSpawner を選んだとき、出現範囲を黄色い枠で表示する
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        var center = new Vector3(transform.position.x, transform.position.y, spawnZ);
        Gizmos.DrawWireCube(center, new Vector3(rangeX * 2f, rangeY * 2f, 0.1f));
        Gizmos.DrawWireSphere(center, centerRadius);
    }
}
