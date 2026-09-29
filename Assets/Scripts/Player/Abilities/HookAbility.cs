using System.Collections.Generic;
using UnityEngine;

// Kanca (C): E basılı tutulunca öndeki en yakın kanca noktasına tutunur.
// Nokta başın üstündeyse sallanma (sarkaç), oyuncuyla aynı hizadaysa çekilme.
// Sallanırken E bırakılınca ileri gidiyorsak teğet hızla fırlatılır, geri gidiyorsak sadece düşer.
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

    [Tooltip("İleri doğru bırakınca teğet hızın çarpanı. 1 = hız aynen korunur.")]
    [SerializeField] private float releaseBoost = 1.15f;

    [Tooltip("Sarkacı çeken yerçekimi (birim/sn²). Motorun yerçekimiyle aynı tutulması önerilir.")]
    [SerializeField] private float swingGravity = 40f;

    [Header("Çekilme")]
    [Tooltip("Noktaya doğru çekilme hızı (birim/sn).")]
    [SerializeField] private float pullSpeed = 22f;

    [Tooltip("Çekilme bitince verilen ileri hız (birim/sn).")]
    [SerializeField] private float pullExitSpeed = 8f;

    [Tooltip("Noktaya bu kadar yaklaşınca çekilme biter (birim).")]
    [SerializeField] private float pullArriveDistance = 0.5f;

    // Kanca açık mı? Bölümün AbilityLoadout'undan PlayerLoadout tarafından ayarlanır
    public bool Unlocked { get; set; }
    public bool IsHooked => mode != Mode.None;
    // İp görseli için tutunulan nokta
    public Vector3 AnchorPosition => anchor;

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
    private float angularVelocity;
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
            // Nokta başın üstünde: sallanma. Başlangıç açısal hızı oyuncunun ipe teğet hızından gelir
            ropeLength = Mathf.Clamp(offset.magnitude, minRopeLength, maxRopeLength);
            angle = Mathf.Atan2(offset.x, -offset.y);
            angularVelocity = Vector3.Dot(motor.Velocity, Tangent()) / ropeLength;
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
        Vector3 velocity = Tangent() * (angularVelocity * ropeLength);

        if (releaseRequested)
        {
            EndHook();
            // İleri gidiyorsak teğet hızla fırlat, geri gidiyorsak sadece düş
            if (velocity.x > 0f) motor.Launch(velocity * releaseBoost);
            else Drop(velocity);
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
            // Yere değdi: sallanma biter, geri bırakma gibi düşer
            EndHook();
            Drop(velocity);
            return;
        }

        // Sarkaç: açısal ivme = -(g / L) * sin(açı)
        angularVelocity += -(swingGravity / ropeLength) * Mathf.Sin(angle) * dt;
        angle += angularVelocity * dt;

        Vector3 goal = anchor + new Vector3(Mathf.Sin(angle), -Mathf.Cos(angle), 0f) * ropeLength;
        motor.SetVelocity((goal - motor.Position) / dt);
    }

    private void StepPull(float dt)
    {
        Vector3 toPoint = anchor - motor.Position;
        toPoint.z = 0f;
        float distance = toPoint.magnitude;

        // Noktaya vardıysa ya da hizasına geldiyse biter (nokta zeminin içine denk gelse bile takılı kalmaz)
        bool arrived = distance <= pullArriveDistance || toPoint.x <= pullArriveDistance;
        if (releaseRequested || arrived)
        {
            EndHook();
            motor.Launch(new Vector3(pullExitSpeed, 0f, 0f));
            return;
        }

        // Son adımda noktayı geçmesin
        float speed = Mathf.Min(pullSpeed, distance / dt);
        motor.SetVelocity(toPoint / distance * speed);
    }

    // İpe teğet birim yön (açı artınca gidilen yön)
    private Vector3 Tangent() => new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

    // Yatay hız sıfırlanır, sadece aşağı yöndeki hız korunur; motor sonra normal koşuya döner
    private void Drop(Vector3 velocity)
    {
        motor.Launch(new Vector3(0f, Mathf.Min(velocity.y, 0f), 0f));
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
