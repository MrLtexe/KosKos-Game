using UnityEngine;

// Zıplama (B.2) ve çift zıplama (B.2.1).
// Coyote time: kenardan düştükten hemen sonra hâlâ zıplanabilir.
// Jump buffer: yere değmeden hemen önce basılan zıplama, yere değince gerçekleşir.
[RequireComponent(typeof(PlayerMotor), typeof(PlayerInputReader), typeof(PlayerDeath))]
public class JumpAbility : MonoBehaviour
{
    [Header("Zıplama")]
    [Tooltip("Zıplamanın ulaşacağı yükseklik (birim).")]
    [SerializeField] private float jumpHeight = 3f;

    [Tooltip("Yerde iki zıplama arasındaki bekleme süresi (sn).")]
    [SerializeField] private float groundJumpCooldown = 0.2f;

    [Tooltip("Kenardan düştükten sonra hâlâ zıplanabilen süre (sn). 0 = kapalı.")]
    [SerializeField] private float coyoteTime = 0.1f;

    [Tooltip("Yere değmeden bu kadar önce basılan zıplama, yere değince gerçekleşir (sn). 0 = kapalı.")]
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Çift Zıplama")]
    [Tooltip("Çift zıplama açıldı mı? (Oyunda ilerledikçe açılır.)")]
    [SerializeField] private bool doubleJumpUnlocked;

    [Tooltip("Havadaki ikinci zıplamanın yüksekliği (birim).")]
    [SerializeField] private float doubleJumpHeight = 2.5f;

    private PlayerMotor motor;
    private PlayerInputReader input;
    private PlayerDeath death;

    private float lastGroundJumpTime = float.NegativeInfinity;
    private float leftGroundTime = float.NegativeInfinity;
    private float bufferedUntil = float.NegativeInfinity;
    private bool jumpedSinceGrounded;
    private bool airJumpUsed;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        input = GetComponent<PlayerInputReader>();
        death = GetComponent<PlayerDeath>();
    }

    private void OnEnable()
    {
        input.JumpPressed += OnJumpPressed;
        motor.Landed += OnLanded;
        motor.LeftGround += OnLeftGround;
        death.Respawned += ResetState;
    }

    private void OnDisable()
    {
        input.JumpPressed -= OnJumpPressed;
        motor.Landed -= OnLanded;
        motor.LeftGround -= OnLeftGround;
        death.Respawned -= ResetState;
    }

    private void OnJumpPressed()
    {
        // Şu an zıplanamıyorsa basışı kısa süre sakla
        if (!TryJump())
        {
            bufferedUntil = Time.time + jumpBufferTime;
        }
    }

    private bool TryJump()
    {
        if (death.IsDead || motor.IsControlLocked) return false;

        // Yer zıplaması (coyote dahil) önceliklidir
        if (CanGroundJump())
        {
            motor.Jump(jumpHeight);
            lastGroundJumpTime = Time.time;
            jumpedSinceGrounded = true;
            return true;
        }

        if (doubleJumpUnlocked && !motor.IsGrounded && !airJumpUsed)
        {
            motor.Jump(doubleJumpHeight);
            airJumpUsed = true;
            return true;
        }

        return false;
    }

    private bool CanGroundJump()
    {
        if (Time.time < lastGroundJumpTime + groundJumpCooldown) return false;
        if (motor.IsGrounded) return true;

        // Coyote time: zıplamadan kenardan düştüysek kısa süre hâlâ yer zıplaması hakkı var
        return !jumpedSinceGrounded && Time.time <= leftGroundTime + coyoteTime;
    }

    private void OnLanded()
    {
        jumpedSinceGrounded = false;
        airJumpUsed = false;

        if (Time.time <= bufferedUntil)
        {
            bufferedUntil = float.NegativeInfinity;
            TryJump();
        }
    }

    private void OnLeftGround()
    {
        leftGroundTime = Time.time;
    }

    private void ResetState()
    {
        lastGroundJumpTime = float.NegativeInfinity;
        leftGroundTime = float.NegativeInfinity;
        bufferedUntil = float.NegativeInfinity;
        jumpedSinceGrounded = false;
        airJumpUsed = false;
    }
}
