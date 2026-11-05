using UnityEngine;
using UnityEngine.Rendering;

// GEÇİCİ: atılma parçacıkları. Atılma başında kısa bir patlama, atılma boyunca arkada kalan ince hız çizgileri.
// Sıyrılganlıklı atılmayla bir duvarın ya da düşman mermisinin içinden geçerken karakter kısa süre yarı saydam olur.
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

    [Header("Sıyrılganlık")]
    [Tooltip("Duvarın ya da merminin içinden geçerken karakterin görünürlüğü (0 = görünmez, 1 = tam opak).")]
    [Range(0f, 1f)]
    [SerializeField] private float phaseOpacity = 0.35f;

    private DashAbility dash;
    private ParticleSystem particles;
    private Material material;
    private bool wasDashing;
    // Saniyedeki çizgi sayısının küsuratı bir sonraki kareye taşınır
    private float emitCarry;

    // Karakterin gövde görseli (kapsül); renk/kostüm de aynı malzeme kopyasını kullanır
    private Renderer bodyRenderer;
    private bool isTransparent;

    private void Awake()
    {
        dash = GetComponent<DashAbility>();
        bodyRenderer = GetComponent<Renderer>();
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
        bool phasing = dash.IsPhasingThrough;
        if (phasing != isTransparent) SetTransparent(phasing);

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

    // URP Lit malzemesini saydam/opak arasında çevirir; renk (kostüm, şarj rengi) korunur, sadece alfa değişir
    private void SetTransparent(bool on)
    {
        isTransparent = on;
        if (bodyRenderer == null) return;

        // .material: karakterin kendi kopyası (ChargeTintPlaceholder ile aynı kopya)
        Material body = bodyRenderer.material;
        if (on)
        {
            body.SetFloat("_Surface", 1f);
            body.SetFloat("_Blend", 0f);
            body.SetOverrideTag("RenderType", "Transparent");
            body.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            body.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            body.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            body.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            body.SetFloat("_ZWrite", 0f);
            body.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            body.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            body.SetFloat("_Surface", 0f);
            body.SetOverrideTag("RenderType", "Opaque");
            body.SetFloat("_SrcBlend", (float)BlendMode.One);
            body.SetFloat("_DstBlend", (float)BlendMode.Zero);
            body.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            body.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
            body.SetFloat("_ZWrite", 1f);
            body.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            // -1: shader'ın kendi sırası (opak)
            body.renderQueue = -1;
        }

        Color color = body.color;
        color.a = on ? phaseOpacity : 1f;
        body.color = color;
    }
}
