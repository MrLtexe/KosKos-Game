using System.Collections.Generic;
using UnityEngine;

// Kanca (C): E basılı tutulunca öndeki en yakın kanca noktasına tutunur.
// Nokta başın üstündeyse sallanma (sarkaç), oyuncuyla aynı hizadaysa çekilme.
// Sallanmada ileri hız giriş hızı + bonus, hiç düşmez (geri sallanma yok); E bırakılınca ileri fırlatılır.
// E yay sonundan (varsayılan 90°) önce bırakılmazsa oyuncu patlar (ölüm).
// Tavana çarpmak ölümdür; önden duvara çarpmak motorun normal kuralıyla ölümdür.
// Motor'dan önce çalışır: kilit bırakıldığı adımda motor hemen normal harekete döner
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(PlayerMotor), typeof(PlayerInputReader), typeof(PlayerDeath))]
public class HookAbility : MonoBehaviour
{
    [Header("Hedefleme")]
    [Tooltip("Kanca menzili (birim). Sadece oyuncunun önündeki noktalar sayılır.")]
    [SerializeField] private float range = 7f;

    [Tooltip("Oyuncu merkezine göre bu yükseklik aralığındaki (±) noktalar çekilme noktasıdır; daha yukarıdakiler sallanma, daha aşağıdakiler kullanılamaz (birim).")]
    [SerializeField] private float pullHeightTolerance = 1f;

    [Header("Sallanma")]
    [Tooltip("İp uzunluğunun alt sınırı (birim).")]
    [SerializeField] private float minRopeLength = 2f;

    [Tooltip("İp uzunluğunun üst sınırı (birim).")]
    [SerializeField] private float maxRopeLength = 7f;

    [Tooltip("Bırakınca (ya da sallanma kendiliğinden bitince) fırlatma hızının çarpanı. 1 = hız aynen korunur.")]
    [SerializeField] private float releaseBoost = 1.15f;

    [Tooltip("Yay sonu (derece, 0 = noktanın tam altı, en fazla 90 = nokta hizası): E bu açıdan önce bırakılmalı; sallanma bu açıya varırsa oyuncu patlar (ölüm).")]
    [SerializeField] private float maxSwingAngle = 90f;

    [Tooltip("Tutununca ileri hıza eklenen hız (birim/sn). Sallanma boyunca bu hız sabit kalır, düşmez.")]
    [SerializeField] private float swingEntryBonus = 3f;
    [Header("Çekilme")]
    [Tooltip("Çekilme hızı (birim/sn).")]
    [SerializeField] private float pullSpeed = 35f;

    [Tooltip("Çekilme noktayı bu kadar geçince biter (birim). Oyuncu noktada durmaz, ilerisine çekilir.")]
    [SerializeField] private float pullOvershoot = 3f;

    [Tooltip("Çekilme bitince verilen ileri hız (birim/sn). 8 = normal koşu hızı (boost yok).")]
    [SerializeField] private float pullExitSpeed = 8f;

    [Tooltip("Çekilmenin bitiş noktasına bu kadar yaklaşınca çekilme biter (birim).")]
    [SerializeField] private float pullArriveDistance = 0.5f;

    // Kanca açık mı? Bölümün AbilityLoadout'undan PlayerLoadout tarafından ayarlanır
    public bool Unlocked { get; set; }
    public bool IsHooked => mode != Mode.None;
    // İp görseli için tutunulan nokta
    public Vector3 AnchorPosition => anchor;
    public bool IsSwinging => mode == Mode.Swing;
    // Sallanma açısı (derece, 0 = noktanın tam altı) ve patlama sınırı; açı göstergesi için
    public float SwingAngleDegrees => angle * Mathf.Rad2Deg;
    public float MaxSwingAngle => Mathf.Min(maxSwingAngle, 90f);

    private enum Mode { None, Swing, Pull }

    private PlayerMotor motor;
    private PlayerInputReader input;
    private PlayerDeath death;

