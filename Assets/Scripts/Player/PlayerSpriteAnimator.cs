using UnityEngine;

// 2B karakterin hangi karede göründüğünü oyunun gerçek durumuna göre seçer (oyuncunun "Visual" çocuğunda durur).
// Yerde / duvarda / tavanda: Animator koşu döngüsünü oynatır.
// Havada: Animator durdurulur, kareler buradan seçilir (zıplamanın gerçek yüksekliğine göre, kısa zıplamada da uzunda da doğru görünür):
//   kalkış (yerden zıplama): koşu 3 → yükseliş: zıplama 1, 2 (birer kare) → 3 (yükselişin geri kalanı ve tepe boyunca tutulur)
//   → düşmeye başlayınca kısa süre: 4 → düşüş: 5 (yere değene kadar).
// Havada ikinci zıplamada kalkış karesi olmadan 1'den başlar. Zıplamadan düşerken (kenardan) doğrudan 5 gösterilir.
// Atılma: dash 1, 2, 3 (hızlı geçiş) → 4 (atılma bitene kadar tutulur). Bitince yerdeyse koşu 1'den, havadaysa düşüş karesinden devam.
// Kareler setup aracıyla atanır: KosKos > Animation > Setup Player Run Animation.
[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class PlayerSpriteAnimator : MonoBehaviour
{
    [Header("Kareler")]
    [Tooltip("Yerden zıplarken ilk gösterilen geçiş karesi (koşu 3).")]
    [SerializeField] private Sprite takeoffFrame;

    [Tooltip("Yükseliş kareleri, sırayla (zıplama 1, 2). Her biri bir kare süresi gösterilir, ardından tepe karesi tutulur.")]
    [SerializeField] private Sprite[] riseFrames;

    [Tooltip("Tepe karesi (zıplama 3): yükseliş kareleri bitince düşmeye başlayana kadar tutulur.")]
    [SerializeField] private Sprite peakFrame;

    [Tooltip("Tepeden düşüşe geçiş karesi (zıplama 4).")]
    [SerializeField] private Sprite peakToFallFrame;

    [Tooltip("Düşüş karesi (zıplama 5): yere değene kadar kalır.")]
    [SerializeField] private Sprite fallFrame;

    [Tooltip("Atılmaya geçiş kareleri, sırayla (dash 1, 2, 3). Her biri bir geçiş karesi süresi gösterilir.")]
    [SerializeField] private Sprite[] dashTransitionFrames;

    [Tooltip("Atılma karesi (dash 4): geçiş kareleri bitince atılma bitene kadar tutulur.")]
    [SerializeField] private Sprite dashHoldFrame;

    [Header("Zamanlama")]
    [Tooltip("Atılma geçiş karelerinin her birinin süresi (sn). 1/36 = koşu karesinden 3 kat hızlı; atılma 0.2 sn sürdüğü için kısa tutulmalı.")]
    [SerializeField] private float dashTransitionFrameTime = 1f / 36f;

    [Tooltip("Atılma bittikten sonra atılma karesinin (dash 4) ekranda kalmaya devam ettiği süre (sn). Sadece görsel; atılmanın hızı ve mesafesi değişmez.")]
    [SerializeField] private float dashHoldAfterTime = 0.1f;

    [Tooltip("Kalkış karesinin (koşu 3) ekranda kaldığı süre (sn). 1/12 = koşu animasyonunun bir karesi.")]
    [SerializeField] private float takeoffTime = 1f / 12f;

    [Tooltip("Yükseliş karelerinin her birinin süresi (sn).")]
    [SerializeField] private float riseFrameTime = 1f / 12f;

    [Tooltip("Dikey hız bu değerin altına inince tepe karesi gösterilir (birim/sn). Büyük = tepe karesi daha uzun görünür.")]
    [SerializeField] private float peakSpeed = 2.5f;

    [Tooltip("Tepeden sonra geçiş karesinin (4) ekranda kaldığı süre (sn).")]
    [SerializeField] private float peakToFallTime = 1f / 12f;

    [Tooltip("Koşu durumunun Animator'daki adı.")]
    [SerializeField] private string runStateName = "Run";

    [Header("Koşu Hızı")]
    [Tooltip("Koşu animasyonu oyuncunun hızına göre hızlanıp yavaşlasın mı? Normal koşu hızında 1x oynar (ör. iki kat hızda 2x).")]
    [SerializeField] private bool matchRunSpeed = true;

    [Tooltip("Koşu animasyonunun en yavaş oynama hızı (çarpan). Çok yavaş koşarken bacaklar donmuş gibi görünmesin.")]
    [SerializeField] private float minRunAnimSpeed = 0.5f;

    [Tooltip("Koşu animasyonunun en hızlı oynama hızı (çarpan). Boost'ta bacaklar bulanıklaşmasın.")]
    [SerializeField] private float maxRunAnimSpeed = 2.5f;

    private enum AirPhase { Takeoff, Rise, Peak, PeakToFall, Fall }

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private PlayerMotor motor;
    private JumpAbility jump;
    private DashAbility dash;

    // Zıplanan karede motor hâlâ "yerde" diyebilir; bu süre boyunca yere değme sayılmaz (kalkış karesi iptal olmasın)
    private const float JumpGroundGrace = 0.1f;

    private bool inAir;
    private AirPhase phase;
    private float phaseStartTime;
    private float ignoreGroundUntil;
    private bool dashing;
    private float dashStartTime;
    // Atılmanın (fiziğin) bittiği an; atılma sürerken sonsuz
    private float dashReleasedTime;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        motor = GetComponentInParent<PlayerMotor>();
        jump = GetComponentInParent<JumpAbility>();
        dash = GetComponentInParent<DashAbility>();
    }

    private void OnEnable()
    {
        if (jump != null) jump.Jumped += OnJumped;
    }

    private void OnDisable()
    {
        if (jump != null) jump.Jumped -= OnJumped;
    }

    private void LateUpdate()
    {
        if (motor == null) return;

        // Atılma sürerken ve bittikten sonra kısa süre kareleri atılma seçer (yerde de havada da)
        bool dashActive = dash != null && dash.IsDashing && dashHoldFrame != null;
        if (dashActive)
        {
            // Yeni atılma (ya da önceki atılmanın bekleme süresinde yeni atılma): geçiş karelerinden başla
            if (!dashing || dashReleasedTime < float.PositiveInfinity) StartDash();
        }
        else if (dashing && dashReleasedTime == float.PositiveInfinity)
        {
            dashReleasedTime = Time.time;
        }
        if (dashing && Time.time < dashReleasedTime + dashHoldAfterTime)
        {
            spriteRenderer.sprite = CurrentDashFrame();
            return;
        }

        bool onSurface = (motor.IsGrounded || motor.IsWallRunning) && Time.time >= ignoreGroundUntil;
        if (dashing) EndDash(onSurface);
        if (onSurface)
        {
            if (inAir) Land();
            UpdateRunSpeed();
            return;
        }

        // Zıplamadan havaya çıkıldıysa (kenardan düşme) doğrudan düşüş
        if (!inAir) EnterAir(UpSpeed > peakSpeed ? AirPhase.Rise : AirPhase.Fall);
        UpdateAirPhase();
        spriteRenderer.sprite = CurrentAirFrame();
    }

    // Zıplama olayı LateUpdate'ten önce gelir: kalkış karesiyle (yerden) ya da 1. kareyle (havada) başlat
    private void OnJumped(bool airJump)
    {
        ignoreGroundUntil = Time.time + JumpGroundGrace;
        // Atılma bitti ama kare hâlâ tutuluyorsa zıplama kareleri hemen başlasın
        if (dashing && !dash.IsDashing) dashing = false;
        EnterAir(airJump ? AirPhase.Rise : AirPhase.Takeoff);
    }

    private void EnterAir(AirPhase startPhase)
    {
        inAir = true;
        // Havadayken kareleri bu bileşen seçer; Animator koşu karesini üstüne yazmasın
        animator.enabled = false;
        SetPhase(startPhase);
    }

    private void Land()
    {
        inAir = false;
        animator.enabled = true;
        animator.Play(runStateName, 0, 0f);
    }

    private void StartDash()
    {
        dashing = true;
        dashStartTime = Time.time;
        dashReleasedTime = float.PositiveInfinity;
        // Atılmada kareleri bu bileşen seçer; Animator koşu karesini üstüne yazmasın
        animator.enabled = false;
    }

    // Atılma bitti: yerdeyse koşu her zaman 1. kareden başlar, havadaysa doğrudan düşüş karesi
    private void EndDash(bool onSurface)
    {
        dashing = false;
        if (onSurface)
        {
            // Land() koşuyu 0. andan (koşu 1) başlatır ve Animator'u açar
            inAir = true;
        }
        else
        {
            EnterAir(AirPhase.Fall);
        }
    }

    private Sprite CurrentDashFrame()
    {
        int count = dashTransitionFrames != null ? dashTransitionFrames.Length : 0;
        int index = Mathf.FloorToInt((Time.time - dashStartTime) / dashTransitionFrameTime);
        return index < count ? dashTransitionFrames[index] : dashHoldFrame;
    }

    // Koşu animasyonunun oynama hızı = yatay hız / normal koşu hızı (sınırlarla). Duvar koşusunda dikey hız da sayılır
    private void UpdateRunSpeed()
    {
        if (!matchRunSpeed || motor.RunSpeed <= 0f)
        {
            animator.speed = 1f;
            return;
        }
        Vector3 v = motor.Velocity;
        float speed = motor.IsWallRunning ? ((Vector2)v).magnitude : Mathf.Abs(v.x);
        animator.speed = Mathf.Clamp(speed / motor.RunSpeed, minRunAnimSpeed, maxRunAnimSpeed);
    }

    private void UpdateAirPhase()
    {
        float elapsed = Time.time - phaseStartTime;
        float up = UpSpeed;
        switch (phase)
        {
            case AirPhase.Takeoff:
                if (elapsed >= takeoffTime) SetPhase(AirPhase.Rise);
                break;
            case AirPhase.Rise:
                // Yükseliş kareleri (1, 2) birer kare gösterilir, sonra yükseliş sürse de tepe karesi (3) tutulur
                // (2'de beklemek "yaylanma" gibi görünüyordu). Kısa zıplamada kareler bitmeden de tepeye geçilir
                int riseCount = riseFrames != null ? riseFrames.Length : 0;
                if (elapsed >= riseCount * riseFrameTime || up <= peakSpeed) SetPhase(AirPhase.Peak);
                break;
            case AirPhase.Peak:
                if (up < -peakSpeed) SetPhase(AirPhase.PeakToFall);
                break;
            case AirPhase.PeakToFall:
                if (elapsed >= peakToFallTime) SetPhase(AirPhase.Fall);
                break;
        }
    }

    private Sprite CurrentAirFrame()
    {
        switch (phase)
        {
            case AirPhase.Takeoff:
                return takeoffFrame != null ? takeoffFrame : FirstRiseFrame();
            case AirPhase.Rise:
                if (riseFrames == null || riseFrames.Length == 0) return peakFrame;
                int index = Mathf.Min(Mathf.FloorToInt((Time.time - phaseStartTime) / riseFrameTime), riseFrames.Length - 1);
                return riseFrames[index];
            case AirPhase.Peak:
                return peakFrame;
            case AirPhase.PeakToFall:
                return peakToFallFrame;
            default:
                return fallFrame;
        }
    }

    private Sprite FirstRiseFrame() => riseFrames != null && riseFrames.Length > 0 ? riseFrames[0] : peakFrame;

    private void SetPhase(AirPhase next)
    {
        phase = next;
        phaseStartTime = Time.time;
    }

    // Tavandayken "yukarı" ters döner
    private float UpSpeed => motor.Velocity.y * (motor.IsCeilingMode ? -1f : 1f);
}
