using UnityEngine;

/// <summary>
/// 隕石に当たったとき（GameManager.OnHit）に、破片のパーティクルを出す。
/// HitParticle（Ship の子、機首の少し前に置いた Particle System）に付ける。
///
/// このスクリプトを Inspector で追加すると、Particle System が
/// 「Looping オフ・Play On Awake オフ・自動放出 0」に設定される（当たったときだけ出すため）。
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class HitEffect : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [Tooltip("破片のパーティクル（空なら同じオブジェクトのもの）")]
    [SerializeField] ParticleSystem particles;
    [Tooltip("1 回の衝突で出す破片の数")]
    [SerializeField] int burstCount = 40;

    // Inspector でこのスクリプトを追加したとき（または ⋮ → Reset）に自動で呼ばれる
    void Reset()
    {
        var system = GetComponent<ParticleSystem>();
        var main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        var emission = system.emission;
        emission.rateOverTimeMultiplier = 0f;
    }

    void Awake()
    {
        if (particles == null) particles = GetComponent<ParticleSystem>();
    }

    void OnEnable()
    {
        if (gameManager != null) gameManager.OnHit += HandleHit;
    }

    void OnDisable()
    {
        if (gameManager != null) gameManager.OnHit -= HandleHit;
    }

    void Start()
    {
        if (gameManager == null) Debug.LogError("[HitEffect] GameManager が設定されていません。");
    }

    void HandleHit()
    {
        if (!particles.isPlaying) particles.Play();
        particles.Emit(burstCount);
    }
}
