using UnityEngine;

// GEÇİCİ: kanca ipi (oyuncudan noktaya düz çizgi; fırlatılınca ucu noktaya doğru uçar), sallanırken açı göstergesi
// ve kanca noktaya tutununca noktada kıvılcım. Gerçek görsel gelince bu bileşen silinecek.
[RequireComponent(typeof(HookAbility))]
public class HookRopePlaceholder : MonoBehaviour
{
    [Tooltip("İp olarak çizilen çizgi.")]
    [SerializeField] private LineRenderer rope;

    [Tooltip("İpin kalınlığı (birim).")]
    [SerializeField] private float ropeWidth = 0.05f;

    [Tooltip("İpin rengi.")]
    [SerializeField] private Color ropeColor = new Color(0.6f, 0.6f, 0.6f);

    [Tooltip("Sallanırken açı göstergesini (ör. 45° / 90°) karakterin üstünde göster.")]
    [SerializeField] private bool showSwingAngle = true;

    [Tooltip("Açı patlama sınırına bu kadar (derece) yaklaşınca gösterge kırmızıya döner.")]
    [SerializeField] private float angleWarningMargin = 15f;

    [Tooltip("Açı göstergesinin arka plan rengi.")]
    [SerializeField] private Color angleBackgroundColor = new Color(0.08f, 0.08f, 0.1f);

    [Tooltip("Açı göstergesinin arka plan boyutu (genişlik, yükseklik; birim).")]
    [SerializeField] private Vector2 angleBackgroundSize = new Vector2(1.5f, 0.4f);

    [Header("Tutunma Kıvılcımı")]
    [Tooltip("Kanca noktaya tutununca çıkan kıvılcım sayısı. 0 = kıvılcım yok.")]
    [SerializeField] private int attachSparkCount = 14;

    [Tooltip("Kıvılcımların birinci rengi (iki renk arasından rastgele).")]
    [SerializeField] private Color attachSparkColorA = new Color(1f, 0.85f, 0.4f);

    [Tooltip("Kıvılcımların ikinci rengi (iki renk arasından rastgele).")]
    [SerializeField] private Color attachSparkColorB = Color.white;

    private HookAbility hook;
    private Material material;
    private Material backgroundMaterial;
    // Tutunma kıvılcımı: oyuncunun çocuğu değil (noktada kalır, ölümde gizlenen görsellere dahil olmaz)
    private ParticleSystem attachSparks;
    private Material attachSparkMaterial;
    // Açı yazısı oyunda oluşturulur (prefab değişmesin diye)
    private TextMesh angleText;
    private int shownAngle = int.MinValue;

    private void Awake()
    {
        hook = GetComponent<HookAbility>();
        rope.positionCount = 2;
        rope.useWorldSpace = true;
        rope.startWidth = ropeWidth;
        rope.endWidth = ropeWidth;
        rope.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rope.receiveShadows = false;
        // Işıktan etkilenmeyen düz renk malzeme
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.color = ropeColor;
        rope.sharedMaterial = material;
        rope.enabled = false;

        if (showSwingAngle) CreateAngleText();
        CreateAttachSparks();
    }

    private void OnEnable()
    {
        hook.Attached += OnAttached;
    }

    private void OnDisable()
    {
        hook.Attached -= OnAttached;
    }

    private void CreateAttachSparks()
    {
        var go = new GameObject("HookAttachSparks");
        attachSparks = go.AddComponent<ParticleSystem>();
        attachSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Konum, hız, renk ve ömür her kıvılcıma elle verilir (EmitParams)
        ParticleSystem.MainModule main = attachSparks.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;

        ParticleSystem.EmissionModule emission = attachSparks.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = attachSparks.shape;
        shape.enabled = false;

        // Saydamlık ayarı gerektirmesin diye sönme yerine küçülerek kaybolur
        ParticleSystem.SizeOverLifetimeModule size = attachSparks.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Hız yönünde uzatılmış ince çizgiler: kıvılcım gibi görünür
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.04f;
        renderer.lengthScale = 1f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        attachSparkMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        renderer.sharedMaterial = attachSparkMaterial;
    }

    // Kanca noktaya tutundu: noktadan her yöne kısa kıvılcım patlaması
    private void OnAttached(Vector3 point)
    {
        for (int i = 0; i < attachSparkCount; i++)
        {
            Vector3 direction = Random.onUnitSphere;
            direction.z *= 0.2f;
            direction.Normalize();

            var emitParams = new ParticleSystem.EmitParams
            {
                position = point,
                velocity = direction * Random.Range(5f, 10f),
                startSize = Random.Range(0.05f, 0.1f),
                startLifetime = Random.Range(0.12f, 0.25f),
                startColor = Color.Lerp(attachSparkColorA, attachSparkColorB, Random.value)
            };
            attachSparks.Emit(emitParams, 1);
        }
    }

    // Karakterin üstünde, kameraya doğru hafif önde basit 3B yazı (sapan ipucuyla aynı yer ve stil)
    private void CreateAngleText()
    {
        var go = new GameObject("SwingAngle");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 1.8f, -0.6f);

        angleText = go.AddComponent<TextMesh>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        angleText.font = font;
        angleText.anchor = TextAnchor.MiddleCenter;
        angleText.alignment = TextAlignment.Center;
        angleText.fontSize = 48;
        angleText.characterSize = 0.05f;
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;

        // Arka plan: yazının hemen arkasında düz renk dörtgen (yazıyla birlikte açılıp kapanır)
        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "Background";
        Destroy(background.GetComponent<Collider>());
        background.transform.SetParent(go.transform, false);
        background.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        background.transform.localScale = new Vector3(angleBackgroundSize.x, angleBackgroundSize.y, 1f);
        var backgroundRenderer = background.GetComponent<Renderer>();
        backgroundRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        backgroundRenderer.receiveShadows = false;
        backgroundMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        backgroundMaterial.color = angleBackgroundColor;
        backgroundRenderer.sharedMaterial = backgroundMaterial;

        go.SetActive(false);
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (backgroundMaterial != null) Destroy(backgroundMaterial);
        if (attachSparks != null) Destroy(attachSparks.gameObject);
        if (attachSparkMaterial != null) Destroy(attachSparkMaterial);
    }

    private void LateUpdate()
    {
        bool firing = hook.IsFiring;
        bool show = hook.IsHooked || firing;
        if (rope.enabled != show) rope.enabled = show;
        RefreshAngleText();
        if (!show) return;

        // Uçarken ipin ucu oyuncudan noktaya doğru ilerler; tutununca noktadadır
        Vector3 end = firing
            ? Vector3.Lerp(transform.position, hook.FireTargetPosition, hook.FireProgress01)
            : hook.AnchorPosition;
        rope.SetPosition(0, transform.position);
        rope.SetPosition(1, end);
    }

    private void RefreshAngleText()
    {
        if (angleText == null) return;

        bool show = hook.IsSwinging;
        if (angleText.gameObject.activeSelf != show) angleText.gameObject.SetActive(show);
        if (!show) return;

        // Yazı sadece tam derece değişince yenilenir
        int degrees = Mathf.RoundToInt(hook.SwingAngleDegrees);
        if (degrees == shownAngle) return;
        shownAngle = degrees;

        int limit = Mathf.RoundToInt(hook.MaxSwingAngle);
        angleText.text = $"{degrees}° / {limit}°";
        angleText.color = degrees >= limit - angleWarningMargin ? Color.red : Color.white;
    }
}
