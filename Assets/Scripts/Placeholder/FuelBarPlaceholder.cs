using UnityEngine;

// GEÇİCİ: jet-çanta yakıt çubuğu (karakterin üstünde, depo dolu değilken görünür) ve buhar motoru parçacıkları:
// itiş boyunca sırttan aşağı-geriye buhar bulutları (büyüyerek dağılır) ve turuncu kıvılcımlar,
// itiş başlarken büyük buhar patlaması, bitince küçük "pıss" bulutu. Gerçek arayüz/efekt gelince silinecek.
[RequireComponent(typeof(JetBagAbility))]
public class FuelBarPlaceholder : MonoBehaviour
{
    [Tooltip("Çubuğun kök nesnesi; sadece depo dolu değilken açık.")]
    [SerializeField] private GameObject barRoot;

    [Tooltip("Dolu kısmı gösteren görsel; yakıt azaldıkça sağdan kısalır.")]
    [SerializeField] private Transform fill;

    [Tooltip("Çubuğun tam genişliği (birim).")]
    [SerializeField] private float barWidth = 1.2f;

    [Header("Buhar")]
    [Tooltip("Buharın çıktığı nokta (karaktere göre; X negatif = sırt, Y negatif = aşağı).")]
    [SerializeField] private Vector3 nozzleOffset = new Vector3(-0.4f, -0.2f, 0f);

    [Tooltip("İtiş boyunca saniyede çıkan buhar bulutu sayısı.")]
    [SerializeField] private float steamRate = 45f;

    [Tooltip("Buhar bulutlarının renkleri (iki renk arasından rastgele).")]
    [SerializeField] private Color steamColorA = new Color(0.95f, 0.95f, 0.95f);

    [Tooltip("Buhar bulutlarının renkleri (iki renk arasından rastgele).")]
    [SerializeField] private Color steamColorB = new Color(0.65f, 0.65f, 0.68f);

    [Tooltip("Buharın çıkış hızı (en az, en çok; birim/sn).")]
    [SerializeField] private Vector2 steamSpeed = new Vector2(3f, 6f);

    [Tooltip("Buhar bulutunun en büyük boyutu (birim).")]
    [SerializeField] private float steamSize = 0.7f;

    [Tooltip("İtiş başlarken çıkan buhar patlaması bulut sayısı.")]
    [SerializeField] private int startPuffCount = 14;

    [Tooltip("İtiş bitince çıkan küçük buhar bulutu sayısı.")]
    [SerializeField] private int stopPuffCount = 6;

    [Header("Kıvılcım")]
    [Tooltip("İtiş boyunca saniyede çıkan kıvılcım sayısı. 0 = kıvılcım yok.")]
    [SerializeField] private float sparkRate = 18f;

    [Tooltip("Kıvılcım renkleri (iki renk arasından rastgele).")]
    [SerializeField] private Color sparkColorA = new Color(1f, 0.6f, 0.15f);

    [Tooltip("Kıvılcım renkleri (iki renk arasından rastgele).")]
    [SerializeField] private Color sparkColorB = new Color(1f, 0.85f, 0.35f);

    // Dolu kısım arka planın önünde kalsın
    private const float FillDepthOffset = -0.01f;

    private JetBagAbility jetBag;

    private GameObject particleRoot;
    private ParticleSystem steam;
    private ParticleSystem sparks;
    private Material particleMaterial;
    private bool wasThrusting;
    private float steamCarry;
    private float sparkCarry;

    private void Awake()
    {
        jetBag = GetComponent<JetBagAbility>();
        barRoot.SetActive(false);
        CreateParticles();
    }

    private void OnDestroy()
    {
        if (particleRoot != null) Destroy(particleRoot);
        if (particleMaterial != null) Destroy(particleMaterial);
    }

    private void LateUpdate()
    {
        UpdateBar();
        UpdateParticles();
    }

    private void UpdateBar()
    {
        float fuel = jetBag.Fuel01;
        bool show = fuel < 1f;
        if (barRoot.activeSelf != show) barRoot.SetActive(show);
        if (!show) return;

        // Dolu kısım sol kenara yaslı kalır
        fill.localScale = new Vector3(barWidth * fuel, fill.localScale.y, 1f);
        fill.localPosition = new Vector3(-barWidth * (1f - fuel) * 0.5f, 0f, FillDepthOffset);
    }

