using System.Collections.Generic;
using UnityEngine;

// Kılıç (B.5): ileri doğru, üstü ve altı da kapsayan kısa bir vuruş.
// Kontrol kilidi kullanmaz; koşarken, zıplarken, atılırken kullanılabilir.
// Havadayken bir şeye vurursa atılma hakkı yenilenir.
[RequireComponent(typeof(PlayerMotor), typeof(PlayerInputReader), typeof(PlayerDeath))]
[RequireComponent(typeof(DashAbility))]
public class SwordAttack : MonoBehaviour
{
    [Header("Zamanlama")]
    [Tooltip("Bir kılıç vuruşunun toplam süresi (sn).")]
    [SerializeField] private float attackDuration = 0.25f;

    [Tooltip("Vuruşun başından itibaren isabet kutusunun aktif olduğu süre (sn).")]
    [SerializeField] private float activeTime = 0.1f;

    [Tooltip("İki vuruşun başlangıçları arasındaki en kısa süre (sn).")]
    [SerializeField] private float attackCooldown = 0.35f;

    [Header("İsabet Kutusu")]
    [Tooltip("İsabet kutusunun oyuncu merkezine göre konumu (birim). X pozitif = önde.")]
    [SerializeField] private Vector3 hitboxOffset = new Vector3(1f, 0f, 0f);

    [Tooltip("İsabet kutusunun boyutu (birim). Oyun ilerledikçe büyütülerek kılıç alanı genişletilir.")]
    [SerializeField] private Vector3 hitboxSize = new Vector3(2f, 3f, 1f);

    [Tooltip("Kılıcın vurabileceği layer'lar. Player layer'ı dahil edilmemeli.")]
    [SerializeField] private LayerMask hitLayers = ~0;

    [Header("Geçici Görsel")]
    [Tooltip("GEÇİCİ: vuruş görseli. Oyunda bunun üstünde hilal şeklinde beyaz bir kılıç izi çizilir. Animasyon gelince kaldırılacak. Boş bırakılabilir.")]
    [SerializeField] private Renderer hitboxVisual;

    [Tooltip("Kılıç izinin yarıçapı (birim, oyuncu merkezinden).")]
    [SerializeField] private float slashRadius = 1.7f;

    [Tooltip("Kılıç izinin ortadaki en kalın yeri (birim); uçlara doğru sivrilir.")]
    [SerializeField] private float slashThickness = 0.5f;

    [Tooltip("Kılıç izinin başladığı açı (derece; 0 = tam ön, 90 = tam üst).")]
    [SerializeField] private float slashStartAngle = -80f;

    [Tooltip("Kılıç izinin bittiği açı (derece; negatif = önün altı).")]
    [SerializeField] private float slashEndAngle = 100f;

    [Tooltip("Kılıç izinin toplam görünme süresi (sn): önce ucu süpürür, sonra kuyruğu yetişip kaybolur.")]
    [SerializeField] private float slashDuration = 0.2f;

    [Tooltip("Kılıç izinin rengi.")]
    [SerializeField] private Color slashColor = Color.white;

    [Tooltip("Kılıç izinin derinliği (Z). Negatif = karakterin önünde (kameraya yakın).")]
    [SerializeField] private float slashDepth = -0.3f;

    public bool IsAttacking { get; private set; }
    // Savuşturma açık mı? Bölümün AbilityLoadout'undan PlayerLoadout tarafından ayarlanır
    public bool ParryUnlocked { get; set; }

    private const int MaxHitsPerQuery = 16;
    // Kılıç izi yayındaki dilim sayısı (fazlası = daha pürüzsüz eğri)
    private const int SlashSegments = 24;

    private readonly Collider[] overlapBuffer = new Collider[MaxHitsPerQuery];
    private readonly List<ISwordHittable> hitThisSwing = new List<ISwordHittable>(MaxHitsPerQuery);

    private Rigidbody rb;
    private PlayerMotor motor;
    private PlayerInputReader input;
    private PlayerDeath death;
    private DashAbility dash;
    // İsteğe bağlı: sapan yavaşlamasında sadece zıplama çalışır (D.7), kılıç kullanılamaz
    private SlingAbility sling;
    // İsteğe bağlı: jet-çanta itkisi sürerken kılıç kullanılamaz
    private JetBagAbility jetBag;

    private float attackStartTime = float.NegativeInfinity;
    private bool dashRefreshedThisSwing;

