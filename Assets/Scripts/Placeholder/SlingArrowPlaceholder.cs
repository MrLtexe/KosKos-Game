using UnityEngine;

// GEÇİCİ: sapan nişan oku (çizgi), "<zıplama tuşu>: launch" ipucu, alanın topunun etrafında küçülen geri sayım halkası
// ve halka küçülüp otomatik fırlatınca patlama parçacıkları. Gerçek görsel gelince bu bileşen silinecek.
[RequireComponent(typeof(SlingAbility), typeof(PlayerInputReader))]
public class SlingArrowPlaceholder : MonoBehaviour
{
    [Tooltip("Ok olarak çizilen çizgi.")]
    [SerializeField] private LineRenderer arrow;

    [Tooltip("Fırlatma tuşu ipucu nesnesi (TextMesh); sadece sapan sırasında açık. Yazısı güncel zıplama tuşundan alınır.")]
    [SerializeField] private GameObject hint;

    [Tooltip("Okun uzunluğu (birim).")]
    [SerializeField] private float arrowLength = 1.5f;

    [Tooltip("Okun kalınlığı (birim).")]
    [SerializeField] private float arrowWidth = 0.12f;

    [Tooltip("Okun rengi.")]
    [SerializeField] private Color arrowColor = new Color(0.3f, 0.9f, 1f);

    [Header("Geri Sayım Halkası")]
    [Tooltip("Halkanın başlangıçta alanın topundan ne kadar dışarıda olduğu (birim). Süre boyunca topa kadar küçülür.")]
    [SerializeField] private float ringGap = 1f;

    [Tooltip("Halka çizgisinin kalınlığı (birim).")]
    [SerializeField] private float ringWidth = 0.06f;

    [Tooltip("Halkanın rengi.")]
    [SerializeField] private Color ringColor = new Color(0.85f, 0.4f, 0.95f);

    [Header("Patlama")]
    [Tooltip("Halka küçülüp otomatik fırlatınca her yöne saçılan parçacık sayısı.")]
    [SerializeField] private int burstCount = 30;

    [Tooltip("Patlamadaki genişleyen halkanın parçacık sayısı.")]
    [SerializeField] private int burstRingCount = 32;

    [Tooltip("Patlama parçacıklarının ikinci rengi (birincisi halka rengi).")]
    [SerializeField] private Color burstColor = Color.white;

    // Halkadaki nokta sayısı (fazlası = daha yuvarlak)
    private const int RingSegments = 48;
    // Alanın topunun yarıçapı bulunamazsa
    private const float DefaultInnerRadius = 0.6f;

    private SlingAbility sling;
    private PlayerInputReader input;
    private TextMesh hintText;
    private Material material;
    private Vector3 direction = Vector3.right;

    // Halka ve patlama oyuncunun çocuğu değil: alanın merkezinde durur, ölümde gizlenen görsellere dahil olmaz
    private GameObject effectsRoot;
    private LineRenderer ring;
    private Material ringMaterial;
    private ParticleSystem burst;
    private Material burstMaterial;
    private Vector3 ringCenter;
    private float innerRadius;

    private void Awake()
    {
        sling = GetComponent<SlingAbility>();
        input = GetComponent<PlayerInputReader>();
        hintText = hint.GetComponent<TextMesh>();
        arrow.positionCount = 2;
        arrow.useWorldSpace = true;
        arrow.startWidth = arrowWidth;
        arrow.endWidth = 0f;
        arrow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        arrow.receiveShadows = false;
        // Işıktan etkilenmeyen düz renk malzeme
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.color = arrowColor;
        arrow.sharedMaterial = material;
        CreateEffects();
        SetVisible(false);
    }

    private void OnEnable()
    {
        sling.SlingActiveChanged += SetVisible;
        sling.AimChanged += OnAimChanged;
        sling.Launched += OnLaunched;
    }

    private void OnDisable()
    {
        sling.SlingActiveChanged -= SetVisible;
        sling.AimChanged -= OnAimChanged;
        sling.Launched -= OnLaunched;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (effectsRoot != null) Destroy(effectsRoot);
        if (ringMaterial != null) Destroy(ringMaterial);
        if (burstMaterial != null) Destroy(burstMaterial);
    }

