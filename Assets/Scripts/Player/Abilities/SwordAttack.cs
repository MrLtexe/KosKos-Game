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
    [Tooltip("GEÇİCİ: isabet kutusu aktifken gösterilen görsel. Animasyon gelince kaldırılacak. Boş bırakılabilir.")]
    [SerializeField] private Renderer hitboxVisual;

    public bool IsAttacking { get; private set; }
    // Savuşturma açık mı? Bölümün AbilityLoadout'undan PlayerLoadout tarafından ayarlanır
    public bool ParryUnlocked { get; set; }

    // Görselin kapsülün arkasında kalması için Z kaydırması
    private const float VisualDepthOffset = 0.6f;
    private const int MaxHitsPerQuery = 16;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        motor = GetComponent<PlayerMotor>();
        input = GetComponent<PlayerInputReader>();
        death = GetComponent<PlayerDeath>();
        dash = GetComponent<DashAbility>();
        sling = GetComponent<SlingAbility>();
        jetBag = GetComponent<JetBagAbility>();
        SetVisual(false);
    }

    private void OnEnable()
    {
        input.SwordPressed += OnSwordPressed;
        death.Died += EndAttack;
    }

    private void OnDisable()
    {
        input.SwordPressed -= OnSwordPressed;
        death.Died -= EndAttack;
    }

    private void OnSwordPressed()
    {
        if (death.IsDead) return;
        if (sling != null && sling.IsSlinging) return;
        if (jetBag != null && jetBag.IsThrusting) return;
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

        if (elapsed < activeTime)
        {
            DetectHits();
        }
        else
        {
            // İsabet kutusu kapandı; vuruşun geri kalanı sadece bekleme
            SetVisual(false);
        }
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

    private void EndAttack()
    {
        IsAttacking = false;
        SetVisual(false);
    }

    private void SetVisual(bool visible)
    {
        if (hitboxVisual == null) return;

        if (visible)
        {
            // Görsel her zaman güncel isabet kutusu ayarlarıyla aynı yerde/boyutta olsun
            Transform t = hitboxVisual.transform;
            t.localPosition = hitboxOffset + Vector3.forward * VisualDepthOffset;
            t.localScale = new Vector3(hitboxSize.x, hitboxSize.y, 1f);
        }
        hitboxVisual.enabled = visible;
    }

    // Scene görünümünde isabet kutusunu gösterir (ayar yaparken kolaylık)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + hitboxOffset, hitboxSize);
    }
}
