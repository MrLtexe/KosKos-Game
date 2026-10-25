using System.Collections.Generic;
using UnityEngine;

// Yolculuk baloncuğu: oyuncu girince baloncuğa hapsolur, baloncuk belirlenen rotayı (ara noktalardan geçen yumuşak eğri)
// oyuncuyla birlikte uçar, rotanın sonunda patlar ve oyuncuyu belirlenen yöne fırlatır. Oyuncu hiçbir şey yapamaz.
// Kurulum: rota için boş nesneleri (Waypoints listesi) ve fırlatma yönü için bir boş nesneyi (Eject Direction) Scene'de sürükleyin.
// Kök nesne yerinde durur (giriş tetiği burada); hareket eden sadece baloncuk görselidir.
// Motor'dan önce çalışır: oyuncunun kontrol kilidini tutar (zipline/sapan gibi)
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(Collider))]
public class TravelBubble : MonoBehaviour
{
    [Header("Rota")]
    [Tooltip("Rotanın ara noktaları (boş nesneler), sırayla. Baloncuk kendi konumundan başlar ve bu noktalardan geçen yumuşak bir eğri izler. Son nokta = varış.")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    [Tooltip("Rota üzerindeki hız (birim/sn).")]
    [SerializeField] private float travelSpeed = 12f;

    [Tooltip("Oyuncu yakalandıktan sonra yola çıkmadan önceki bekleme (sn).")]
    [SerializeField] private float startDelay = 0.3f;

    [Header("Fırlatma")]
    [Tooltip("Fırlatma yönünü gösteren boş nesne: oyuncu son noktadan bu nesneye doğru fırlatılır (uzaklığı önemsiz, sadece yönü).")]
    [SerializeField] private Transform ejectDirection;

    [Tooltip("Fırlatma hızı (birim/sn).")]
    [SerializeField] private float ejectSpeed = 16f;

    [Header("Görsel ve Yeniden Kullanım")]
    [Tooltip("GEÇİCİ baloncuk görseli (rota boyunca hareket eden küre). Boş bırakılırsa görsel yok.")]
    [SerializeField] private Transform visual;

    [Tooltip("Baloncuk patladıktan sonra başlangıç noktasında yeniden belirme süresi (sn).")]
    [SerializeField] private float reappearDelay = 1.5f;

    [Header("Oyunda Rota Gösterimi")]
    [Tooltip("GEÇİCİ: oyunda rota ve fırlatma yönü noktalı çizgiyle gösterilsin mi? (bölüm başında oluşturulur)")]
    [SerializeField] private bool showPathInGame = true;

    [Tooltip("Noktalar arası mesafe (birim).")]
    [SerializeField] private float dotSpacing = 1.2f;

    [Tooltip("Fırlatma yönünde gösterilen nokta sayısı.")]
    [SerializeField] private int ejectDots = 3;

    // Şu an bir baloncuğun içindeki oyuncu (kılıç gibi kilit kullanmayan yetenekler bunu kontrol eder)
    public static PlayerMotor ActiveRider { get; private set; }

    private enum State { Ready, Waiting, Traveling, Gone }

    // Eğri her parça için bu kadar örnekle çizilir (sabit hızlı hareket için)
    private const int SamplesPerSegment = 16;
    private static readonly Color VisualColor = new Color(0.55f, 0.85f, 1f, 0.35f);
    private static readonly Color DotColor = new Color(0.55f, 0.85f, 1f, 0.45f);
    private const float DotSize = 0.18f;

    private State state = State.Ready;
    private float stateStartTime;
    private Collider trigger;
    private Renderer visualRenderer;
    private Material visualMaterial;
    private Material dotMaterial;

    private PlayerMotor rider;
    private PlayerDeath riderDeath;
    private JumpAbility riderJump;

    // Örneklenmiş rota ve her örneğe kadar olan toplam uzunluk
    private readonly List<Vector3> path = new List<Vector3>();
    private readonly List<float> pathLengths = new List<float>();
    private float distance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        ActiveRider = null;
    }

    private void Awake()
    {
        trigger = GetComponent<Collider>();
        if (visual != null)
        {
            visualRenderer = visual.GetComponent<Renderer>();
            if (visualRenderer != null)
            {
                visualMaterial = CreateTransparentMaterial(VisualColor);
                visualRenderer.sharedMaterial = visualMaterial;
            }
        }
        ResetBubble();
    }

    private void Start()
    {
        if (showPathInGame) BuildPathDots();
    }

