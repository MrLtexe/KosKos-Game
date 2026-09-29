using UnityEngine;

// Düşmanın atış döngüsü: Bekle → Nişan (uyarı çizgisi) → Ateş → Soğuma → Bekle.
// Oyuncuyu sahne başında kendisi bulur; sahneye bırakmak yeterli.
[RequireComponent(typeof(EnemyBody), typeof(LeadAim))]
public class EnemyShooter : MonoBehaviour
{
    [Header("Menzil")]
    [Tooltip("Oyuncu bu mesafeye girince nişan almaya başlar (birim).")]
    [SerializeField] private float range = 18f;

    [Tooltip("Oyuncu düşmanı bu kadar geçtikten sonra ateş kesilir (birim). Arkadan vurulmayı engeller.")]
    [SerializeField] private float passTolerance = 0.5f;

    [Header("Zamanlama")]
    [Tooltip("Nişan alma (uyarı çizgisi) süresi (sn).")]
    [SerializeField] private float windUpTime = 0.6f;

    [Tooltip("Ateşten bu kadar önce nişan kilitlenir ve çizgi renk değiştirir (sn).")]
    [SerializeField] private float aimLockTime = 0.15f;

    [Tooltip("Ateşten sonra tekrar nişan almadan önceki bekleme (sn).")]
    [SerializeField] private float cooldownTime = 1.5f;

    [Header("Atış")]
    [Tooltip("Bir atışta çıkan mermi sayısı. 1 = tek mermi, 3 = yelpaze.")]
    [SerializeField] private int bulletsPerShot = 1;

    [Tooltip("Yelpazenin nişan çizgisine göre açısı (derece). Mermiler -açı ile +açı arasına eşit dağılır.")]
    [SerializeField] private float spreadAngle = 0f;

    [Tooltip("Merminin hızı (birim/sn).")]
    [SerializeField] private float bulletSpeed = 14f;

    [Tooltip("Atılacak mermi prefab'ı (sarı = savuşturulabilir, mor = savuşturulamaz).")]
    [SerializeField] private Bullet bulletPrefab;

    [Tooltip("Havuzda önceden hazırlanan mermi sayısı.")]
    [SerializeField] private int prewarmCount = 4;

    [Header("Bağlantılar")]
    [Tooltip("Mermilerin çıktığı nokta.")]
    [SerializeField] private Transform muzzle;

    [Tooltip("Nişan uyarı çizgisi.")]
    [SerializeField] private AimTelegraph telegraph;

    private enum State { Idle, WindUp, Cooldown }

    private State state = State.Idle;
    private float stateStartTime;
    private Vector3 aimDirection = Vector3.left;

    private EnemyBody body;
    private LeadAim aim;
    private BulletPool pool;
    private PlayerMotor player;
    private PlayerDeath playerDeath;

    private void Awake()
    {
        body = GetComponent<EnemyBody>();
        aim = GetComponent<LeadAim>();
        // Mermiler düşmanın altında değil, sahne kökünde durur (düşmanın görselleriyle karışmasın)
        var container = new GameObject($"{name}_Bullets").transform;
        pool = new BulletPool(bulletPrefab, container, prewarmCount);
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMotor>();
        if (player == null)
        {
            Debug.LogError($"[KosKos] {name}: sahnede oyuncu (PlayerMotor) bulunamadı, düşman çalışmayacak.");
            enabled = false;
            return;
        }
        playerDeath = player.GetComponent<PlayerDeath>();
        playerDeath.Died += OnPlayerDied;
    }

    private void OnDestroy()
    {
        if (playerDeath != null) playerDeath.Died -= OnPlayerDied;
    }

    private void OnEnable()
    {
        body.Killed += ResetCycle;
    }

    private void OnDisable()
    {
        body.Killed -= ResetCycle;
    }

    private void Update()
    {
        if (body.IsDead || playerDeath.IsDead) return;

        float elapsed = Time.time - stateStartTime;
        switch (state)
        {
            case State.Idle:
                if (PlayerInRange()) Enter(State.WindUp);
                break;

            case State.WindUp:
                if (!PlayerInRange())
                {
                    ResetCycle();
                }
                else if (elapsed < windUpTime - aimLockTime)
                {
                    // Nişan serbest: her karede hedef önü yeniden hesaplanır.
                    // Hedef, atış anındaki tahmini konum (kalan nişan süresi kadar ileri); yoksa kilit süresi kadar geride kalır
                    float timeUntilFire = windUpTime - elapsed;
                    Vector3 targetAtFire = player.transform.position + player.Velocity * timeUntilFire;
                    aimDirection = aim.GetAimDirection(muzzle.position, targetAtFire, player.Velocity, bulletSpeed);
                    telegraph.Show(muzzle.position, aimDirection, false);
                }
                else if (elapsed < windUpTime)
                {
                    // Nişan kilitli: oyuncunun tepki verme anı
                    telegraph.Show(muzzle.position, aimDirection, true);
                }
                else
                {
                    Fire();
                    telegraph.Hide();
                    Enter(State.Cooldown);
                }
                break;

            case State.Cooldown:
                if (elapsed >= cooldownTime) Enter(State.Idle);
                break;
        }
    }

    private bool PlayerInRange()
    {
        Vector3 p = player.transform.position;
        Vector3 me = transform.position;
        // Oyuncu düşmanı geçtiyse ateş yok
        if (p.x > me.x + passTolerance) return false;
        return (p - me).sqrMagnitude <= range * range;
    }

    private void Fire()
    {
        for (int i = 0; i < bulletsPerShot; i++)
        {
            float angle = bulletsPerShot > 1
                ? Mathf.Lerp(-spreadAngle, spreadAngle, i / (float)(bulletsPerShot - 1))
                : 0f;
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * aimDirection;
            pool.Get().Launch(muzzle.position, direction, bulletSpeed, body, pool);
        }
    }

    private void Enter(State next)
    {
        state = next;
        stateStartTime = Time.time;
    }

    private void ResetCycle()
    {
        Enter(State.Idle);
        telegraph.Hide();
    }

    private void OnPlayerDied()
    {
        // Oyuncu yeniden doğduğunda ekranda mermi kalmasın ve nişan baştan başlasın
        ResetCycle();
        pool.ReturnAll();
    }
}
