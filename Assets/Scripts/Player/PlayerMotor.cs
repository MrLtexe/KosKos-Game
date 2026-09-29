using System;
using UnityEngine;

// Oyuncunun Rigidbody'sini süren TEK script.
// Otomatik koşu, özel yerçekimi, zemin kontrolü, önden çarpma tespiti ve kontrol kilidini yönetir.
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Koşu")]
    [Tooltip("Yerdeyken sabit koşu hızı (birim/sn). Yerde bu hız asla düşmez.")]
    [SerializeField] private float runSpeed = 8f;

    [Tooltip("Havadayken hızın saniyede ne kadar azaldığı (birim/sn²).")]
    [SerializeField] private float airDeceleration = 1.5f;

    [Tooltip("Havada hızın düşebileceği en düşük değer (birim/sn).")]
    [SerializeField] private float minAirSpeed = 5f;

    [Tooltip("Boost ile koşu hızının üstüne çıkan hızın saniyede ne kadar azaldığı (birim/sn²).")]
    [SerializeField] private float boostDecayRate = 8f;

    [Header("Yerçekimi")]
    [Tooltip("Karaktere uygulanan özel yerçekimi (birim/sn²). Yüksek değer = daha keskin zıplama.")]
    [SerializeField] private float gravity = 40f;

    [Header("Zemin Kontrolü")]
    [Tooltip("Zemin sayılan layer'lar.")]
    [SerializeField] private LayerMask groundLayers;

    [Tooltip("Ayakların altında zemin aranan mesafe (birim).")]
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Header("Çarpışma")]
    [Tooltip("Temas normalinin X'i bu değerin eksisinden küçükse önden çarpma sayılır (0-1). 0.7 ≈ 45° ve daha dik yüzeyler.")]
    [SerializeField] private float frontalHitThreshold = 0.7f;

    [Tooltip("Ayak seviyesinin bu kadar üstündeki temaslar önden çarpma sayılır. Zemin eklemlerinde yanlış ölümü engeller (birim).")]
    [SerializeField] private float stepTolerance = 0.15f;

    public event Action Landed;
    public event Action LeftGround;
    public event Action HitObstacle;

    public bool IsGrounded { get; private set; }
    public float CurrentSpeed => currentSpeed;
    public bool IsControlLocked => controlOwner != null;
    public bool IsBoosted => currentSpeed > runSpeed + BoostEpsilon;
    // Düşmanların hedef önü nişan alması için
    public Vector3 Velocity => rb.linearVelocity;
    // Fizik konumu (interpolasyonsuz); zipline gibi konum takibi yapan yetenekler için
    public Vector3 Position => rb.position;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private float currentSpeed;
    private object controlOwner;
    private bool frozen;

    // Zemin taramasının başlangıcını biraz yukarı alır; başlangıçta zemine gömülü kalmasın diye
    private const float CastSkin = 0.05f;
    // Yukarı doğru bu hızdan hızlı gidiyorsak zeminde sayılmayız (zıplamanın ilk karesi için)
    private const float GroundedMaxUpVelocity = 0.01f;
    // Hız koşu hızını bu kadar aşarsa "boost'lu" sayılır (kayan nokta hatasına karşı pay)
    private const float BoostEpsilon = 0.01f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        // 2D düzleme kilitle: Z ekseninde hareket ve tüm dönmeler kapalı
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Sürtünmesiz malzeme: aksi halde zemin sürtünmesi yerdeki koşu hızını runSpeed'in altına düşürür
        capsule.sharedMaterial = new PhysicsMaterial("PlayerNoFriction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };

        currentSpeed = runSpeed;
    }

    private void FixedUpdate()
    {
        if (frozen) return;

        // Önce zemin kontrolü: Landed içinde tetiklenen zıplama aşağıdaki hız hesabında kaybolmasın
        UpdateGrounded();

        // Kontrol kilidi bir yetenekteyse (ör. atılma) hızı o yetenek belirler
        if (controlOwner != null) return;

        UpdateHorizontalSpeed(Time.fixedDeltaTime);

        float verticalVelocity = rb.linearVelocity.y - gravity * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector3(currentSpeed, verticalVelocity, 0f);
    }

    private void UpdateGrounded()
    {
        bool wasGrounded = IsGrounded;

        float radius = capsule.radius * 0.95f;
        // Kapsülün alt küresinin merkezi
        Vector3 bottomSphere = transform.position + capsule.center + Vector3.down * (capsule.height * 0.5f - capsule.radius);
        Vector3 origin = bottomSphere + Vector3.up * CastSkin;

        bool hit = Physics.SphereCast(origin, radius, Vector3.down, out _, groundCheckDistance + CastSkin,
            groundLayers, QueryTriggerInteraction.Ignore);

        IsGrounded = hit && rb.linearVelocity.y <= GroundedMaxUpVelocity;

        if (!wasGrounded && IsGrounded)
        {
            Landed?.Invoke();
        }
        else if (wasGrounded && !IsGrounded)
        {
            LeftGround?.Invoke();
        }
    }

    private void UpdateHorizontalSpeed(float dt)
    {
        if (currentSpeed > runSpeed)
        {
            // Boost fazlası (yerde veya havada) yavaşça koşu hızına iner
            currentSpeed = Mathf.MoveTowards(currentSpeed, runSpeed, boostDecayRate * dt);
        }
        else if (IsGrounded)
        {
            // Yerde hız sabit
            currentSpeed = runSpeed;
        }
        else
        {
            // Havada yavaş yavaş minimum hava hızına düşer
            currentSpeed = Mathf.MoveTowards(currentSpeed, minAirSpeed, airDeceleration * dt);
        }
    }

    private void OnCollisionEnter(Collision collision) => CheckFrontalHit(collision);
    private void OnCollisionStay(Collision collision) => CheckFrontalHit(collision);

    private void CheckFrontalHit(Collision collision)
    {
        if (frozen) return;

        // Boost'luyken kırılabilire önden çarpmak onu kırar (D.3), ölüm olmaz
        Breakable breakable = IsBoosted ? collision.collider.GetComponentInParent<Breakable>() : null;

        float feetY = capsule.bounds.min.y;
        int count = collision.contactCount;
        for (int i = 0; i < count; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            // Yüzey koşu yönüne karşı bakıyorsa ve ayak seviyesinin üstündeyse: önden çarpma
            if (contact.normal.x < -frontalHitThreshold && contact.point.y > feetY + stepTolerance)
            {
                if (breakable != null)
                {
                    breakable.Break();
                    return;
                }

                HitObstacle?.Invoke();
                return;
            }
        }
    }

    // Verilen yüksekliğe ulaşacak dikey hızı uygular
    public void Jump(float height)
    {
        Vector3 v = rb.linearVelocity;
        v.y = Mathf.Sqrt(2f * gravity * height);
        rb.linearVelocity = v;
    }

    // Boost gibi anlık hız artışları; fazlası boostDecayRate ile söner
    public void AddSpeed(float amount)
    {
        currentSpeed += amount;
    }

    // İstenen yöne fırlatma (sapan, zipline sonu). Yatay hız motorun koşu hızına yazılır,
    // böylece fırlatmadan sonra normal hava kuralları (yavaşlama, boost sönmesi) devam eder.
    public void Launch(Vector3 velocity)
    {
        currentSpeed = Mathf.Max(0f, velocity.x);
        rb.linearVelocity = new Vector3(currentSpeed, velocity.y, 0f);
    }

    // Özel yetenekler (atılma, boost şarjı) için tekil kontrol kilidi
    public bool TryAcquireControl(object owner)
    {
        if (frozen || controlOwner != null) return false;
        controlOwner = owner;
        return true;
    }

    public void ReleaseControl(object owner)
    {
        if (controlOwner == owner) controlOwner = null;
    }

    public bool HasControl(object owner) => controlOwner == owner;

    // Sadece kontrol kilidinin sahibi çağırmalı
    public void SetVelocity(Vector3 velocity)
    {
        rb.linearVelocity = velocity;
    }

    // Verilen layer'larla fiziksel çarpışmayı kapatır (sıyrılganlık). 0 = hepsi açık.
    public void SetExcludedLayers(LayerMask layers)
    {
        rb.excludeLayers = layers;
    }

    // Kapsül şu an verilen layer'lardaki bir collider'ın içinde mi?
    public bool OverlapsAny(LayerMask layers)
    {
        Vector3 center = rb.position + capsule.center;
        float halfSegment = capsule.height * 0.5f - capsule.radius;
        Vector3 top = center + Vector3.up * halfSegment;
        Vector3 bottom = center + Vector3.down * halfSegment;
        return Physics.CheckCapsule(bottom, top, capsule.radius, layers, QueryTriggerInteraction.Ignore);
    }

    // Ölüm sırasında karakteri dondurur / çözer.
    // Yerçekimi bizim kodumuzda olduğu için FixedUpdate durunca karakter olduğu yerde kalır;
    // isKinematic kullanılmaz (Continuous çarpışma modu ile uyumsuz).
    public void SetFrozen(bool value)
    {
        frozen = value;
        rb.linearVelocity = Vector3.zero;
    }

    // Yeniden doğuşta konumu ve tüm hareket durumunu sıfırlar
    public void ResetState(Vector3 position)
    {
        rb.position = position;
        transform.position = position;
        currentSpeed = runSpeed;
        controlOwner = null;
        rb.excludeLayers = 0;
        IsGrounded = false;
    }
}