    // Kılıç izi: her karede yeniden çizilen hilal ağı ve görünmeye başladığı an
    private Mesh slashMesh;
    private Material slashMaterial;
    private readonly Vector3[] slashVertices = new Vector3[(SlashSegments + 1) * 2];
    private float slashStartTime = float.NegativeInfinity;
    private bool slashVisible;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        motor = GetComponent<PlayerMotor>();
        input = GetComponent<PlayerInputReader>();
        death = GetComponent<PlayerDeath>();
        dash = GetComponent<DashAbility>();
        sling = GetComponent<SlingAbility>();
        jetBag = GetComponent<JetBagAbility>();
        SetupSlash();
        SetVisual(false);
    }

    private void OnDestroy()
    {
        if (slashMesh != null) Destroy(slashMesh);
        if (slashMaterial != null) Destroy(slashMaterial);
    }

    // Görsel nesnenin dörtgen ağı yerine hilal ağı konur (prefab değişmez, sadece oyunda)
    private void SetupSlash()
    {
        if (hitboxVisual == null || !hitboxVisual.TryGetComponent(out MeshFilter filter)) return;

        slashMesh = new Mesh { name = "SwordSlash" };
        slashMesh.MarkDynamic();
        slashMesh.vertices = slashVertices;
        var triangles = new int[SlashSegments * 6];
        for (int i = 0; i < SlashSegments; i++)
        {
            int outer = i * 2, inner = outer + 1, nextOuter = outer + 2, nextInner = outer + 3;
            triangles[i * 6 + 0] = outer;
            triangles[i * 6 + 1] = nextOuter;
            triangles[i * 6 + 2] = inner;
            triangles[i * 6 + 3] = inner;
            triangles[i * 6 + 4] = nextOuter;
            triangles[i * 6 + 5] = nextInner;
        }
        slashMesh.triangles = triangles;
        filter.sharedMesh = slashMesh;

        // Işıksız düz renk, iki yüzü de görünür (yayın dönüş yönünden bağımsız)
        slashMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        slashMaterial.color = slashColor;
        slashMaterial.SetFloat("_Cull", 0f);
        hitboxVisual.sharedMaterial = slashMaterial;
        hitboxVisual.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        hitboxVisual.receiveShadows = false;

        Transform t = hitboxVisual.transform;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    private void Update()
    {
        if (!slashVisible) return;

        float t = (Time.time - slashStartTime) / slashDuration;
        if (t >= 1f)
        {
            SetVisual(false);
            return;
        }
        UpdateSlashMesh(t);
    }

    // Uç (baş) önce yayı süpürür, kuyruk arkadan yetişir; kalınlık yayın ortasında en fazla, uçlarda sıfır (hilal)
    private void UpdateSlashMesh(float t)
    {
        float head = Mathf.Clamp01(t / 0.5f);
        float tail = Mathf.Clamp01((t - 0.35f) / 0.65f);

        for (int i = 0; i <= SlashSegments; i++)
        {
            float u = Mathf.Lerp(tail, head, (float)i / SlashSegments);
            float angle = Mathf.Lerp(slashStartAngle, slashEndAngle, u) * Mathf.Deg2Rad;
            float thickness = slashThickness * Mathf.Sin(u * Mathf.PI);
            var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            slashVertices[i * 2] = direction * slashRadius;
            slashVertices[i * 2 + 1] = direction * (slashRadius - thickness);
        }
        slashMesh.vertices = slashVertices;
        slashMesh.RecalculateBounds();
    }

    private void OnEnable()
    {
        input.SwordPressed += OnSwordPressed;
        death.Died += OnDied;
    }

    private void OnDisable()
    {
        input.SwordPressed -= OnSwordPressed;
        death.Died -= OnDied;
    }

    private void OnSwordPressed()
    {
        if (death.IsDead) return;
        if (sling != null && sling.IsSlinging) return;
        if (jetBag != null && jetBag.IsThrusting) return;
        // Yolculuk baloncuğunun içindeyken hiçbir şey yapılamaz
        if (TravelBubble.ActiveRider == motor) return;
        if (Time.fixedTime < attackStartTime + attackCooldown) return;

        IsAttacking = true;
        attackStartTime = Time.fixedTime;
        hitThisSwing.Clear();
        dashRefreshedThisSwing = false;
        SetVisual(true);
    }

    private void FixedUpdate()
    {
        if (!IsAttacking) return;

        float elapsed = Time.fixedTime - attackStartTime;
        if (elapsed >= attackDuration)
        {
            EndAttack();
            return;
        }

        // İsabet kutusu sadece ilk activeTime boyunca açık; görsel kendi süresinde (slashDuration) kaybolur
        if (elapsed < activeTime) DetectHits();
    }

    private void DetectHits()
    {
        // rb.position: interpolasyonlu transform bir fizik adımı geride kalır (atılırken ~0.4 birim)
        Vector3 center = rb.position + hitboxOffset;
        // Collide: düşman gövdeleri ve mermiler trigger; onları da yakalamalıyız
        int count = Physics.OverlapBoxNonAlloc(center, hitboxSize * 0.5f, overlapBuffer, Quaternion.identity,
            hitLayers, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            ISwordHittable hittable = overlapBuffer[i].GetComponentInParent<ISwordHittable>();
            // Aynı vuruşta aynı nesneye iki kez vurulmaz
            if (hittable == null || hitThisSwing.Contains(hittable)) continue;

            hitThisSwing.Add(hittable);
            bool counted = hittable.OnSwordHit(this);

            // Havada gerçek bir isabet atılmayı yeniler (vuruş başına bir kez yeterli)
            if (counted && !motor.IsGrounded && !dashRefreshedThisSwing)
            {
                dash.RefreshAirDash();
                dashRefreshedThisSwing = true;
            }
        }
    }

    // Vuruş süresi bitince sadece vuruş biter; kılıç izi kendi süresinde kaybolur
    private void EndAttack()
    {
        IsAttacking = false;
    }

    // Ölümde iz anında kaybolur
    private void OnDied()
    {
        EndAttack();
        SetVisual(false);
    }

    private void SetVisual(bool visible)
    {
        slashVisible = visible && slashMesh != null;
        if (hitboxVisual == null) return;

        if (slashVisible)
        {
            // Hilal oyuncu merkezinin etrafında çizilir
            hitboxVisual.transform.localPosition = new Vector3(0f, 0f, slashDepth);
            slashStartTime = Time.time;
            UpdateSlashMesh(0f);
        }
        hitboxVisual.enabled = slashVisible;
    }

    // Scene görünümünde isabet kutusunu gösterir (ayar yaparken kolaylık)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + hitboxOffset, hitboxSize);
    }
}