    // Oyuncunun çocuğu değil: ölümde gizlenen görsellere dahil olmasın, bulutlar havada geride kalsın
    private void CreateParticles()
    {
        particleRoot = new GameObject("JetSteam");
        particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        // Buhar: büyüyerek dağılır, hafifçe yükselir ve havada yavaşlar
        steam = NewSystem("Steam", 0.5f, 1f, steamColorA, steamColorB);
        ParticleSystem.MainModule steamMain = steam.main;
        steamMain.gravityModifier = -0.15f;
        steamMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        ParticleSystem.SizeOverLifetimeModule steamSizeCurve = steam.sizeOverLifetime;
        steamSizeCurve.enabled = true;
        // Saydamlık ayarı gerektirmesin diye: küçük çıkar, büyür, sonunda küçülerek kaybolur
        steamSizeCurve.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.25f), new Keyframe(0.6f, 1f), new Keyframe(1f, 0f)));
        ParticleSystem.LimitVelocityOverLifetimeModule steamDrag = steam.limitVelocityOverLifetime;
        steamDrag.enabled = true;
        steamDrag.limit = 1000f;
        steamDrag.drag = 3f;

        // Kıvılcım: küçük, hızlı, yerçekimiyle düşer ve küçülerek söner
        sparks = NewSystem("Sparks", 0.2f, 0.4f, sparkColorA, sparkColorB);
        ParticleSystem.MainModule sparkMain = sparks.main;
        sparkMain.gravityModifier = 1f;
        ParticleSystem.SizeOverLifetimeModule sparkSizeCurve = sparks.sizeOverLifetime;
        sparkSizeCurve.enabled = true;
        sparkSizeCurve.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
    }

    // Ortak ayarlar: dünya uzayı, elle Emit (konum ve hız her parçacığa verilir), ışıksız malzeme
    private ParticleSystem NewSystem(string name, float minLifetime, float maxLifetime, Color colorA, Color colorB)
    {
        var go = new GameObject(name);
        go.transform.SetParent(particleRoot.transform, false);

        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.maxParticles = 300;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = false;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = particleMaterial;
        return system;
    }

    private void UpdateParticles()
    {
        bool thrusting = jetBag.IsThrusting;
        if (thrusting && !wasThrusting)
        {
            EmitSteam(startPuffCount, 1.4f);
            steamCarry = sparkCarry = 0f;
        }
        else if (!thrusting && wasThrusting)
        {
            EmitSteam(stopPuffCount, 0.6f);
        }

        if (thrusting)
        {
            steamCarry += steamRate * Time.deltaTime;
            int steamCount = (int)steamCarry;
            steamCarry -= steamCount;
            EmitSteam(steamCount, 1f);

            sparkCarry += sparkRate * Time.deltaTime;
            int sparkCount = (int)sparkCarry;
            sparkCarry -= sparkCount;
            EmitSparks(sparkCount);
        }
        wasThrusting = thrusting;
    }

    // Aşağı ve biraz geriye (karakterin dönüşüne göre: tavanda ters çevrilince de sırttan çıkar)
    private Vector3 ExhaustDirection()
    {
        Vector3 direction = -transform.up + -transform.right * Random.Range(0.1f, 0.5f);
        direction += Random.insideUnitSphere * 0.25f;
        direction.z *= 0.3f;
        return direction.normalized;
    }

    private void EmitSteam(int count, float sizeScale)
    {
        Vector3 nozzle = transform.TransformPoint(nozzleOffset);
        for (int i = 0; i < count; i++)
        {
            var emitParams = new ParticleSystem.EmitParams
            {
                position = nozzle + Random.insideUnitSphere * 0.1f,
                velocity = ExhaustDirection() * Random.Range(steamSpeed.x, steamSpeed.y),
                startSize = steamSize * sizeScale * Random.Range(0.7f, 1.2f)
            };
            steam.Emit(emitParams, 1);
        }
    }

    private void EmitSparks(int count)
    {
        Vector3 nozzle = transform.TransformPoint(nozzleOffset);
        for (int i = 0; i < count; i++)
        {
            var emitParams = new ParticleSystem.EmitParams
            {
                position = nozzle,
                velocity = ExhaustDirection() * Random.Range(5f, 9f),
                startSize = Random.Range(0.05f, 0.1f)
            };
            sparks.Emit(emitParams, 1);
        }
    }
}
