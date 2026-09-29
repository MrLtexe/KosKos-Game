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
    [SerializeField] private float dashDuration = 0.15f;

    [Tooltip("Yerde iki atılma arasındaki bekleme süresi (sn).")]
    [SerializeField] private float groundDashCooldown = 0.3f;

    [Header("Sıyrılganlık")]
    [Tooltip("Sıyrılganlık açıldı mı? (Oyunda ilerledikçe açılır.) Açıksa atılırken ince duvarların ve kırılabilirlerin içinden geçer.")]
    [SerializeField] private bool phaseUnlocked;

    [Tooltip("Sıyrılganlık ile içinden geçilebilen layer'lar (Phaseable). Kalın duvarlar bu layer'da OLMAMALI.")]
    [SerializeField] private LayerMask phaseableLayers;

    public bool IsDashing { get; private set; }

    private PlayerMotor motor;
    private PlayerInputReader input;
    private PlayerDeath death;

    private bool airDashAvailable = true;
    private bool isPhasing;
    private float dashEndTime;
    private float lastGroundDashTime = float.NegativeInfinity;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        input = GetComponent<PlayerInputReader>();
        death = GetComponent<PlayerDeath>();
    }

    private void OnEnable()
    {
        input.DashPressed += OnDashPressed;
        motor.Landed += RefreshAirDash;
        death.Died += OnDied;
        death.Respawned += OnRespawned;
    }

    private void OnDisable()
    {
        input.DashPressed -= OnDashPressed;
        motor.Landed -= RefreshAirDash;
        death.Died -= OnDied;
        death.Respawned -= OnRespawned;
    }

    private void OnDashPressed()
    {
        if (IsDashing || death.IsDead) return;

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

        isPhasing = phaseUnlocked;
        if (isPhasing) motor.SetExcludedLayers(phaseableLayers);

        motor.SetVelocity(new Vector3(dashSpeed, 0f, 0f));
    }

    private void FixedUpdate()
    {
        if (!IsDashing) return;

        // Süre bittiğinde hâlâ bir duvarın içindeysek çıkana kadar atılmaya devam (sıkışmayı önler)
        if (Time.fixedTime >= dashEndTime && !(isPhasing && motor.OverlapsAny(phaseableLayers)))
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
        if (isPhasing)
        {
            motor.SetExcludedLayers(0);
            isPhasing = false;
        }
        motor.ReleaseControl(this);
    }

    // Kılıç havada bir şeye vurduğunda da çağrılır
    public void RefreshAirDash()
    {
        airDashAvailable = true;
    }

    private void OnDied()
    {
        // Atılma sırasında ölürsek kilit takılı kalmasın
        if (IsDashing) EndDash();
    }

    private void OnRespawned()
    {
        airDashAvailable = true;
        lastGroundDashTime = float.NegativeInfinity;
    }
}
