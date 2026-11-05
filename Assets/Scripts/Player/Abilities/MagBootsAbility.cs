using System;
using UnityEngine;

// Manyetik Botlar (C): kontrol kilidi kullanmaz, motorun yüzey modlarını açıp kapatır.
// Yerdeyken ya da havadayken (zıplarken) üstte (menzilde) metal tavan varsa Q: yerçekimi ters döner, tavanda baş aşağı koşulur.
// Havada metal duvar alanındayken Q: duvar koşusu (yerçekimi yok, zeminde sayılır, atılma yok); W ile yukarı, S ile aşağı tırmanılır.
// Q tekrar basılınca, tavan bitince ya da duvar alanından çıkınca normale dönülür.
// Motor'dan önce çalışır: mod bittiği adımda motor normal yerçekimini uygular
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(PlayerMotor), typeof(PlayerInputReader), typeof(PlayerDeath))]
public class MagBootsAbility : MonoBehaviour
{
    [Tooltip("Metal tavanın aranacağı en uzak mesafe (birim). Tavan modunda tavanın hâlâ üstte olup olmadığı da bu mesafeyle kontrol edilir.")]
    [SerializeField] private float ceilingRange = 8f;

    [Tooltip("Q metal tavanın/duvarın biraz öncesinde basılırsa bu süre boyunca bekletilir; altına/içine girilince çalışır (sn). 0 = kapalı.")]
    [SerializeField] private float pressBufferTime = 0.25f;

    // Manyetik botlar açık mı? Bölümün AbilityLoadout'undan PlayerLoadout tarafından ayarlanır
    public bool Unlocked { get; set; }

    // Botlar gerçekten tutundu (tavan ya da duvar modu açıldı); Q'ya basılıp tutunulamazsa tetiklenmez. Gösterge buna bağlı
    public event Action Activated;

    private PlayerMotor motor;
    private PlayerInputReader input;
    private PlayerDeath death;
    private MetalWall currentWall;
    private float bufferedUntil = float.NegativeInfinity;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
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

        // Havada metal duvar alanındaysak önce duvar koşusu
        if (!motor.IsGrounded && currentWall != null)
        {
            motor.SetWallRun(true);
            Activated?.Invoke();
            return true;
        }

        // Yerde ya da havada (zıplarken) üstte metal tavan varsa: yerçekimi ters döner, karakter tavana "düşer"
        if (!MetalCeilingAbove()) return false;
        motor.SetGravityFlipped(true);
        Activated?.Invoke();
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

        // Duvar alanından çıkınca duvar koşusu biter (yukarı/aşağı tırmanarak alanın kenarından çıkmak da dahil)
        if (motor.IsWallRunning && currentWall == null) motor.SetWallRun(false);

        // Duvar koşusunda W (jet-çanta tuşu) yukarı, S aşağı tırmanır
        motor.WallClimbInput = motor.IsWallRunning
            ? (input.JetBagHeld ? 1f : 0f) - (input.DownHeld ? 1f : 0f)
            : 0f;
    }

    // Dünyanın yukarı yönünde (+Y), menzil içindeki ilk yüzey metal tavan mı?
    // Tavan modunda karakterin "yukarı"sı zemine bakar; tavan ise hep dünyanın yukarısındadır.
    // Karakterin merkezinden tek ışın: tavanın gerçekten altında olmak gerekir (kenarına yakın durmak yetmez).
    // Tavan modunu bitirme kontrolü de bunu kullanır: merkez tavanın ucunu geçince normale dönülür
    private bool MetalCeilingAbove()
    {
        return Physics.Raycast(motor.Position, Vector3.up, out RaycastHit hit, ceilingRange,
                   motor.GroundLayers, QueryTriggerInteraction.Ignore)
               && hit.collider.GetComponentInParent<MetalCeiling>() != null;
    }
}
