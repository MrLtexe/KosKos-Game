using DG.Tweening;
using UnityEngine;

// Basit takip kamerası: X'te oyuncuya kilitli (oyuncu solda kalır, ileri görülür), Y'de yumuşak takip.
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Tooltip("Takip edilecek oyuncu.")]
    [SerializeField] private Transform target;

    [Tooltip("Oyuncuya göre kamera konumu. X pozitif = oyuncu ekranın solunda kalır, Z negatif = kamera geride.")]
    [SerializeField] private Vector3 offset = new Vector3(4f, 1.5f, -14f);

    [Tooltip("Dikey takibin yumuşaklığı (sn). Büyük değer = daha yavaş, daha yumuşak.")]
    [SerializeField] private float verticalSmoothTime = 0.25f;

    [Tooltip("Şarj basılı tutulurken sürekli titreşimin gücü (birim): adım 0 (şarj başladı), 1, 2, 3. Kol titreşimi gibi hep vızıldar, her adımda artar.")]
    [SerializeField] private float[] chargeRumbleStrengths = { 0.01f, 0.025f, 0.045f, 0.07f };

    [Tooltip("Boost şarjı adım 1, 2, 3'e ulaşınca sürekli titreşimin üstüne gelen vuruşun gücü (birim). Sonraki adım daha sert.")]
    [SerializeField] private float[] chargeShakeStrengths = { 0.08f, 0.16f, 0.3f };

    [Tooltip("Boost şarjı adım 1, 2, 3'e ulaşınca gelen vuruşun sönme süresi (sn).")]
    [SerializeField] private float[] chargeShakeDurations = { 0.15f, 0.2f, 0.3f };

    [Tooltip("Titreşimin hızı (saniyedeki salınım). Büyük değer = daha ince, motor gibi vızıltı; küçük değer = daha yavaş sarsıntı.")]
    [SerializeField] private float rumbleFrequency = 30f;

    [Tooltip("Boost şarjı başlayınca kameranın sağa kayması (birim).")]
    [SerializeField] private float chargeRightShift = 4f;

    [Tooltip("Sağa kaymaya girip çıkma süresi (sn). Büyük değer = daha yavaş, daha yumuşak.")]
    [SerializeField] private float chargeRightShiftTime = 0.075f;

    [Tooltip("Boost şarjı adım 1, 2, 3'e ulaşınca kameranın uzaklaşması (birim, geriye). Sallanmayla birlikte her adımda biraz daha uzaklaşır; bırakınca boost hızı söndükçe yavaşça geri gelir.")]
    [SerializeField] private float[] chargeZoomOuts = { 1f, 2f, 3.2f };

    [Tooltip("Şarj uzaklaşmasına girip çıkma süresi (sn). Büyük değer = daha yavaş, daha yumuşak.")]
    [SerializeField] private float chargeShiftTime = 0.25f;

    [Tooltip("Atılma sırasında kameranın sola kayması (birim). Oyuncu ekranda biraz öne kaçar, hız hissi verir.")]
    [SerializeField] private float dashLeftShift = 1.5f;

    [Tooltip("Atılma kaymasına girip çıkma süresi (sn). Atılma kısa olduğu için küçük tutulmalı.")]
    [SerializeField] private float dashShiftTime = 0.06f;

    private float verticalVelocity;
    // Atılma sırasındaki sola kayma (negatif X) ve yumuşatma hızı
    private float dashShift;
    private float dashShiftVelocity;
    private DashAbility targetDash;
    // Şarj sırasında kameranın şu anki kayması (sağa + geriye), hedefe yumuşakça gider (ikisi ayrı hızda)
    private Vector3 chargeShift;
    private float chargeRightVelocity;
    private float chargeZoomVelocity;
    // Bırakınca tutulan uzaklaşma ve o andaki boost fazlası (uzaklaşma boost söndükçe orantılı azalır)
    private float heldZoomOut;
    private float heldBoost;
    private PlayerMotor targetMotor;
    // Sallanmasız takip yüksekliği (sallanma yumuşak takibi bozmasın diye ayrı tutulur)
    private float followY;
    private PlayerDeath targetDeath;
    private BoostChargeAbility targetBoost;

    private float shakeStrength;
    private float shakeDuration;
    private float shakeEndTime;

    // DOTween tabanlı sarsıntı ve darbe (Punch) ofseti
    private Vector3 dotweenOffset = Vector3.zero;
    private Tweener dotweenTween;

    private void Awake()
    {
        Instance = this;
        targetDeath = target.GetComponent<PlayerDeath>();
        targetBoost = target.GetComponent<BoostChargeAbility>();
        targetMotor = target.GetComponent<PlayerMotor>();
        targetDash = target.GetComponent<DashAbility>();
    }

    private void OnDestroy()
    {
        if (dotweenTween != null && dotweenTween.IsActive()) dotweenTween.Kill();
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        if (targetDeath != null) targetDeath.Respawned += SnapToTarget;
        if (targetBoost != null) targetBoost.ChargeStepChanged += OnChargeStepChanged;
    }

    private void OnDisable()
    {
        if (targetDeath != null) targetDeath.Respawned -= SnapToTarget;
        if (targetBoost != null) targetBoost.ChargeStepChanged -= OnChargeStepChanged;
    }

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        // Şarj başlayınca kamera sağa kayar, her adımda biraz daha uzaklaşır; bırakınca geri döner
        Vector3 shiftGoal = ChargeShiftGoal();
        chargeShift = new Vector3(
            Mathf.SmoothDamp(chargeShift.x, shiftGoal.x, ref chargeRightVelocity, chargeRightShiftTime),
            0f,
            Mathf.SmoothDamp(chargeShift.z, shiftGoal.z, ref chargeZoomVelocity, chargeShiftTime));

        // Atılma boyunca kamera biraz sola kayar, bitince geri döner
        float dashGoal = targetDash != null && targetDash.IsDashing ? -dashLeftShift : 0f;
        dashShift = Mathf.SmoothDamp(dashShift, dashGoal, ref dashShiftVelocity, dashShiftTime);

        Vector3 goal = target.position + offset + chargeShift + new Vector3(dashShift, 0f, 0f);
        followY = Mathf.SmoothDamp(followY, goal.y, ref verticalVelocity, verticalSmoothTime);
        transform.position = new Vector3(goal.x, followY, goal.z) + ShakeOffset() + dotweenOffset;
    }

    // Yeniden doğuşta kamera kaymadan direkt oyuncuya atlar
    private void SnapToTarget()
    {
        transform.position = target.position + offset;
        followY = transform.position.y;
        verticalVelocity = 0f;
        shakeEndTime = 0f;
        if (dotweenTween != null && dotweenTween.IsActive()) dotweenTween.Kill();
        dotweenOffset = Vector3.zero;
        chargeShift = Vector3.zero;
        chargeRightVelocity = 0f;
        chargeZoomVelocity = 0f;
        heldZoomOut = 0f;
        dashShift = 0f;
        dashShiftVelocity = 0f;
    }

    // Şarjda: sağa kayma + ulaşılan adımın uzaklaşması (adım 0'da uzaklaşma yok).
    // Bıraktıktan sonra: sağa kayma geri döner, uzaklaşma boost söndükçe azalır, boost bitince sıfır olur.
    private Vector3 ChargeShiftGoal()
    {
        if (targetBoost != null && targetBoost.IsCharging)
        {
            float zoomOut = 0f;
            int step = targetBoost.CurrentStep;
            if (step > 0 && chargeZoomOuts.Length > 0) zoomOut = chargeZoomOuts[Mathf.Min(step, chargeZoomOuts.Length) - 1];
            heldZoomOut = zoomOut;
            heldBoost = 0f;
            return new Vector3(chargeRightShift, 0f, -zoomOut);
        }

        if (heldZoomOut <= 0f || targetMotor == null) return Vector3.zero;

        float boost = targetMotor.CurrentSpeed - targetMotor.RunSpeed;
        if (boost <= 0f)
        {
            // Boost bitti (ya da hiç verilmedi, ör. patlama)
            heldZoomOut = 0f;
            return Vector3.zero;
        }

        // Bırakıldıktan sonraki en yüksek boost'u başlangıç say; uzaklaşma ona oranla azalır
        heldBoost = Mathf.Max(heldBoost, boost);
        return new Vector3(0f, 0f, -heldZoomOut * boost / heldBoost);
    }

    // DOTween tabanlı yönlü darbe etkisi (boşa savurma, zıplama darbesi vb.)
    public void DoPunch(Vector3 punchDirection, float duration = 0.08f, int vibrato = 4, float elasticity = 0.5f)
    {
        if (dotweenTween != null && dotweenTween.IsActive()) dotweenTween.Kill();
        dotweenOffset = Vector3.zero;

        dotweenTween = DOTween.Punch(
            () => dotweenOffset,
            x => dotweenOffset = x,
            punchDirection,
            duration,
            vibrato,
            elasticity
        ).SetUpdate(true);
    }

    // DOTween tabanlı sönümlenen kaliteli sarsıntı etkisi (hedefe vurma, patlama vb.)
    public void DoShake(float duration = 0.12f, float strength = 0.25f, int vibrato = 14, float randomness = 90f)
    {
        if (dotweenTween != null && dotweenTween.IsActive()) dotweenTween.Kill();
        dotweenOffset = Vector3.zero;

        dotweenTween = DOTween.Shake(
            () => dotweenOffset,
            x => dotweenOffset = x,
            duration,
            new Vector3(strength, strength * 0.7f, 0f),
            vibrato,
            randomness,
            true, // fadeOut
            ShakeRandomnessMode.Full
        ).SetUpdate(true);
    }

    // Dışarıdan tetiklenebilen genel kamera sarsıntısı (geriye uyumluluk için korundu)
    public void Shake(float strength, float duration)
    {
        DoShake(duration, strength);
    }

    // Yeni şarj adımı: adım numarasına göre sallanma (0 = şarj başladı/bitti, sallanma yok)
    private void OnChargeStepChanged(int step)
    {
        if (step <= 0) return;
        int i = Mathf.Min(step, chargeShakeStrengths.Length, chargeShakeDurations.Length) - 1;
        if (i < 0) return;

        shakeStrength = chargeShakeStrengths[i];
        shakeDuration = chargeShakeDurations[i];
        shakeEndTime = Time.time + shakeDuration;
    }

    // Kol titreşimi gibi: şarj boyunca sürekli vızıltı + adım vuruşu (azalarak söner).
    // Rastgele zıplama yerine yumuşak gürültü kullanılır; kamera ışınlanmaz, titrer.
    private Vector3 ShakeOffset()
    {
        // Duraklatmada (zaman durunca) sallanma da durur
        if (Time.deltaTime <= 0f) return Vector3.zero;

        float strength = 0f;
        if (targetBoost != null && targetBoost.IsCharging && chargeRumbleStrengths.Length > 0)
        {
            strength += chargeRumbleStrengths[Mathf.Clamp(targetBoost.CurrentStep, 0, chargeRumbleStrengths.Length - 1)];
        }

        float remaining = shakeEndTime - Time.time;
        if (remaining > 0f && shakeDuration > 0f) strength += shakeStrength * remaining / shakeDuration;

        if (strength <= 0f) return Vector3.zero;

        // X ve Y için ayrı gürültü satırları (ikisi aynı yöne birlikte gitmesin)
        float t = Time.time * rumbleFrequency;
        float x = Mathf.PerlinNoise(t, 0.3f) * 2f - 1f;
        float y = Mathf.PerlinNoise(0.7f, t) * 2f - 1f;
        return new Vector3(x, y, 0f) * strength;
    }
}