    // GEÇİCİ: rota boyunca ve fırlatma yönünde küçük yarı saydam noktalar
    private void BuildPathDots()
    {
        BuildPath();
        if (TotalLength <= 0f || dotSpacing <= 0f) return;

        dotMaterial = CreateTransparentMaterial(DotColor);
        var container = new GameObject("PathDots").transform;
        container.SetParent(transform, false);

        // Başlangıç noktası baloncuğun içinde kalır; ilk nokta bir aralık ileriden başlar
        for (float d = dotSpacing; d <= TotalLength; d += dotSpacing)
        {
            CreateDot(container, PointAt(d));
        }

        Vector3 end = PointAt(TotalLength);
        Vector3 direction = EjectDirection();
        for (int i = 1; i <= ejectDots; i++)
        {
            CreateDot(container, end + direction * (dotSpacing * i));
        }
    }

    private void CreateDot(Transform parent, Vector3 position)
    {
        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = "Dot";
        // Hemen silinir: noktalar bir fizik adımı bile oyuncuya çarpmasın
        DestroyImmediate(dot.GetComponent<Collider>());
        dot.transform.SetParent(parent, false);
        dot.transform.position = position;
        dot.transform.localScale = Vector3.one * DotSize;
        var dotRenderer = dot.GetComponent<Renderer>();
        dotRenderer.sharedMaterial = dotMaterial;
        dotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void OnEnable()
    {
        PlayerDeath.AnyPlayerRespawned += ResetBubble;
    }

    private void OnDisable()
    {
        PlayerDeath.AnyPlayerRespawned -= ResetBubble;
        if (rider != null) EndRide(false);
    }

    private void OnDestroy()
    {
        if (visualMaterial != null) Destroy(visualMaterial);
        if (dotMaterial != null) Destroy(dotMaterial);
    }

    private void OnTriggerEnter(Collider other) => TryCatch(other);
    private void OnTriggerStay(Collider other) => TryCatch(other);

    private void TryCatch(Collider other)
    {
        if (state != State.Ready) return;
        Rigidbody body = other.attachedRigidbody;
        if (body == null || !body.TryGetComponent(out PlayerMotor motor)) return;
        if (!body.TryGetComponent(out PlayerDeath death) || death.IsDead) return;
        if (!motor.TryAcquireControl(this)) return;

        // Metal tavan / duvar modundan çık; baloncuk normal yerçekimiyle biter
        motor.ExitSurfaceModes();
        rider = motor;
        riderDeath = death;
        riderJump = body.GetComponent<JumpAbility>();
        riderDeath.Died += OnRiderDied;
        ActiveRider = motor;

        BuildPath();
        distance = 0f;
        Enter(State.Waiting);
        trigger.enabled = false;
    }

    private void FixedUpdate()
    {
        if (state != State.Waiting && state != State.Traveling) return;

        // Kilit başka yerden sıfırlandıysa yolculuğu bitir
        if (!rider.HasControl(this))
        {
            EndRide(false);
            return;
        }

        float dt = Time.fixedDeltaTime;
        if (state == State.Waiting)
        {
            if (Time.fixedTime - stateStartTime >= startDelay) Enter(State.Traveling);
        }
        else
        {
            distance += travelSpeed * dt;
            if (distance >= TotalLength)
            {
                EndRide(true);
                return;
            }
        }

        Vector3 point = PointAt(distance);
        // Oyuncu baloncuğun merkezinde taşınır (bir adımda hedefe ulaşacak hız)
        rider.SetVelocity((point - rider.Position) / dt);
        if (visual != null) visual.position = point;
    }

    // ok: rota tamamlandı ve oyuncu fırlatılır; değilse (ölüm vb.) sadece bırakılır
    private void EndRide(bool ok)
    {
        PlayerMotor motor = rider;
        if (riderDeath != null) riderDeath.Died -= OnRiderDied;
        rider = null;
        riderDeath = null;
        if (ActiveRider == motor) ActiveRider = null;

        if (motor != null) motor.ReleaseControl(this);

        if (ok && motor != null)
        {
            motor.Launch(EjectDirection() * ejectSpeed);
            // Baloncuktayken basılan zıplama, inişte ayrıca zıplatmasın
            if (riderJump != null) riderJump.ClearBufferedJump();
        }
        riderJump = null;

        // Baloncuk patlar; bir süre sonra başlangıçta yeniden belirir
        Enter(State.Gone);
        if (visual != null) visual.gameObject.SetActive(false);
        CancelInvoke(nameof(ResetBubble));
        Invoke(nameof(ResetBubble), reappearDelay);
    }

    private void OnRiderDied()
    {
        if (rider != null) EndRide(false);
    }

    private void ResetBubble()
    {
        CancelInvoke(nameof(ResetBubble));
        if (rider != null) return;
        Enter(State.Ready);
        if (trigger != null) trigger.enabled = true;
        if (visual != null)
        {
            visual.gameObject.SetActive(true);
            visual.position = transform.position;
        }
    }

    private void Enter(State next)
    {
        state = next;
        stateStartTime = Time.fixedTime;
    }

    private Vector3 EjectDirection()
    {
        Vector3 end = waypoints.Count > 0 && waypoints[waypoints.Count - 1] != null
            ? waypoints[waypoints.Count - 1].position
            : transform.position;
        Vector3 direction = ejectDirection != null ? ejectDirection.position - end : Vector3.right;
        direction.z = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
    }

    // --- Rota (Catmull-Rom eğrisi, sabit hız için örneklenmiş) ---

    private float TotalLength => pathLengths.Count > 0 ? pathLengths[pathLengths.Count - 1] : 0f;

    private void BuildPath()
    {
        SamplePath(ControlPoints(), path, pathLengths);
    }

    private List<Vector3> ControlPoints()
    {
        var points = new List<Vector3> { Flat(transform.position) };
        foreach (Transform waypoint in waypoints)
        {
            if (waypoint != null) points.Add(Flat(waypoint.position));
        }
        return points;
    }

    private static void SamplePath(List<Vector3> points, List<Vector3> samples, List<float> lengths)
    {
        samples.Clear();
        lengths.Clear();
        samples.Add(points[0]);
        lengths.Add(0f);

        for (int i = 0; i < points.Count - 1; i++)
        {
            // Uçlarda eksik komşu nokta, uç noktanın kendisiyle tamamlanır
            Vector3 p0 = points[Mathf.Max(i - 1, 0)];
            Vector3 p1 = points[i];
            Vector3 p2 = points[i + 1];
            Vector3 p3 = points[Mathf.Min(i + 2, points.Count - 1)];
            for (int s = 1; s <= SamplesPerSegment; s++)
            {
                Vector3 sample = CatmullRom(p0, p1, p2, p3, s / (float)SamplesPerSegment);
                lengths.Add(lengths[lengths.Count - 1] + Vector3.Distance(samples[samples.Count - 1], sample));
                samples.Add(sample);
            }
        }
    }

    private Vector3 PointAt(float d) => PointOn(path, pathLengths, d);

    // Örneklenmiş rotada baştan d uzaklıktaki nokta (oyun ve Scene çizimi ortak kullanır)
    private static Vector3 PointOn(List<Vector3> samples, List<float> lengths, float d)
    {
        if (samples.Count == 1 || d <= 0f) return samples[0];
        for (int i = 1; i < samples.Count; i++)
        {
            if (lengths[i] >= d)
            {
                float segment = lengths[i] - lengths[i - 1];
                float t = segment > 0f ? (d - lengths[i - 1]) / segment : 1f;
                return Vector3.Lerp(samples[i - 1], samples[i], t);
            }
        }
        return samples[samples.Count - 1];
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, v.y, 0f);

    // GEÇİCİ yarı saydam malzeme (URP Unlit, saydam yüzey)
    private static Material CreateTransparentMaterial(Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.color = color;
        return material;
    }

    // Scene görünümünde rotayı oyundaki gibi noktalı çizgiyle her zaman gösterir (ara noktaları sürüklerken canlı güncellenir).
    // Ara noktalar halka, fırlatma yönü sarı noktalarla gösterilir.
    private void OnDrawGizmos()
    {
        List<Vector3> points = ControlPoints();
        if (points.Count < 2) return;

        var samples = new List<Vector3>();
        var lengths = new List<float>();
        SamplePath(points, samples, lengths);
        float total = lengths[lengths.Count - 1];

        // Oyundaki noktalarla aynı aralık; editörde daha görünür olsun diye biraz daha opak
        float spacing = dotSpacing > 0f ? dotSpacing : 1f;
        Gizmos.color = new Color(DotColor.r, DotColor.g, DotColor.b, 0.9f);
        for (float d = spacing; d <= total; d += spacing) Gizmos.DrawSphere(PointOn(samples, lengths, d), DotSize * 0.5f);

        Gizmos.color = Color.cyan;
        foreach (Vector3 p in points) Gizmos.DrawWireSphere(p, 0.3f);

        Vector3 end = points[points.Count - 1];
        Vector3 direction = EjectDirection();
        Gizmos.color = Color.yellow;
        for (int i = 1; i <= Mathf.Max(ejectDots, 1); i++) Gizmos.DrawSphere(end + direction * (spacing * i), DotSize * 0.5f);
    }
}
