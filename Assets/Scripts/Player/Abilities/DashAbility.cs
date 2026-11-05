using UnityEngine;

// Atılma (B.3): karakter kısa süre yerçekimsiz, düz bir çizgide ileri fırlar.
// Havada tek kullanımlık; yere değince veya kılıçla bir şeye vurunca yenilenir.
// Sıyrılganlık (B.3.1) açıksa atılma boyunca Phaseable layer'ındaki nesnelerin içinden geçer.
// Motor'dan önce çalışır: atılmanın bittiği adımda kilit bırakılır ve motor aynı adımda normal koşuya döner
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(PlayerMotor), typeof(PlayerInputReader), typeof(PlayerDeath))]
public class DashAbility : MonoBehaviour
{
    [Tooltip("Atılma sırasındaki yatay hız (birim/sn).")]
    [SerializeField] private float dashSpeed = 22f;

    [Tooltip("Atılmanın süresi (sn). Mesafe = hız x süre.")]
    [SerializeField] private float dashDuration = 0.2f;

    [Tooltip("Yerde iki atılma arasındaki bekleme süresi (sn).")]
    [SerializeField] private float groundDashCooldown = 0.3f;

    [Header("Sıyrılganlık")]
    [Tooltip("Sıyrılganlık ile içinden geçilebilen layer'lar (Phaseable). Kalın duvarlar bu layer'da OLMAMALI.")]
    [SerializeField] private LayerMask phaseableLayers;

    [Tooltip("Duvarın ya da merminin içinden geçince karakterin yarı saydam kaldığı süre (sn). Geçiş çok kısa sürdüğü için görünsün diye.")]
    [SerializeField] private float phaseVisualHold = 0.15f;

    [Header("Mermiden Kaçış")]
    [Tooltip("Atılma bittikten sonra mermilerden kaçış korumasının devam ettiği ek süre (sn). Yelpaze mermilerinin hepsini tek atılmayla geçebilmek için.")]
    [SerializeField] private float dodgeGraceTime = 0.1f;

    [Header("Eğilme")]
    [Tooltip("Atılma sırasında karakterin Z ekseninde döndüğü açı (derece).")]
    [SerializeField] private float dashTiltAngle = -17f;

    [Tooltip("Atılma başında düz duruştan atılma açısına geçiş süresi (sn).")]
    [SerializeField] private float dashTiltTime = 0.05f;

    [Tooltip("Atılma bitince karakterin düz duruşa dönme süresi (sn).")]
    [SerializeField] private float tiltReturnTime = 0.2f;

    public bool IsDashing { get; private set; }
    // Sıyrılganlık açık mı? Açıksa atılırken Phaseable layer'ındaki nesnelerin (ince duvarlar) içinden geçer.
    // Bölümün AbilityLoadout'undan PlayerLoadout tarafından ayarlanır
    public bool PhaseUnlocked { get; set; }
    // Mermiden kaçış: atılma sürüyor ya da atılma yeni bitti (tavan silahı mermileri)
    public bool IsDodging => IsDashing || Time.time < dodgeGraceEndTime;
    // Sıyrılganlıklı kaçış: sıyrılganlıklı atılma sürüyor ya da yeni bitti (dron mermileri)
    public bool IsPhaseDodging => isPhasing || (lastDashPhased && Time.time < dodgeGraceEndTime);
    // Şu an bir duvarın ya da düşman mermisinin içinden geçiliyor mu (görsel: karakter yarı saydam)
    public bool IsPhasingThrough => Time.time < phaseVisualUntil;

    private PlayerMotor motor;
    private PlayerInputReader input;
    private PlayerDeath death;

    private bool airDashAvailable = true;
    private bool isPhasing;
    private bool lastDashPhased;
    private float dashEndTime;
    private float dodgeGraceEndTime = float.NegativeInfinity;
    private float lastGroundDashTime = float.NegativeInfinity;
    private float phaseVisualUntil = float.NegativeInfinity;
    // Zipline'dan düşünce yere inene kadar atılma yok
    private bool blockedUntilLanded;