    private void CreateEffects()
    {
        effectsRoot = new GameObject("SlingEffects");

        // Geri sayım halkası: kapalı çizgi
        var ringObject = new GameObject("CountdownRing");
        ringObject.transform.SetParent(effectsRoot.transform, false);
        ring = ringObject.AddComponent<LineRenderer>();
        ring.positionCount = RingSegments;
        ring.loop = true;
        ring.useWorldSpace = true;
        ring.startWidth = ringWidth;
        ring.endWidth = ringWidth;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows = false;
        ringMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        ringMaterial.color = ringColor;
        ring.sharedMaterial = ringMaterial;
        ring.enabled = false;

        // Patlama: konum, hız, renk ve ömür her parçacığa elle verilir (EmitParams)
        var burstObject = new GameObject("LaunchBurst");
        burstObject.transform.SetParent(effectsRoot.transform, false);
        burst = burstObject.AddComponent<ParticleSystem>();
        burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = burst.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        ParticleSystem.EmissionModule emission = burst.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = burst.shape;
        shape.enabled = false;
        // Saydamlık ayarı gerektirmesin diye sönme yerine küçülerek kaybolur
        ParticleSystem.SizeOverLifetimeModule size = burst.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        // Hızla çıkıp yavaşlar
        ParticleSystem.LimitVelocityOverLifetimeModule drag = burst.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.limit = 1000f;
        drag.drag = 3f;
        var burstRenderer = burstObject.GetComponent<ParticleSystemRenderer>();
        burstRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        burstRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        burstRenderer.receiveShadows = false;
        burstMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        burstRenderer.sharedMaterial = burstMaterial;
    }

    private void LateUpdate()
    {
        UpdateRing();
        if (!arrow.enabled) return;
        Vector3 origin = transform.position;
        arrow.SetPosition(0, origin);
        arrow.SetPosition(1, origin + direction * arrowLength);
    }

    // Halka, topun dışından (ringGap kadar) başlayıp süre boyunca topun kenarına kadar küçülür
    private void UpdateRing()
    {
        if (!ring.enabled) return;

        float radius = innerRadius + ringGap * (1f - sling.SlowProgress01);
        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / RingSegments;
            ring.SetPosition(i, ringCenter + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
    }

    // Halkanın merkezi ve iç yarıçapı: alanın topu (görseli) varsa ondan, yoksa alanın merkezi ve varsayılan boyut
    private void CaptureZoneShape(SlingZone zone)
    {
        ringCenter = zone.transform.position;
        innerRadius = DefaultInnerRadius;
        Renderer ball = zone.GetComponentInChildren<Renderer>();
        if (ball != null)
        {
            ringCenter = ball.bounds.center;
            innerRadius = ball.bounds.extents.x;
        }
    }

    // Otomatik fırlatma (halka topa ulaştı): alanın merkezinde patlama. Tuşla erken fırlatmada patlama yok
    private void OnLaunched(bool automatic)
    {
        if (!automatic) return;

        for (int i = 0; i < burstCount; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            dir.z *= 0.3f;
            dir.Normalize();
            var emitParams = new ParticleSystem.EmitParams
            {
                position = ringCenter + dir * innerRadius,
                velocity = dir * Random.Range(6f, 12f),
                startSize = Random.Range(0.1f, 0.22f),
                startLifetime = Random.Range(0.3f, 0.55f),
                startColor = Color.Lerp(ringColor, burstColor, Random.value)
            };
            burst.Emit(emitParams, 1);
        }

        // Halkanın bittiği yerden dışarı açılan şok halkası
        for (int i = 0; i < burstRingCount; i++)
        {
            float angle = i * Mathf.PI * 2f / burstRingCount;
            var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            var emitParams = new ParticleSystem.EmitParams
            {
                position = ringCenter + dir * innerRadius,
                velocity = dir * 9f,
                startSize = 0.12f,
                startLifetime = 0.3f,
                startColor = ringColor
            };
            burst.Emit(emitParams, 1);
        }
    }

    private void OnAimChanged(Vector3 aim)
    {
        direction = aim;
    }

    private void SetVisible(bool visible)
    {
        // Tuş ataması değişmiş olabilir; ipucu her sapanda güncel zıplama tuşunu gösterir
        if (visible && hintText != null)
        {
            // Eğitici alan: nasıl nişan alınacağını da anlatan uzun ipucu
            bool tutorial = sling.CurrentZone != null && sling.CurrentZone.ShowTutorialHint;
            hintText.text = Loc.Format(tutorial ? "hint.slingTutorial" : "hint.launch", input.JumpKeyName);
        }
        arrow.enabled = visible;
        hint.SetActive(visible);

        // Awake'te (halka henüz kurulmadan) çağrılabilir
        if (ring == null) return;
        if (visible && sling.CurrentZone != null) CaptureZoneShape(sling.CurrentZone);
        ring.enabled = visible && sling.CurrentZone != null;
        UpdateRing();
    }
}
