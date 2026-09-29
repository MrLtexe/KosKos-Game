using System;
using UnityEngine;

// Zipline sürme (D.6): hatta otomatik tutunur, sabit hızla ilerler, A/D ile denge tutulur.
// Denge "devrilme" modelidir: imleç merkezden uzaklaştıkça daha hızlı kayar, rastgele itmeler de vardır.
// Çıkışlar: hattın sonu (momentumla devam), zıplama (hava zıplaması yok, atılma var), dengeyi kaybetme (zıplama da atılma da yok).
// Motor'dan önce çalışır: bırakıldığı adımda motor hemen normal harekete döner
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(PlayerMotor), typeof(PlayerInputReader), typeof(PlayerDeath))]
[RequireComponent(typeof(JumpAbility), typeof(DashAbility))]
public class ZiplineRider : MonoBehaviour
{
    [Header("Sürüş")]
    [Tooltip("Hat boyunca ilerleme hızı (birim/sn).")]
    [SerializeField] private float rideSpeed = 8f;

    [Tooltip("Oyuncu merkezinin hattın ne kadar altında asılı durduğu (birim).")]
    [SerializeField] private float hangOffset = 1.2f;

    [Header("Denge")]
    [Tooltip("Devrilme gücü: imleç merkezden uzaklaştıkça ne kadar hızlı kayar. Büyük = daha zor.")]
    [SerializeField] private float instability = 3f;

    [Tooltip("Rastgele itmelerin gücü.")]
    [SerializeField] private float noise = 1.5f;

    [Tooltip("Rastgele itmenin yön değiştirme aralığı (sn).")]
    [SerializeField] private float noiseInterval = 0.4f;

    [Tooltip("A/D tuşlarının imleci itme gücü.")]
    [SerializeField] private float pushStrength = 6f;

    [Tooltip("İmleç hızının sönümlenmesi. Büyük = daha kontrollü.")]
    [SerializeField] private float damping = 1.5f;

    // Sürüş başladı/bitti (görsel bar buna bağlı)
    public event Action<bool> RidingChanged;
    // Denge imlecinin konumu: -1 (sol uç) .. 1 (sağ uç)
    public event Action<float> BalanceChanged;

    public bool IsRiding { get; private set; }

    private PlayerMotor motor;
    private PlayerInputReader input;
    private PlayerDeath death;
    private JumpAbility jump;
    private DashAbility dash;

    private Zipline current;
    private float distance;
    private float balanceOffset;
    private float balanceVelocity;
    private float noiseSign = 1f;
    private float nextNoiseTime;
    private bool jumpOffRequested;

    // Ayrılınan hat, oyuncu yere inene kadar tekrar tutunulamaz (zıplayarak çıkış gerçek bir çıkış olsun)
    private Zipline blockedZipline;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        input = GetComponent<PlayerInputReader>();
        death = GetComponent<PlayerDeath>();
        jump = GetComponent<JumpAbility>();
        dash = GetComponent<DashAbility>();
    }

    private void OnEnable()
    {
        input.JumpPressed += OnJumpPressed;
        motor.Landed += ClearBlockedZipline;
        death.Died += OnDied;
        death.Respawned += ClearBlockedZipline;
    }

    private void OnDisable()
    {
        input.JumpPressed -= OnJumpPressed;
        motor.Landed -= ClearBlockedZipline;
        death.Died -= OnDied;
        death.Respawned -= ClearBlockedZipline;
    }

    public void TryGrab(Zipline zipline)
    {
        if (IsRiding || death.IsDead) return;
        if (zipline == blockedZipline) return;
        if (!motor.TryAcquireControl(this)) return;

        current = zipline;
        distance = zipline.DistanceAlong(motor.Position);
        balanceOffset = 0f;
        balanceVelocity = 0f;
        noiseSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        nextNoiseTime = Time.time + noiseInterval;
        jumpOffRequested = false;

        IsRiding = true;
        RidingChanged?.Invoke(true);
        BalanceChanged?.Invoke(0f);
    }

    // Zıplama bir sonraki fizik adımında yapılır; aynı tuş basışı JumpAbility'de ikinci bir zıplama tetiklemesin
    private void OnJumpPressed()
    {
        if (IsRiding) jumpOffRequested = true;
    }

    private void FixedUpdate()
    {
        if (!IsRiding) return;

        // Kilit başka yerden sıfırlandıysa (ör. yeniden doğuş) sürüşü bitir
        if (!motor.HasControl(this))
        {
            StopRiding();
            return;
        }

        float dt = Time.fixedDeltaTime;

        if (jumpOffRequested)
        {
            StopRiding();
            motor.Jump(jump.JumpHeight);
            jump.BlockAirJumpsUntilLanded();
            jump.ClearBufferedJump();
            return;
        }

        distance += rideSpeed * dt;
        if (distance >= current.Length)
        {
            // Hattın sonu: hat yönünde momentumla devam
            Vector3 exitVelocity = current.Direction * rideSpeed;
            StopRiding();
            motor.Launch(exitVelocity);
            return;
        }

        UpdateBalance(dt);
        if (Mathf.Abs(balanceOffset) >= 1f)
        {
            // Denge kaybı: düş, yere inene kadar zıplama ve atılma yok
            StopRiding();
            jump.BlockAirJumpsUntilLanded();
            dash.BlockUntilLanded();
            return;
        }

        // Hattın altında asılı konuma bir adımda ulaşacak hız
        Vector3 target = current.PointAt(distance) + Vector3.down * hangOffset;
        motor.SetVelocity((target - motor.Position) / dt);
    }

    private void UpdateBalance(float dt)
    {
        if (Time.time >= nextNoiseTime)
        {
            noiseSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            nextNoiseTime = Time.time + noiseInterval;
        }

        // Devrilme + rastgele itme + oyuncunun A/D itmesi
        float acceleration = instability * balanceOffset + noise * noiseSign + input.BalanceInput * pushStrength;
        balanceVelocity += acceleration * dt;
        balanceVelocity *= Mathf.Max(0f, 1f - damping * dt);
        balanceOffset += balanceVelocity * dt;

        BalanceChanged?.Invoke(balanceOffset);
    }

    private void StopRiding()
    {
        IsRiding = false;
        blockedZipline = current;
        current = null;
        jumpOffRequested = false;
        motor.ReleaseControl(this);
        RidingChanged?.Invoke(false);
    }

    private void ClearBlockedZipline()
    {
        blockedZipline = null;
    }

    private void OnDied()
    {
        if (IsRiding) StopRiding();
    }
}
