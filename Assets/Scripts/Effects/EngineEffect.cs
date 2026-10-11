using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// エンジンの噴射（パーティクル）と発光を、声の大きさ（MicInput.Level）に合わせて強くする。
/// EngineParticle（Ship の子に置いた Particle System）に付ける。
///
/// 発光させるには:
///   ・glowRenderers にエンジンの Renderer（Engine_Center / Engine_Left / Engine_Right）を入れる
///   ・そのマテリアル（名前に glowMaterialName を含むもの。例: M_Engine_Glow）の Emission をオンにしておく
/// マテリアルのファイル自体は書き換えない（MaterialPropertyBlock で色だけ変える）ので、Git に差分は出ない。
///
/// パーティクルの Start Speed / Start Size は「Constant」にしておくこと。
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class EngineEffect : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] MicInput mic;
    [Tooltip("噴射のパーティクル（空なら同じオブジェクトのもの）")]
    [SerializeField] ParticleSystem particles;
    [Tooltip("発光させるエンジンの Renderer（任意）")]
    [SerializeField] Renderer[] glowRenderers;
    [Tooltip("発光させるマテリアルの名前に含まれる文字")]
    [SerializeField] string glowMaterialName = "Engine_Glow";
    [Tooltip("一緒に明るさを変える Point Light（任意）")]
    [SerializeField] Light engineLight;

    [Header("噴射（声 0 → 1）")]
    [SerializeField] float minEmission = 20f;
    [SerializeField] float maxEmission = 200f;
    [SerializeField] float minStartSpeed = 2f;
    [SerializeField] float maxStartSpeed = 12f;
    [SerializeField] float minStartSize = 0.15f;
    [SerializeField] float maxStartSize = 0.4f;

    [Header("発光（声 0 → 1）")]
    [ColorUsage(false, true)]
    [SerializeField] Color glowColor = new Color(0.3f, 0.7f, 1f);
    [SerializeField] float minGlowIntensity = 1f;
    [SerializeField] float maxGlowIntensity = 6f;
    [SerializeField] float minLightIntensity = 0.5f;
    [SerializeField] float maxLightIntensity = 4f;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    struct GlowSlot
    {
        public Renderer renderer;
        public int materialIndex;
    }

    readonly List<GlowSlot> glowSlots = new List<GlowSlot>();
    MaterialPropertyBlock block;

    void Awake()
    {
        if (particles == null) particles = GetComponent<ParticleSystem>();
        block = new MaterialPropertyBlock();

        if (glowRenderers == null) return;
        foreach (var renderer in glowRenderers)
        {
            if (renderer == null) continue;
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && materials[i].name.Contains(glowMaterialName))
                    glowSlots.Add(new GlowSlot { renderer = renderer, materialIndex = i });
            }
        }
        if (glowRenderers.Length > 0 && glowSlots.Count == 0)
            Debug.LogWarning($"[EngineEffect] 名前に「{glowMaterialName}」を含むマテリアルが見つかりません。発光は変化しません。");
    }

    void Start()
    {
        if (mic == null) Debug.LogError("[EngineEffect] Mic が設定されていません。");
    }

    void Update()
    {
        float level = mic != null ? mic.Level : 0f;

        var main = particles.main;
        main.startSpeedMultiplier = Mathf.Lerp(minStartSpeed, maxStartSpeed, level);
        main.startSizeMultiplier = Mathf.Lerp(minStartSize, maxStartSize, level);
        var emission = particles.emission;
        emission.rateOverTimeMultiplier = Mathf.Lerp(minEmission, maxEmission, level);

        if (glowSlots.Count > 0)
        {
            Color color = glowColor * Mathf.Lerp(minGlowIntensity, maxGlowIntensity, level);
            foreach (var slot in glowSlots)
            {
                slot.renderer.GetPropertyBlock(block, slot.materialIndex);
                block.SetColor(EmissionColorId, color);
                slot.renderer.SetPropertyBlock(block, slot.materialIndex);
            }
        }

        if (engineLight != null) engineLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, level);
    }
}
