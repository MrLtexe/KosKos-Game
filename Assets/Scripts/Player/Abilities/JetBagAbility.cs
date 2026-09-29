using UnityEngine;

// Jet-Çanta (C, Jetpack Joyride tarzı): W basılı tutulunca karakter koşmaya devam ederken yukarı itilir.
// İtki sürerken kontrol kilidi bu yetenektedir; başka hiçbir şey (kılıç dahil) çalışmaz. Bırakınca her şey normale döner.
// Yakıt sınırlıdır; sadece batarya alınca veya yeniden doğunca dolar (yerde kendiliğinden dolmaz).
// Motor'dan önce çalışır
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(PlayerMotor), typeof(PlayerInputReader), typeof(PlayerDeath))]
public class JetBagAbility : MonoBehaviour
{
    [Tooltip("Dolu depo ile toplam itki süresi (sn).")]
    [SerializeField] private float fuelCapacity = 2f;

    [Tooltip("İtki ivmesi (birim/sn²). Basılıyken yerçekiminin yerine geçer.")]
    [SerializeField] private float thrustAcceleration = 60f;

    [Tooltip("İtkiyle ulaşılabilen en yüksek yükselme hızı (birim/sn).")]
    [SerializeField] private float maxRiseSpeed = 8f;

    // Jet-çanta açık mı? Bölümün AbilityLoadout'undan PlayerLoadout tarafından ayarlanır
    public bool Unlocked { get; set; }
    public bool IsThrusting { get; private set; }
    // Yakıt oranı: 0 = boş, 1 = dolu (yakıt çubuğu için)
    public float Fuel01 => fuel / fuelCapacity;

    private PlayerMotor motor;
    private PlayerInputReader input;
    private PlayerDeath death;

    private float fuel;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        input = GetComponent<PlayerInputReader>();
        death = GetComponent<PlayerDeath>();
        fuel = fuelCapacity;
    }

    private void OnEnable()
    {
        death.Died += OnDied;
        death.Respawned += OnRespawned;
    }

    private void OnDisable()
    {
        death.Died -= OnDied;
        death.Respawned -= OnRespawned;
        if (IsThrusting) StopThrust();
    }

    // Batarya: depoyu tamamen doldurur
    public void Refill()
    {
        fuel = fuelCapacity;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (IsThrusting)
        {
            // W bırakıldıysa, yakıt bittiyse ya da kilit başka yerden sıfırlandıysa normale dön
            if (!input.JetBagHeld || fuel <= 0f || !Unlocked || !motor.HasControl(this))
            {
                StopThrust();
                return;
            }
        }
        else
        {
            if (!input.JetBagHeld || !CanStart()) return;
            if (!motor.TryAcquireControl(this)) return;
            IsThrusting = true;
        }

        // Yatay hız motorun koşu hızında kalır; dikey hız itkiyle artar (yerçekimi yok)
        float rise = Mathf.Min(motor.Velocity.y + thrustAcceleration * dt, maxRiseSpeed);
        motor.SetVelocity(new Vector3(motor.CurrentSpeed, rise, 0f));
        fuel = Mathf.Max(0f, fuel - dt);
    }

    // Metal tavanda veya duvar koşusunda jet-çanta kullanılamaz
    private bool CanStart() => Unlocked && fuel > 0f && !death.IsDead && !motor.IsCeilingMode && !motor.IsWallRunning;

    private void StopThrust()
    {
        IsThrusting = false;
        motor.ReleaseControl(this);
    }

    private void OnDied()
    {
        if (IsThrusting) StopThrust();
    }

    private void OnRespawned()
    {
        fuel = fuelCapacity;
    }
}
