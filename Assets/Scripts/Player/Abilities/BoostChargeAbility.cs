using System;
using UnityEngine;

// Boost Şarjı (B.4): basılı tutunca karakter yerinde durur ve 3 adımda enerji toplar.
// Bırakınca ulaşılan adımın boost'u kadar hızlanır. Çok uzun tutarsa patlar (ölüm).
// Sadece yerdeyken başlatılabilir.
// Motor'dan önce çalışır: kilit bırakıldığı adımda motor hemen normal koşuya döner
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(PlayerMotor), typeof(PlayerInputReader), typeof(PlayerDeath))]
public class BoostChargeAbility : MonoBehaviour
{
    [Tooltip("Her şarj adımının süresi (sn).")]
    [SerializeField] private float stepDuration = 0.5f;

    [Tooltip("Adım 1, 2, 3'te bırakınca eklenen hız (birim/sn). Dizi uzunluğu adım sayısını belirler.")]
    [SerializeField] private float[] stepBoosts = { 4f, 8f, 12f };

    [Tooltip("Son adıma ulaştıktan sonra patlamadan önce tutulabilecek süre (sn).")]
    [SerializeField] private float overchargeTime = 0.75f;

    // 0 = adım yok (şarj başladı veya bitti), 1..N = ulaşılan adım. Animator buna bağlanacak.
    public event Action<int> ChargeStepChanged;

    public bool IsCharging { get; private set; }
    public int CurrentStep { get; private set; }

    private PlayerMotor motor;
    private PlayerInputReader input;
    private PlayerDeath death;

    private float chargeStartTime;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        input = GetComponent<PlayerInputReader>();
        death = GetComponent<PlayerDeath>();
    }

    private void OnEnable()
    {
        input.BoostPressed += OnBoostPressed;
        input.BoostReleased += OnBoostReleased;
        death.Died += OnDied;
    }

    private void OnDisable()
    {
        input.BoostPressed -= OnBoostPressed;
        input.BoostReleased -= OnBoostReleased;
        death.Died -= OnDied;
    }

    private void OnBoostPressed()
    {
        if (IsCharging || death.IsDead || !motor.IsGrounded) return;
        if (!motor.TryAcquireControl(this)) return;

        IsCharging = true;
        chargeStartTime = Time.fixedTime;
        SetStep(0);
        motor.SetVelocity(Vector3.zero);
    }

    private void OnBoostReleased()
    {
        if (!IsCharging) return;

        int reachedStep = CurrentStep;
        EndCharge();

        // Adım 1'e ulaşmadan bırakılırsa boost yok, koşu devam eder
        if (reachedStep > 0) motor.AddSpeed(stepBoosts[reachedStep - 1]);
    }

    private void FixedUpdate()
    {
        if (!IsCharging) return;

        // Kilit başka bir yerden sıfırlandıysa (ör. yeniden doğuş) şarjı iptal et
        if (!motor.HasControl(this))
        {
            EndCharge();
            return;
        }

        // Şarj boyunca karakter yerinde sabit
        motor.SetVelocity(Vector3.zero);

        float elapsed = Time.fixedTime - chargeStartTime;
        int maxStep = stepBoosts.Length;
        int step = Mathf.Min(maxStep, Mathf.FloorToInt(elapsed / stepDuration));
        if (step != CurrentStep) SetStep(step);

        if (elapsed >= maxStep * stepDuration + overchargeTime)
        {
            // Kilit ölmeden önce bırakılır; yeniden doğuşta karakter normal koşsun
            EndCharge();
            Debug.Log("[KosKos] Boost patladı");
            death.Die();
        }
    }

    private void EndCharge()
    {
        IsCharging = false;
        motor.ReleaseControl(this);
        SetStep(0);
    }

    private void SetStep(int step)
    {
        CurrentStep = step;
        ChargeStepChanged?.Invoke(step);
    }

    private void OnDied()
    {
        if (IsCharging) EndCharge();
    }
}
