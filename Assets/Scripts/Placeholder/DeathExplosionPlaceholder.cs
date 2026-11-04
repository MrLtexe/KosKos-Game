using UnityEngine;

// GEÇİCİ: teknolojik ölüm patlaması. Oyuncu öldüğü yerde:
// dönen veri küpleri (hızla saçılıp yavaşlar), çok hızlı ince elektrik çizgileri ve ekran düzleminde genişleyen halka.
// Parçacık sistemleri oyunda koddan kurulur (prefab'da ayar gerekmez). Gerçek efekt gelince bu bileşen silinecek.
// Ölümde oyuncunun tüm görselleri gizlendiği için patlama oyuncunun çocuğu değil, sahnede ayrı bir nesnedir.
[RequireComponent(typeof(PlayerDeath))]
public class DeathExplosionPlaceholder : MonoBehaviour
{
    [Header("Renkler")]
    [Tooltip("Patlamanın birinci rengi. Her parça iki renk arasından rastgele bir renk alır.")]
    [SerializeField] private Color colorA = new Color(0f, 0.55f, 0.65f);

    [Tooltip("Patlamanın ikinci rengi. Her parça iki renk arasından rastgele bir renk alır.")]
    [SerializeField] private Color colorB = new Color(0.4f, 0.22f, 0.65f);

    [Header("Veri Küpleri")]
    [Tooltip("Saçılan küp sayısı.")]
    [SerializeField] private int cubeCount = 24;

    [Tooltip("Küplerin çıkış hızı (en az, en çok; birim/sn).")]
    [SerializeField] private Vector2 cubeSpeed = new Vector2(4f, 10f);

    [Tooltip("Küplerin boyutu (en az, en çok; birim).")]
    [SerializeField] private Vector2 cubeSize = new Vector2(0.12f, 0.3f);

    [Tooltip("Küplerin ekranda kalma süresi (en az, en çok; sn); bu sürede küçülerek kaybolur.")]
    [SerializeField] private Vector2 cubeLifetime = new Vector2(0.5f, 0.9f);

    [Tooltip("Küplerin havada yavaşlaması. Büyük = hızla çıkıp çabuk durur (dijital his).")]
    [SerializeField] private float cubeDrag = 4f;

    [Header("Elektrik Çizgileri")]
    [Tooltip("Çıkan ince çizgi sayısı.")]
    [SerializeField] private int streakCount = 16;

    [Tooltip("Çizgilerin hızı (en az, en çok; birim/sn). Çizgi uzunluğu hızla artar.")]
    [SerializeField] private Vector2 streakSpeed = new Vector2(12f, 22f);

    [Tooltip("Çizgilerin ekranda kalma süresi (en az, en çok; sn).")]
    [SerializeField] private Vector2 streakLifetime = new Vector2(0.12f, 0.25f);

    [Header("Şok Halkası")]
    [Tooltip("Halkayı oluşturan nokta sayısı. 0 = halka yok.")]
    [SerializeField] private int ringCount = 48;

    [Tooltip("Halkanın genişleme hızı (birim/sn).")]
    [SerializeField] private float ringSpeed = 10f;

    [Tooltip("Halkanın ekranda kalma süresi (sn).")]
    [SerializeField] private float ringLifetime = 0.25f;

    private PlayerDeath death;
    private GameObject root;
    private ParticleSystem cubes;
    private ParticleSystem streaks;
    private ParticleSystem ring;
    private Material material;

    private void Awake()
    {
        death = GetComponent<PlayerDeath>();
        CreateParticles();
    }

    private void OnEnable()
    {
        death.Died += Explode;
    }

    private void OnDisable()
    {
        death.Died -= Explode;
    }

    private void OnDestroy()
    {
        if (root != null) Destroy(root);
        if (material != null) Destroy(material);
    }

    private void CreateParticles()
    {
        // Oyuncunun çocuğu değil: ölümde gizlenen görsellere dahil olmasın
        root = new GameObject("DeathExplosion");
        material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        // Veri küpleri: her yöne, dönerek; hava direnciyle hızla yavaşlar
        cubes = NewSystem("Cubes", cubeLifetime, cubeSpeed, cubeSize);
        ParticleSystem.MainModule cubeMain = cubes.main;
        cubeMain.startRotation3D = true;
        cubeMain.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        cubeMain.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        cubeMain.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        SetSphereShape(cubes, 0.3f);
        ParticleSystem.RotationOverLifetimeModule spin = cubes.rotationOverLifetime;
        spin.enabled = true;
        spin.separateAxes = true;
        spin.x = new ParticleSystem.MinMaxCurve(-10f, 10f);
        spin.y = new ParticleSystem.MinMaxCurve(-10f, 10f);
        spin.z = new ParticleSystem.MinMaxCurve(-10f, 10f);
        ParticleSystem.LimitVelocityOverLifetimeModule drag = cubes.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.limit = 1000f;
        drag.drag = cubeDrag;
        var cubeRenderer = cubes.GetComponent<ParticleSystemRenderer>();
        cubeRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        cubeRenderer.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

        // Elektrik çizgileri: çok hızlı, hız yönünde uzatılmış ince çizgiler
        streaks = NewSystem("Streaks", streakLifetime, streakSpeed, new Vector2(0.04f, 0.07f));
        SetSphereShape(streaks, 0.2f);
        var streakRenderer = streaks.GetComponent<ParticleSystemRenderer>();
        streakRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        streakRenderer.velocityScale = 0.04f;
        streakRenderer.lengthScale = 1f;

        // Şok halkası: ekran düzleminde (XY) bir çemberin kenarından eşit aralıkla dışarı
        ring = NewSystem("Ring", new Vector2(ringLifetime, ringLifetime), new Vector2(ringSpeed, ringSpeed), new Vector2(0.12f, 0.12f));
        ParticleSystem.ShapeModule ringShape = ring.shape;
        ringShape.enabled = true;
        ringShape.shapeType = ParticleSystemShapeType.Circle;
        ringShape.radius = 0.2f;
        ringShape.radiusThickness = 0f;
        ringShape.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread;
    }

    // Ortak ayarlar: dünya uzayı, elle Emit, küçülerek kaybolma, ışıksız malzeme
    private ParticleSystem NewSystem(string name, Vector2 lifetime, Vector2 speed, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);

        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.maxParticles = 200;

        // Parçalar elle (Emit) çıkarılır
        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;

        // Saydamlık ayarı gerektirmesin diye sönme yerine küçülerek kaybolur
        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = material;
        return system;
    }

    private static void SetSphereShape(ParticleSystem system, float radius)
    {
        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
    }

    private void Explode()
    {
        root.transform.position = transform.position;
        cubes.Emit(cubeCount);
        streaks.Emit(streakCount);
        if (ringCount > 0) ring.Emit(ringCount);
    }
}