    private Mode mode;
    private HookPoint target;
    private Vector3 anchor;
    private float ropeLength;
    // Sarkaç açısı: 0 = noktanın tam altı, pozitif = noktanın önü
    private float angle;
    // Sallanmadaki ileri (yatay) hız: giriş hızı + bonus, sallanma boyunca sabit; geri sallanma yok
    private float swingSpeed;
    private bool releaseRequested;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        input = GetComponent<PlayerInputReader>();
        death = GetComponent<PlayerDeath>();
    }

    private void OnEnable()
    {
        input.HookReleased += OnHookReleased;
        death.Died += OnDied;
    }

    private void OnDisable()
    {
        input.HookReleased -= OnHookReleased;
        death.Died -= OnDied;
        if (IsHooked) EndHook();
        SetTarget(null);
    }

    private void Update()
    {
        // Tutunmuşken hedef değişmez
        if (IsHooked) return;
        SetTarget(CanStart() ? FindTarget() : null);

        // E basılı tutuldukça denenir: nokta menzile girmeden biraz erken basılan E de çalışır
        if (target != null && input.HookHeld) TryHook();
    }

    private bool CanStart() => Unlocked && !death.IsDead && !motor.IsControlLocked;

    // Öndeki, menzil içindeki ve çok aşağıda olmayan en yakın nokta.
    // Sallanma noktaları sadece havadayken kullanılabilir (yerden sallanma oyuncuyu zemine bastırır)
    private HookPoint FindTarget()
    {
        Vector3 position = motor.Position;
        bool grounded = motor.IsGrounded;
        HookPoint best = null;
        float bestSqr = range * range;

        IReadOnlyList<HookPoint> all = HookPoint.All;
        for (int i = 0; i < all.Count; i++)
        {
            Vector3 delta = all[i].Position - position;
            if (delta.x <= 0f || delta.y < -pullHeightTolerance) continue;
            if (grounded && delta.y > pullHeightTolerance) continue;

            float sqr = delta.x * delta.x + delta.y * delta.y;
            if (sqr > bestSqr) continue;

            best = all[i];
            bestSqr = sqr;
        }
        return best;
    }

    private void SetTarget(HookPoint point)
    {
        if (point == target) return;
        if (target != null) target.SetHighlighted(false);
        target = point;
        if (target != null) target.SetHighlighted(true);
    }

    private void TryHook()
    {
        if (IsHooked || target == null || !CanStart()) return;
        if (!motor.TryAcquireControl(this)) return;
        // Metal tavan / duvar modundan çık; bu yetenek normal yerçekimiyle çalışır ve biter
        motor.ExitSurfaceModes();

        anchor = target.Position;
        anchor.z = 0f;
        Vector3 offset = motor.Position - anchor;
        offset.z = 0f;
        releaseRequested = false;

        if (-offset.y > pullHeightTolerance)
        {
            // Nokta başın üstünde: sallanma. Oyuncu yay boyunca giriş ileri hızıyla (en az koşu hızı) hep ileri gider
            ropeLength = Mathf.Clamp(offset.magnitude, minRopeLength, maxRopeLength);
            angle = Mathf.Atan2(offset.x, -offset.y);
            swingSpeed = Mathf.Max(motor.Velocity.x, motor.RunSpeed) + swingEntryBonus;
            mode = Mode.Swing;
        }
        else
        {
            mode = Mode.Pull;
        }
    }

    // Bırakma bir sonraki fizik adımında yapılır
    private void OnHookReleased()
    {
        if (IsHooked) releaseRequested = true;
    }

    private void FixedUpdate()
    {
        if (!IsHooked) return;

        // Kilit başka yerden sıfırlandıysa (ör. yeniden doğuş) kancayı bitir
        if (!motor.HasControl(this))
        {
            EndHook();
            return;
        }

        if (mode == Mode.Swing) StepSwing(Time.fixedDeltaTime);
        else StepPull(Time.fixedDeltaTime);
    }

    private void StepSwing(float dt)
    {
        // Bırakınca fırlat: ileri hız aynen (çarpanla), yukarı hız ipin açısından
        if (releaseRequested)
        {
            EndHook();
            motor.Launch(new Vector3(swingSpeed, swingSpeed * Mathf.Sin(angle), 0f) * releaseBoost);
            return;
        }

        if (motor.TouchingCeiling)
        {
            EndHook();
            death.Die();
            return;
        }

        if (motor.IsGrounded)
        {
            // Yere değdi: sallanma biter, ileri hız korunarak koşuya devam edilir
            EndHook();
            motor.Launch(new Vector3(swingSpeed, 0f, 0f));
            return;
        }

        // Yay sonuna varıldı ve E hâlâ basılı: patlama (90°'de açı tam sınıra eşit olur, küçük pay bırakılır)
        if (SwingAngleDegrees >= MaxSwingAngle - 0.01f)
        {
            // Kilit ölmeden önce bırakılır; yeniden doğuşta karakter normal koşsun
            EndHook();
            Debug.Log("[KosKos] Kanca patladı");
            death.Die();
            return;
        }

        // Konum yatayda sabit ileri hızla ilerler, yükseklik ipin çemberinden gelir (durmaz, yay sonunu geçebilir)
        float x = Mathf.Min(Mathf.Sin(angle) * ropeLength + swingSpeed * dt, ropeLength);
        angle = Mathf.Asin(x / ropeLength);

        Vector3 goal = anchor + new Vector3(Mathf.Sin(angle), -Mathf.Cos(angle), 0f) * ropeLength;
        motor.SetVelocity((goal - motor.Position) / dt);
    }

    private void StepPull(float dt)
    {
        // Bitiş noktası: kanca noktasının biraz ilerisi (oyuncu noktada durmaz, ileri çekilir)
        Vector3 end = anchor + Vector3.right * pullOvershoot;
        Vector3 toEnd = end - motor.Position;
        toEnd.z = 0f;
        float distance = toEnd.magnitude;

        // Bitişe vardıysa ya da hizasına geldiyse biter (bitiş zeminin içine denk gelse bile takılı kalmaz)
        bool arrived = distance <= pullArriveDistance || toEnd.x <= pullArriveDistance;
        if (releaseRequested || arrived)
        {
            EndHook();
            motor.Launch(new Vector3(pullExitSpeed, 0f, 0f));
            return;
        }

        // Son adımda bitişi geçmesin
        float speed = Mathf.Min(pullSpeed, distance / dt);
        motor.SetVelocity(toEnd / distance * speed);
    }

    private void EndHook()
    {
        mode = Mode.None;
        releaseRequested = false;
        motor.ReleaseControl(this);
    }

    private void OnDied()
    {
        if (IsHooked) EndHook();
        SetTarget(null);
    }
}
