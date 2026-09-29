using UnityEngine;

// Zıplama (B.2) ve çift zıplama (B.2.1).
// Coyote time: kenardan düştükten hemen sonra hâlâ zıplanabilir.
// Jump buffer: yere değmeden hemen önce basılan zıplama, yere değince gerçekleşir.
// Fırlatma alanı ek hava zıplaması verir; zipline'dan çıkış hava zıplamalarını yere inene kadar kapatır.
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

    [Tooltip("Havadaki ikinci zıplamanın (ve fırlatma alanı ek zıplamasının) yüksekliği (birim).")]
    [SerializeField] private float doubleJumpHeight = 2.5f;

    public float JumpHeight => jumpHeight;

    private PlayerMotor motor;
    private PlayerInputReader input;
    private PlayerDeath death;

    private float lastGroundJumpTime = float.NegativeInfinity;
    private float leftGroundTime = float.NegativeInfinity;
    private float bufferedUntil = float.NegativeInfinity;
    private bool jumpedSinceGrounded;
    private bool airJumpUsed;
    // Fırlatma alanından gelen, çift zıplama kilitli olsa bile kullanılabilen ek hava zıplaması
    private bool bonusAirJump;
    // Zipline'dan çıkınca yere inene kadar hiçbir hava zıplaması yok
    private bool airJumpsBlocked;

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

        if (motor.IsGrounded || airJumpsBlocked) return false;

        // Önce fırlatma alanının ek zıplaması, sonra çift zıplama
        if (bonusAirJump)
        {
            motor.Jump(doubleJumpHeight);
            bonusAirJump = false;
            return true;
        }

        if (doubleJumpUnlocked && !airJumpUsed)
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

    // Fırlatma alanı: bir ek hava zıplaması ver. Fırlatma "zıplamış" sayılır (coyote yer zıplaması fırlatmayı ezmesin).
    public void GrantBonusAirJump()
    {
        bonusAirJump = true;
        airJumpsBlocked = false;
        jumpedSinceGrounded = true;
        // Aynı karede hâlâ "yerde" görünürken basılan Space fırlatmayı yer zıplamasıyla ezmesin (bekleme süresi devreye girer)
        lastGroundJumpTime = Time.time;
    }

    // Sapan fırlatması / zipline'dan zıplama gibi Space'i kendisi kullanan eylemler, saklanan basışı iptal eder
    public void ClearBufferedJump()
    {
        bufferedUntil = float.NegativeInfinity;
    }

    // Zipline'dan çıkış: yere inene kadar hiçbir hava zıplaması yok
    public void BlockAirJumpsUntilLanded()
    {
        airJumpsBlocked = true;
        bonusAirJump = false;
        jumpedSinceGrounded = true;
    }

    private void OnLanded()
    {
        jumpedSinceGrounded = false;
        airJumpUsed = false;
        bonusAirJump = false;
        airJumpsBlocked = false;

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
        bonusAirJump = false;
        airJumpsBlocked = false;
    }
}
