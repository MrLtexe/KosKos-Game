using UnityEngine;

// Atılma (B.3): karakter kısa süre yerçekimsiz, düz bir çizgide ileri fırlar.
// Havada tek kullanımlık; yere değince veya kılıçla bir şeye vurunca yenilenir.
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

    public bool IsDashing { get; private set; }

    private PlayerMotor motor;
    private PlayerInputReader input;
    private PlayerDeath death;

    private bool airDashAvailable = true;
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
        motor.SetVelocity(new Vector3(dashSpeed, 0f, 0f));
    }

    private void FixedUpdate()
    {
        if (!IsDashing) return;

        if (Time.fixedTime >= dashEndTime)
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
        motor.ReleaseControl(this);
    }

    // Kılıç havada bir şeye vurduğunda da çağrılır (faz 2)
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
