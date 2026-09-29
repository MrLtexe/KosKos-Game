using System;
using UnityEngine;

// Sapan (D.7): havadayken sapan alanına girince zaman yavaşlar, ok fareyi takip eder (öndeki 180°).
// Space ile hemen, ya da yavaşlama bitince otomatik olarak okun yönüne fırlatılır. Yavaşlarken sadece zıplama çalışır.
// Motor'dan önce çalışır
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(PlayerMotor), typeof(PlayerInputReader), typeof(PlayerDeath))]
[RequireComponent(typeof(JumpAbility))]
public class SlingAbility : MonoBehaviour
{
    [Tooltip("Yavaşlama sırasındaki zaman ölçeği. 0.2 = 5 kat yavaş.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float slowTimeScale = 0.2f;

    [Tooltip("Yavaşlamanın süresi (gerçek zaman, sn). Bitince otomatik fırlatılır.")]
    [SerializeField] private float slowDuration = 1f;

    [Tooltip("Fırlatma hızı (birim/sn).")]
    [SerializeField] private float launchSpeed = 30f;

    [Tooltip("Aynı alana fırlatmadan sonra tekrar girememe süresi (gerçek zaman, sn).")]
    [SerializeField] private float reentryCooldown = 0.5f;

    [Tooltip("DENEME: açıksa oyuncu sapan alanına girince alanın ortasına çekilir. Kapatınca girdiği yerde durur.")]
    [SerializeField] private bool snapToZoneCenter = true;

    [Tooltip("DENEME: ortaya kayma yumuşaklığı (gerçek zaman, sn). Büyük = daha yavaş ve yumuşak. 0 = anında.")]
    [SerializeField] private float snapSmoothTime = 0.08f;

    // Sapan başladı/bitti (ok görseli buna bağlı)
    public event Action<bool> SlingActiveChanged;
    // Nişan yönü (normalize, XY düzleminde, öndeki 180°)
    public event Action<Vector3> AimChanged;

    public bool IsSlinging { get; private set; }

    // Fare oyuncuya bu kadar yakınsa yön değiştirilmez (titremeyi önler)
    private const float MinAimDistance = 0.1f;

    private PlayerMotor motor;
    private PlayerInputReader input;
    private PlayerDeath death;
    private JumpAbility jump;

    private SlingZone currentZone;
    private SlingZone lastZone;
    private Vector3 aimDirection = Vector3.right;
    private float slowEndRealTime;
    private float reentryAllowedRealTime;
    private bool launchRequested;
    private float baseFixedDeltaTime;

    // Güvenlik: bir önceki oturumdan yavaş zaman kalmasın
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTimeScale()
    {
        Time.timeScale = 1f;
    }

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        input = GetComponent<PlayerInputReader>();
        death = GetComponent<PlayerDeath>();
        jump = GetComponent<JumpAbility>();
        baseFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void OnEnable()
    {
        input.JumpPressed += OnJumpPressed;
        death.Died += OnDied;
    }

    private void OnDisable()
    {
        input.JumpPressed -= OnJumpPressed;
        death.Died -= OnDied;
        // Bileşen kapanırsa zaman asla yavaş kalmasın
        if (IsSlinging) EndSling();
    }

    public void TryEnter(SlingZone zone)
    {
        if (IsSlinging || death.IsDead || motor.IsGrounded) return;
        if (zone == lastZone && Time.unscaledTime < reentryAllowedRealTime) return;
        if (!motor.TryAcquireControl(this)) return;

        IsSlinging = true;
        currentZone = zone;
        launchRequested = false;
        aimDirection = Vector3.right;
        slowEndRealTime = Time.unscaledTime + slowDuration;

        // Fizik adımı da ölçeklenir; yavaş çekimde hareket takılmasın
        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = baseFixedDeltaTime * slowTimeScale;

        motor.SetVelocity(Vector3.zero);
        SlingActiveChanged?.Invoke(true);
        AimChanged?.Invoke(aimDirection);
    }

    // Fırlatma bir sonraki Update'te yapılır; aynı tuş basışı JumpAbility'de ikinci bir zıplama tetiklemesin
    private void OnJumpPressed()
    {
        if (IsSlinging) launchRequested = true;
    }

    private void Update()
    {
        if (!IsSlinging) return;

        UpdateAim();
        if (launchRequested || Time.unscaledTime >= slowEndRealTime)
        {
            Launch();
        }
    }

    private void FixedUpdate()
    {
        if (!IsSlinging) return;

        // Kilit başka yerden sıfırlandıysa sapanı bitir
        if (!motor.HasControl(this))
        {
            EndSling();
            return;
        }

        if (snapToZoneCenter)
        {
            // Her adımda kalan mesafenin bir kısmı kadar ortaya kayar (üstel yumuşama).
            // Oran gerçek zamana göre hesaplanır; zaman yavaşken de kayma süresi aynı hissettirir.
            Vector3 center = currentZone.transform.position;
            center.z = 0f;
            float fraction = snapSmoothTime > 0f ? 1f - Mathf.Exp(-baseFixedDeltaTime / snapSmoothTime) : 1f;
            motor.SetVelocity((center - motor.Position) * fraction / Time.fixedDeltaTime);
        }
        else
        {
            // Nişan alırken karakter havada sabit
            motor.SetVelocity(Vector3.zero);
        }
    }

    private void UpdateAim()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Fare ekran konumunu oyuncunun düzlemine (z = oyuncu) çevir
        Vector3 playerPosition = transform.position;
        float depth = playerPosition.z - cam.transform.position.z;
        Vector2 screen = input.AimScreenPosition;
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));

        Vector3 direction = world - playerPosition;
        direction.z = 0f;
        if (direction.sqrMagnitude < MinAimDistance * MinAimDistance) return;
        direction.Normalize();

        // Sadece öndeki 180°: fare arkadaysa tam yukarı veya tam aşağı
        if (direction.x < 0f) direction = direction.y >= 0f ? Vector3.up : Vector3.down;

        aimDirection = direction;
        AimChanged?.Invoke(aimDirection);
    }

    private void Launch()
    {
        Vector3 velocity = aimDirection * launchSpeed;
        lastZone = currentZone;
        reentryAllowedRealTime = Time.unscaledTime + reentryCooldown;
        EndSling();
        motor.Launch(velocity);
        // Fırlatmayı tetikleyen Space basışı, hızlı bir inişte ayrıca yer zıplaması yapmasın
        jump.ClearBufferedJump();
    }

    private void EndSling()
    {
        IsSlinging = false;
        currentZone = null;
        launchRequested = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDeltaTime;
        motor.ReleaseControl(this);
        SlingActiveChanged?.Invoke(false);
    }

    private void OnDied()
    {
        if (IsSlinging) EndSling();
    }
}