    // Eğilme görseli: atılma başlangıcı, dönüş başlangıcı ve dönüşe başlarken açı
    private float dashStartTime;
    private bool tiltReturning;
    private float tiltReturnStartTime;
    private float tiltReturnFrom;
    private float currentTilt;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        input = GetComponent<PlayerInputReader>();
        death = GetComponent<PlayerDeath>();
    }

    private void OnEnable()
    {
        input.DashPressed += OnDashPressed;
        motor.Landed += OnLanded;
        death.Died += OnDied;
        death.Respawned += OnRespawned;
    }

    private void OnDisable()
    {
        input.DashPressed -= OnDashPressed;
        motor.Landed -= OnLanded;
        death.Died -= OnDied;
        death.Respawned -= OnRespawned;
    }

    private void OnDashPressed()
    {
        // Duvar koşusunda atılma yok
        if (IsDashing || death.IsDead || blockedUntilLanded || motor.IsWallRunning) return;

        bool grounded = motor.IsGrounded;
        if (grounded)
        {
            if (Time.time < lastGroundDashTime + groundDashCooldown) return;
        }
        else if (!airDashAvailable)
        {
            return;
        }

        if (!motor.TryAcquireControl(this)) return;

        if (grounded) lastGroundDashTime = Time.time;
        else airDashAvailable = false;

        IsDashing = true;
        // Fizik zamanına göre ölçülür; FixedUpdate'teki kontrol ile aynı saat kullanılsın diye
        dashEndTime = Time.fixedTime + dashDuration;
        dashStartTime = Time.time;
        tiltReturning = false;

        isPhasing = PhaseUnlocked;
        if (isPhasing) motor.SetExcludedLayers(phaseableLayers);

        motor.SetVelocity(new Vector3(dashSpeed, 0f, 0f));
    }

    private void FixedUpdate()
    {
        if (!IsDashing) return;

        bool insideWall = isPhasing && motor.OverlapsAny(phaseableLayers);
        if (insideWall) MarkPhasedThrough();

        // Süre bittiğinde hâlâ bir duvarın içindeysek çıkana kadar atılmaya devam (sıkışmayı önler)
        if (Time.fixedTime >= dashEndTime && !insideWall)
        {
            EndDash();
            return;
        }

        // Atılma boyunca hız sabit, yerçekimi yok
        motor.SetVelocity(new Vector3(dashSpeed, 0f, 0f));
    }

    private void EndDash()
    {
        IsDashing = false;
        // Mermi koruması atılmadan sonra kısa bir süre daha devam eder
        dodgeGraceEndTime = Time.time + dodgeGraceTime;
        lastDashPhased = isPhasing;
        if (isPhasing)
        {
            motor.SetExcludedLayers(0);
            isPhasing = false;
        }
        motor.ReleaseControl(this);

        // Görsel: bulunulan açıdan düz duruşa yavaşça dönülür
        tiltReturning = true;
        tiltReturnStartTime = Time.time;
        tiltReturnFrom = currentTilt;
    }

    // Eğilme görseli: atılma başında dashTiltAngle'a döner; atılma bitince düze döner
    private void Update()
    {
        float tilt;
        if (IsDashing)
        {
            tilt = Mathf.Lerp(0f, dashTiltAngle, (Time.time - dashStartTime) / dashTiltTime);
        }
        else if (tiltReturning)
        {
            float t = (Time.time - tiltReturnStartTime) / tiltReturnTime;
            tilt = Mathf.Lerp(tiltReturnFrom, 0f, t);
            if (t >= 1f) tiltReturning = false;
        }
        else
        {
            return;
        }
        SetTilt(tilt);
    }

    private void SetTilt(float degrees)
    {
        currentTilt = degrees;
        motor.SetTiltZ(degrees);
    }

    // Ölüm/yeniden doğuşta karakter anında düz durur
    private void ResetTilt()
    {
        tiltReturning = false;
        SetTilt(0f);
    }

    // Duvarın ya da düşman mermisinin içinden geçildi: karakter kısa süre yarı saydam görünür (mermiler de çağırır)
    public void MarkPhasedThrough()
    {
        phaseVisualUntil = Time.time + phaseVisualHold;
    }

    // Kılıç havada bir şeye vurduğunda da çağrılır
    public void RefreshAirDash()
    {
        airDashAvailable = true;
    }

    // Zipline'dan düşüş: yere inene kadar atılma kapalı (kılıç isabeti de açmaz)
    public void BlockUntilLanded()
    {
        blockedUntilLanded = true;
    }

    private void OnLanded()
    {
        RefreshAirDash();
        blockedUntilLanded = false;
    }

    private void OnDied()
    {
        // Atılma sırasında ölürsek kilit takılı kalmasın
        if (IsDashing) EndDash();
        ResetTilt();
        // Ölümde ek koruma süresi ve saydamlık taşınmaz
        dodgeGraceEndTime = float.NegativeInfinity;
        phaseVisualUntil = float.NegativeInfinity;
    }

    private void OnRespawned()
    {
        airDashAvailable = true;
        lastGroundDashTime = float.NegativeInfinity;
        dodgeGraceEndTime = float.NegativeInfinity;
        blockedUntilLanded = false;
        ResetTilt();
    }
}
