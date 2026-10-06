using UnityEngine;

// Düşmanın atış döngüsü: Bekle → Nişan (uyarı çizgisi) → Ateş → Soğuma → Bekle.
// Oyuncuyu sahne başında kendisi bulur; sahneye bırakmak yeterli.
[RequireComponent(typeof(EnemyBody), typeof(LeadAim))]
public class EnemyShooter : MonoBehaviour
{
    // Lead: oyuncunun anlık hızıyla hedef önü (zıplama/atılma nişanı oynatır).
    // RunPath: oyuncu düz koşmaya devam ederse olacağı yere nişan (zıplamayı yok sayar; çizgi sakin kayar, tepki gerekir)
    public enum AimMode { Lead, RunPath }

    [Header("Nişan")]
    [Tooltip("Lead = anlık hıza göre hedef önü. RunPath = oyuncunun koşu yoluna nişan; oyuncu bir şey yapmazsa vurulur (tavan silahı).")]
    [SerializeField] private AimMode aimMode = AimMode.Lead;

    [Tooltip("RunPath: nişan noktasının yeni hedefe kayma yumuşaklığı (sn). Büyük = daha sakin çizgi.")]
    [SerializeField] private float aimSmoothTime = 0.08f;

    [Header("Menzil")]
    [Tooltip("Oyuncu bu mesafeye girince nişan almaya başlar (birim).")]
    [SerializeField] private float range = 18f;

    [Tooltip("Oyuncu düşmanı bu kadar geçtikten sonra ateş kesilir (birim). Arkadan vurulmayı engeller.")]
    [SerializeField] private float passTolerance = 0.3f;

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

    // Oyuncuyu gördü ve atış döngüsünde (nişan veya soğuma); devriye dronları bu sürede yerinde durur
    public bool IsEngaged => state != State.Idle;
    private Vector3 aimDirection = Vector3.left;

    // RunPath: yumuşatılmış nişan noktası ve oyuncunun en son yerde koştuğu yükseklik
    private Vector3 aimPoint;
    private Vector3 aimPointVelocity;
    private float lastGroundY;

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
        lastGroundY = player.Position.y;
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

        // Koşu yolu nişanı için: zıplarken değil, yerdeyken koşulan yükseklik
        if (player.IsGrounded) lastGroundY = player.Position.y;

        float elapsed = Time.time - stateStartTime;
        switch (state)
        {
            case State.Idle:
                if (PlayerInRange())
                {
                    Enter(State.WindUp);
                    // Çizgi eski bir noktadan kaymaya başlamasın: ilk karede hedefe otur
                    aimPoint = RunPathTarget(windUpTime);
                    aimPointVelocity = Vector3.zero;
                }
                break;

            case State.WindUp:
                // Nişan başladıysa atış her zaman yapılır (oyuncu menzilden çıksa bile): uyarı çizgisi hiç boşa çıkmaz
                if (elapsed < windUpTime - aimLockTime)
                {
                    // Nişan serbest: her karede yeniden hesaplanır
                    float timeUntilFire = windUpTime - elapsed;
                    if (aimMode == AimMode.RunPath)
                    {
                        aimPoint = Vector3.SmoothDamp(aimPoint, RunPathTarget(timeUntilFire), ref aimPointVelocity, aimSmoothTime);
                        aimDirection = (aimPoint - muzzle.position).normalized;
                        telegraph.Show(muzzle.position, aimDirection, false);
                    }
                    else
                    {
                        // Hedef, atış anındaki tahmini konum (kalan nişan süresi kadar ileri); yoksa kilit süresi kadar geride kalır
                        Vector3 targetAtFire = player.transform.position + player.Velocity * timeUntilFire;
                        aimDirection = aim.GetAimDirection(muzzle.position, targetAtFire, player.Velocity, bulletSpeed);
                        telegraph.Show(muzzle.position, aimDirection, false);
                    }
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

    // Oyuncu düz koşmaya devam ederse merminin onunla buluşacağı nokta (yükseklik = son yerde koşulan yükseklik)
    private Vector3 RunPathTarget(float timeUntilFire)
    {
        Vector3 start = player.Position;
        Vector3 target = new Vector3(start.x + player.CurrentSpeed * timeUntilFire, lastGroundY, 0f);
        // Mermi uçuş süresi hedefin uzaklığına bağlı; iki adımda yeterince yakınsar
        for (int i = 0; i < 2; i++)
        {
            float travelTime = Vector3.Distance(muzzle.position, target) / bulletSpeed;
            target.x = start.x + player.CurrentSpeed * (timeUntilFire + travelTime);
        }
        return target;
    }

    // Menzil/geçiş kontrolü olmadan hemen nişan alıp ateş eder (ör. kovalayan devriye dronunun son atışı)
    public void ForceShot()
    {
        if (player == null || body.IsDead || playerDeath.IsDead || state == State.WindUp) return;
        Enter(State.WindUp);
        aimPoint = RunPathTarget(windUpTime);
        aimPointVelocity = Vector3.zero;
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
