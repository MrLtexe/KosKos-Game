using UnityEngine;

// GEÇİCİ: manyetik bot efektleri.
// Botlar tavana/duvara gerçekten tutununca ayak hizasında yatay açılan mavi halka (gösterge; tutunulamayan Q basışında çıkmaz).
// Tavana tutunulduktan sonra oyuncu tavana çekilirken, tavana değene kadar oyuncudan tavana doğru akan manyetik
// kıvılcımlar çıkar; tavanda koşarken çıkmaz. Parçacık sistemi oyunda koddan kurulur. Gerçek efekt gelince bu bileşen silinecek.
[RequireComponent(typeof(PlayerMotor), typeof(MagBootsAbility))]
public class MagBootsFxPlaceholder : MonoBehaviour
{
    [Header("Tutunma Halkası")]
    [Tooltip("Botlar tutununca ayakta açılan halkanın parçacık sayısı.")]
    [SerializeField] private int pressRingCount = 20;

    [Tooltip("Halkanın açılma hızı (birim/sn).")]
    [SerializeField] private float pressRingSpeed = 4f;

    [Tooltip("Halkanın rengi.")]
    [SerializeField] private Color pressColor = new Color(0.3f, 0.7f, 1f);

    [Header("Tavana Çekilme")]
    [Tooltip("Tavana çekilirken saniyede çıkan manyetik kıvılcım sayısı. 0 = kapalı.")]
    [SerializeField] private float pullSparkRate = 70f;

    [Tooltip("Manyetik kıvılcımların birinci rengi (iki renk arasından rastgele).")]
    [SerializeField] private Color sparkColorA = new Color(0.35f, 0.75f, 1f);

    [Tooltip("Manyetik kıvılcımların ikinci rengi (iki renk arasından rastgele).")]
    [SerializeField] private Color sparkColorB = new Color(0.85f, 0.95f, 1f);

    // Karakter merkezinden ayak hizasına (kapsül yüksekliği 2), karakterin kendi uzayında
    private static readonly Vector3 FeetLocal = new Vector3(0f, -0.9f, 0f);
    // Kıvılcımlar kapsülün ve blokların önünde (kameraya doğru) çizilsin; yoksa onların içinde kalıp görünmez
    private const float SparkDepth = -0.7f;
    // Tavanın aranacağı en uzak mesafe (botların tavan menziliyle aynı)
    private const float CeilingSearchRange = 8f;

    private PlayerMotor motor;
    private MagBootsAbility magBoots;
    private ParticleSystem particles;
    private Material material;
    private float sparkCarry;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        magBoots = GetComponent<MagBootsAbility>();
        CreateParticles();
    }

    private void OnEnable()
    {
        magBoots.Activated += OnActivated;
    }

    private void OnDisable()
    {
        magBoots.Activated -= OnActivated;
    }

    private void OnDestroy()
    {
        if (particles != null) Destroy(particles.gameObject);
        if (material != null) Destroy(material);
    }

    // Oyuncunun çocuğu değil: ölümde gizlenen görsellere dahil olmasın, kıvılcımlar dünyada kalsın
    private void CreateParticles()
    {
        var go = new GameObject("MagBootsFx");
        particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Konum, hız, boyut, renk ve ömür her parçacığa elle verilir (EmitParams)
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = false;

        // Saydamlık ayarı gerektirmesin diye sönme yerine küçülerek kaybolur
        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Hız yönünde uzatılmış ince çizgiler: elektrik/manyetik kıvılcım gibi
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.05f;
        renderer.lengthScale = 1f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        renderer.sharedMaterial = material;
    }

    // Tutunma göstergesi: ayak hizasında, karakterin "yere paralel" düzleminde açılan halka
    // (tavandayken de ayak tarafında, tavana paralel)
    private void OnActivated()
    {
        Vector3 feet = transform.TransformPoint(FeetLocal);
        for (int i = 0; i < pressRingCount; i++)
        {
            float angle = i * Mathf.PI * 2f / pressRingCount;
            Vector3 direction = transform.TransformDirection(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            var emitParams = new ParticleSystem.EmitParams
            {
                position = feet + direction * 0.2f,
                velocity = direction * pressRingSpeed,
                startSize = 0.08f,
                startLifetime = 0.22f,
                startColor = pressColor
            };
            particles.Emit(emitParams, 1);
        }
    }

    private void Update()
    {
        UpdatePullSparks();
    }

    // Tavan modu açık ama henüz tavana değmedik (çekiliyoruz): oyuncudan tavana doğru akan kıvılcımlar.
    // Tavana değince (tavan modunda "yer" tavandır) durur
    private void UpdatePullSparks()
    {
        bool pulling = motor.IsCeilingMode && !motor.IsGrounded;
        if (!pulling || pullSparkRate <= 0f || Time.deltaTime <= 0f)
        {
            sparkCarry = 0f;
            return;
        }

        // Tavanın yüksekliği: oyuncunun tam üstündeki ilk zemin yüzeyi
        Vector3 center = motor.Position;
        if (!Physics.Raycast(center, Vector3.up, out RaycastHit hit, CeilingSearchRange, motor.GroundLayers,
                QueryTriggerInteraction.Ignore)) return;

        sparkCarry += pullSparkRate * Time.deltaTime;
        int count = (int)sparkCarry;
        sparkCarry -= count;

        Vector3 depth = new Vector3(0f, 0f, SparkDepth);
        // Oyuncu koşmaya devam ediyor: kıvılcımlar onun yatay hızını da taşır, böylece arada kalırlar
        var carry = new Vector3(motor.Velocity.x, 0f, 0f);
        float ceilingY = hit.point.y;
        for (int i = 0; i < count; i++)
        {
            // Oyuncunun gövdesi boyunca rastgele bir yerden tavana doğru
            Vector3 start = center + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.8f, 0.8f), 0f) + depth;
            float gap = ceilingY - start.y;
            if (gap < 0.1f) continue;

            float speed = Random.Range(8f, 14f);
            var emitParams = new ParticleSystem.EmitParams
            {
                position = start,
                velocity = Vector3.up * speed + carry,
                startSize = Random.Range(0.05f, 0.09f),
                // Tavana varınca söner
                startLifetime = gap / speed,
                startColor = Color.Lerp(sparkColorA, sparkColorB, Random.value)
            };
            particles.Emit(emitParams, 1);
        }
    }
}
