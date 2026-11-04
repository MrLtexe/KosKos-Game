using UnityEngine;

// GEÇİCİ: Boost şarj adımını karakterin rengiyle gösterir (bar yok, B.4) ve şarj parçacıkları:
// şarj boyunca çevreden karaktere doğru çekilen kıvılcımlar (her adımda daha yoğun, adım renginde),
// bırakınca ayak hizasında genişleyen halka. Animator ile "idle değişimi" gelince bu bileşen silinecek.
[RequireComponent(typeof(BoostChargeAbility))]
public class ChargeTintPlaceholder : MonoBehaviour
{
    [Tooltip("Rengi değişecek görsel (şimdilik kapsülün MeshRenderer'ı).")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("Adım 0 (normal), 1, 2, 3 için renkler.")]
    [SerializeField] private Color[] stepColors = { Color.white, Color.yellow, new Color(1f, 0.5f, 0f), Color.red };

    [Header("Parçacıklar")]
    [Tooltip("Şarj boyunca saniyede çekilen kıvılcım sayısı: adım 0, 1, 2, 3. Her adımda daha yoğun.")]
    [SerializeField] private float[] pullRates = { 15f, 35f, 60f, 90f };

    [Tooltip("Kıvılcımların çıktığı kürenin yarıçapı (birim); buradan karakterin merkezine çekilirler.")]
    [SerializeField] private float pullRadius = 1.6f;

    [Tooltip("Kıvılcımların karaktere çekilme hızı (birim/sn).")]
    [SerializeField] private float pullSpeed = 5f;

    [Tooltip("Bırakınca halkadaki kıvılcım sayısı: adım 1, 2, 3.")]
    [SerializeField] private int[] releaseRingCounts = { 16, 28, 44 };

    [Tooltip("Bırakma halkasının genişleme hızı (birim/sn).")]
    [SerializeField] private float releaseRingSpeed = 8f;

    [Tooltip("Kıvılcımların boyutu (birim).")]
    [SerializeField] private float sparkSize = 0.1f;

    private BoostChargeAbility boost;
    private PlayerDeath death;
    private Material material;
    // Şarj yokken (adım 0) kullanılan renk; giyili kostüm bunu değiştirir
    private Color baseColor;

    private ParticleSystem sparks;
    private Material sparkMaterial;
    private float pullCarry;
    private bool wasCharging;
    private int lastStep;

    // Oyuncu merkezinden ayak hizasına (kapsül yüksekliği 2)
    private static readonly Vector3 FeetOffset = new Vector3(0f, -0.9f, 0f);

    private void Awake()
    {
        boost = GetComponent<BoostChargeAbility>();
        death = GetComponent<PlayerDeath>();
        // Kopya malzeme; paylaşılan malzemeyi kullanan diğer nesneler etkilenmez
        material = targetRenderer.material;
        baseColor = stepColors[0];
        CreateSparks();
    }

    // GEÇİCİ kostüm: karakterin normal rengi (bölüm başında LevelRun ayarlar)
    public void SetBaseColor(Color color)
    {
        baseColor = color;
        if (boost.CurrentStep == 0) material.color = color;
    }

    private void OnEnable()
    {
        boost.ChargeStepChanged += OnStepChanged;
    }

    private void OnDisable()
    {
        boost.ChargeStepChanged -= OnStepChanged;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (sparks != null) Destroy(sparks.gameObject);
        if (sparkMaterial != null) Destroy(sparkMaterial);
    }

    private void OnStepChanged(int step)
    {
        material.color = step == 0 ? baseColor : StepColor(step);
    }

    private Color StepColor(int step) => stepColors[Mathf.Clamp(step, 0, stepColors.Length - 1)];

    // Oyuncunun çocuğu değil: ölümde gizlenen görsellere dahil olmasın
    private void CreateSparks()
    {
        var go = new GameObject("ChargeSparks");
        sparks = go.AddComponent<ParticleSystem>();
        sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Konum, hız, renk ve ömür her kıvılcım için elle verilir (EmitParams)
        ParticleSystem.MainModule main = sparks.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;

        ParticleSystem.EmissionModule emission = sparks.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = sparks.shape;
        shape.enabled = false;

        // Saydamlık ayarı gerektirmesin diye sönme yerine küçülerek kaybolur
        ParticleSystem.SizeOverLifetimeModule size = sparks.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        sparkMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        renderer.sharedMaterial = sparkMaterial;
    }

    private void Update()
    {
        bool charging = boost.IsCharging;
        if (charging)
        {
            lastStep = boost.CurrentStep;
            EmitPull(lastStep);
        }
        else if (wasCharging && lastStep > 0 && !death.IsDead)
        {
            // Bırakıldı (patlama değil): ulaşılan adıma göre halka
            EmitReleaseRing(lastStep);
        }
        wasCharging = charging;
    }

    // Çevredeki bir küreden merkeze doğru; tam merkeze varınca ömrü biter
    private void EmitPull(int step)
    {
        if (pullRates.Length == 0 || pullSpeed <= 0f) return;
        pullCarry += pullRates[Mathf.Clamp(step, 0, pullRates.Length - 1)] * Time.deltaTime;
        int count = (int)pullCarry;
        pullCarry -= count;

        Color color = StepColor(step);
        Vector3 center = transform.position;
        for (int i = 0; i < count; i++)
        {
            // Kameraya bakan düzlemde görünsün diye derinlik (Z) dağılımı az tutulur
            Vector3 direction = Random.onUnitSphere;
            direction.z *= 0.3f;
            direction.Normalize();

            var emitParams = new ParticleSystem.EmitParams
            {
                position = center + direction * pullRadius,
                velocity = -direction * pullSpeed,
                startLifetime = pullRadius / pullSpeed,
                startSize = sparkSize,
                startColor = color
            };
            sparks.Emit(emitParams, 1);
        }
    }

    // Ayak hizasında ekran düzleminde (XY) eşit aralıkla dışarı açılan halka
    private void EmitReleaseRing(int step)
    {
        if (releaseRingCounts.Length == 0) return;
        int count = releaseRingCounts[Mathf.Clamp(step - 1, 0, releaseRingCounts.Length - 1)];
        Color color = StepColor(step);
        Vector3 feet = transform.position + FeetOffset;

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            var emitParams = new ParticleSystem.EmitParams
            {
                position = feet + direction * 0.2f,
                velocity = direction * releaseRingSpeed,
                startLifetime = 0.3f,
                startSize = sparkSize * 1.5f,
                startColor = color
            };
            sparks.Emit(emitParams, 1);
        }
    }
}
