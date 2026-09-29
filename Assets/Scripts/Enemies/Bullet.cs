using UnityEngine;

// Düşman mermisi: düz uçar, seviyeye çarpınca yok olur, oyuncuya değerse öldürür.
// Atılma ile kaçılabilir (kurala göre). Savuşturulabilirse kılıçla geri gönderilir ve sahibini yok eder.
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Bullet : MonoBehaviour, ISwordHittable
{
    public enum DodgeRule
    {
        AnyDash,   // her atılma kaçırır (tavan silahı)
        PhaseDash  // sadece sıyrılganlıklı atılma kaçırır (dron)
    }

    [Tooltip("Kılıçla geri gönderilebilir mi? Tavan silahı mermisi: evet (sarı). Dron mermisi: hayır (mor).")]
    [SerializeField] private bool parryable = true;

    [Tooltip("Hangi atılma bu mermiden kaçırır. AnyDash: her atılma. PhaseDash: sadece sıyrılganlıklı atılma.")]
    [SerializeField] private DodgeRule dodgeRule = DodgeRule.AnyDash;

    [Tooltip("Mermiyi yok eden seviye layer'ları (zemin, duvarlar).")]
    [SerializeField] private LayerMask levelLayers;

    [Tooltip("Merminin en fazla yaşayacağı süre (sn).")]
    [SerializeField] private float lifetime = 3f;

    [Tooltip("Kılıçla geri gönderilen merminin hızı (birim/sn).")]
    [SerializeField] private float reflectSpeed = 25f;

    private Rigidbody rb;
    private Vector3 velocity;
    private float spawnTime;
    private bool reflected;
    private EnemyBody owner;
    private BulletPool pool;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void Launch(Vector3 position, Vector3 direction, float speed, EnemyBody shooter, BulletPool ownerPool)
    {
        owner = shooter;
        pool = ownerPool;
        reflected = false;
        velocity = direction * speed;
        velocity.z = 0f;
        spawnTime = Time.time;
        transform.position = position;
        gameObject.SetActive(true);
        // Havuzdan tekrar kullanılan merminin eski konumdan interpolasyonla "kaymasını" önler
        rb.position = position;
    }

    private void FixedUpdate()
    {
        if (Time.time >= spawnTime + lifetime)
        {
            Despawn();
            return;
        }

        if (reflected)
        {
            // Sahibi bu arada öldüyse (başka sebeple) mermi boşa gider
            if (owner == null || owner.IsDead)
            {
                Despawn();
                return;
            }

            // Sahibine doğru güdümlü uçar; mesafe bir adımdan kısaysa kesin isabet
            Vector3 toOwner = owner.transform.position - rb.position;
            toOwner.z = 0f;
            float step = reflectSpeed * Time.fixedDeltaTime;
            if (toOwner.magnitude <= step)
            {
                owner.Kill();
                Despawn();
                return;
            }
            velocity = toOwner.normalized * reflectSpeed;
        }

        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Geri gönderilen mermi hiçbir şeye takılmaz; isabeti FixedUpdate'te garanti edilir
        if (reflected) return;

        if ((levelLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            Despawn();
            return;
        }

        Rigidbody body = other.attachedRigidbody;
        if (body == null || !body.TryGetComponent(out PlayerDeath death)) return;

        // Uygun atılma sırasında mermi oyuncunun içinden geçer ve uçmaya devam eder
        if (body.TryGetComponent(out DashAbility dash) && CanDodge(dash)) return;

        death.Die();
        Despawn();
    }

    private bool CanDodge(DashAbility dash)
    {
        return dodgeRule == DodgeRule.AnyDash ? dash.IsDodging : dash.IsPhaseDodging;
    }

    public bool OnSwordHit(SwordAttack source)
    {
        // Savuşturulamayan mermi, zaten geri dönen mermi veya savuşturma kilitliyse: kılıç etkisiz
        if (!parryable || reflected || !source.ParryUnlocked) return false;

        reflected = true;
        spawnTime = Time.time;
        return true;
    }

    private void Despawn()
    {
        pool.Return(this);
    }
}
