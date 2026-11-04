using UnityEngine;

// GEÇİCİ: atılma parçacıkları. Atılma başında kısa bir patlama, atılma boyunca arkada kalan ince hız çizgileri.
// Parçacık sistemi oyunda koddan kurulur (prefab'da ayar gerekmez). Gerçek efekt gelince bu bileşen silinecek.
[RequireComponent(typeof(DashAbility))]
public class DashTrailPlaceholder : MonoBehaviour
{
    [Tooltip("Hız çizgilerinin birinci rengi. Her çizgi iki renk arasından rastgele bir renk alır.")]
    [SerializeField] private Color streakColorA = new Color(0f, 1f, 1f);

    [Tooltip("Hız çizgilerinin ikinci rengi. Her çizgi iki renk arasından rastgele bir renk alır.")]
    [SerializeField] private Color streakColorB = new Color(0.8f, 0.6f, 1f);

    [Tooltip("Atılma başında bir anda çıkan çizgi sayısı.")]
    [SerializeField] private int burstCount = 10;

    [Tooltip("Atılma boyunca saniyede çıkan çizgi sayısı.")]
    [SerializeField] private float emitRate = 120f;

    [Tooltip("Bir çizginin ekranda kalma süresi (sn); bu sürede küçülerek kaybolur.")]
    [SerializeField] private float lifetime = 0.25f;

    [Tooltip("Çizginin uzunluğu (birim).")]
    [SerializeField] private float streakLength = 0.8f;

    [Tooltip("Çizginin kalınlığı (birim).")]
    [SerializeField] private float streakThickness = 0.06f;

    private DashAbility dash;
    private ParticleSystem particles;
    private Material material;
    private bool wasDashing;
    // Saniyedeki çizgi sayısının küsuratı bir sonraki kareye taşınır
    private float emitCarry;

    private void Awake()
    {
        dash = GetComponent<DashAbility>();
        CreateParticles();
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    private void CreateParticles()
    {
        // Karakterin hemen arkasında (kameradan uzakta) duran boş nesne
        var go = new GameObject("DashTrail");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 0.3f);

        particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Dünya uzayında: çizgiler çıktıkları yerde kalır, karakter ilerledikçe arkasında iz bırakır
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize3D = true;
        main.startSizeX = streakLength;
        main.startSizeY = streakThickness;
        main.startSizeZ = 1f;
        main.startColor = new ParticleSystem.MinMaxGradient(streakColorA, streakColorB);
        main.maxParticles = 200;

        // Çizgiler elle (Emit) çıkarılır
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;

        // Karakterin boyu kadar dikey bir alandan rastgele çıkar
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.4f, 1.6f, 0.1f);

        // Saydamlık ayarı gerektirmesin diye sönme yerine küçülerek kaybolur
        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        renderer.sharedMaterial = material;
    }

    private void Update()
    {
        bool dashing = dash.IsDashing;
        if (dashing)
        {
            if (!wasDashing)
            {
                particles.Emit(burstCount);
                emitCarry = 0f;
            }

            emitCarry += emitRate * Time.deltaTime;
            int count = (int)emitCarry;
            emitCarry -= count;
            if (count > 0) particles.Emit(count);
        }
        wasDashing = dashing;
    }
}
