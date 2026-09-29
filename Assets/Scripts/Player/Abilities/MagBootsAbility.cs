using UnityEngine;

// Manyetik Botlar (C): kontrol kilidi kullanmaz, motorun yüzey modlarını açıp kapatır.
// Yerdeyken üstte (menzilde) metal tavan varsa Q: yerçekimi ters döner, tavanda baş aşağı koşulur.
// Havada metal duvar alanındayken Q: duvar koşusu (yerçekimi yok, zeminde sayılır, atılma yok).
// Q tekrar basılınca, tavan bitince ya da duvar alanından çıkınca normale dönülür.
// Motor'dan önce çalışır: mod bittiği adımda motor normal yerçekimini uygular
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(PlayerMotor), typeof(PlayerInputReader), typeof(PlayerDeath))]
[RequireComponent(typeof(CapsuleCollider))]
public class MagBootsAbility : MonoBehaviour
{
    [Tooltip("Metal tavanın aranacağı en uzak mesafe (birim). Tavan modunda tavanın hâlâ üstte olup olmadığı da bu mesafeyle kontrol edilir.")]
    [SerializeField] private float ceilingRange = 8f;

    [Tooltip("Q metal tavanın/duvarın biraz öncesinde basılırsa bu süre boyunca bekletilir; altına/içine girilince çalışır (sn). 0 = kapalı.")]
    [SerializeField] private float pressBufferTime = 0.25f;

    // Manyetik botlar açık mı? Bölümün AbilityLoadout'undan PlayerLoadout tarafından ayarlanır
    public bool Unlocked { get; set; }

    private PlayerMotor motor;
    private PlayerInputReader input;
    private PlayerDeath death;
    private CapsuleCollider capsule;
    private MetalWall currentWall;
    private float bufferedUntil = float.NegativeInfinity;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        capsule = GetComponent<CapsuleCollider>();
        input = GetComponent<PlayerInputReader>();
        death = GetComponent<PlayerDeath>();
    }

    private void OnEnable()
    {
        input.MagBootsPressed += OnMagBootsPressed;
    }

    private void OnDisable()
    {
        input.MagBootsPressed -= OnMagBootsPressed;
    }

    // MetalWall trigger'ı çağırır
    public void SetInsideWall(MetalWall wall, bool inside)
    {
        if (inside) currentWall = wall;
        else if (currentWall == wall) currentWall = null;
    }

    private void OnMagBootsPressed()
    {
        if (death.IsDead) return;

        // Açık bir modu Q her zaman kapatır
        if (motor.IsCeilingMode)
        {
            motor.SetGravityFlipped(false);
            return;
        }
        if (motor.IsWallRunning)
        {
            motor.SetWallRun(false);
            return;
        }

        // Şu an başlatılamıyorsa basışı kısa süre sakla (FixedUpdate'te tekrar denenir)
        if (!TryActivate()) bufferedUntil = Time.time + pressBufferTime;
    }

    private bool TryActivate()
    {
        if (!Unlocked || death.IsDead || motor.IsControlLocked) return false;

        if (motor.IsGrounded)
        {
            if (!MetalCeilingAbove()) return false;
            motor.SetGravityFlipped(true);
            return true;
        }

        if (currentWall == null) return false;
        motor.SetWallRun(true);
        return true;
    }

    private void FixedUpdate()
    {
        // Erken basılan Q: metal tavanın altına / duvar alanına girilince çalışır
        if (Time.time <= bufferedUntil && !motor.IsCeilingMode && !motor.IsWallRunning && TryActivate())
        {
            bufferedUntil = float.NegativeInfinity;
        }

        // Metal tavan bittiyse ya da metal olmayan bir yüzeye değdiyse normale dön.
        // Tavandan "aşağı" zıplamak tavanı menzilden çıkarmaz, mod devam eder.
        if (motor.IsCeilingMode && !MetalCeilingAbove()) motor.SetGravityFlipped(false);

        // Duvar alanından çıkınca duvar koşusu biter
        if (motor.IsWallRunning && currentWall == null) motor.SetWallRun(false);
    }

    // Dünyanın yukarı yönünde (+Y), menzil içindeki ilk yüzey metal tavan mı?
    // Tavan modunda karakterin "yukarı"sı zemine bakar; tavan ise hep dünyanın yukarısındadır.
    // Karakter genişliğinde tarama: vücudun herhangi bir kısmı tavanın altına girince yeterli
    private bool MetalCeilingAbove()
    {
        return Physics.SphereCast(motor.Position, capsule.radius, Vector3.up, out RaycastHit hit, ceilingRange,
                   motor.GroundLayers, QueryTriggerInteraction.Ignore)
               && hit.collider.GetComponentInParent<MetalCeiling>() != null;
    }
}
