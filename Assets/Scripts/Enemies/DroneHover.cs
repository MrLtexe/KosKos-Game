using UnityEngine;

// Dron hareketi: her zaman hafifçe aşağı yukarı süzülür (görsel canlılık).
// İsteğe bağlı devriye: başlangıç noktası ile (başlangıç + devriye mesafesi) arasında gidip gelir; 0 = yerinde süzülür.
// Devriye dronu oyuncuyu görüp ateş ederken durur (yerinde ateş eder). Oyuncu altından geçince bir kez peşinden uçar,
// bir atış daha yapar, sonra devriye yoluna döner ve kaldığı yerden devam eder.
public class DroneHover : MonoBehaviour
{
    [Header("Süzülme")]
    [Tooltip("Aşağı yukarı süzülme mesafesi (birim). Dron en fazla bu kadar alçalır.")]
    [SerializeField] private float amplitude = 0.25f;

    [Tooltip("Saniyedeki süzülme sayısı.")]
    [SerializeField] private float frequency = 0.6f;

    [Header("Devriye")]
    [Tooltip("Başlangıç noktasına göre devriyenin diğer ucu (birim). (0,0,0) = devriye yok, yerinde süzülür (kovalama da yok).")]
    [SerializeField] private Vector3 patrolOffset = Vector3.zero;

    [Tooltip("Devriye hızı (birim/sn).")]
    [SerializeField] private float patrolSpeed = 6f;

    [Header("Kovalama (sadece devriye dronu)")]
    [Tooltip("Oyuncu dronu bu kadar geçince kovalama başlar (birim).")]
    [SerializeField] private float chaseTriggerDistance = 1f;

    [Tooltip("Kovalama hızı (birim/sn). Oyuncunun koşu hızından (8) büyük olmalı.")]
    [SerializeField] private float chaseSpeed = 14f;

    [Tooltip("Dronun oyuncunun ne kadar gerisinde durup ateş edeceği (birim).")]
    [SerializeField] private float chaseBehindDistance = 3f;

    [Tooltip("En uzun kovalama süresi (sn); yetişemezse yine de durup ateş eder.")]
    [SerializeField] private float maxChaseTime = 1.5f;

    [Tooltip("Ateşten sonra devriye yoluna dönüş hızı (birim/sn).")]
    [SerializeField] private float returnSpeed = 6f;

    private enum Mode { Patrol, Chase, Shoot, Return }

    // Çok kısa devriye mesafesi devriye sayılmaz
    private const float MinPatrolDistance = 0.01f;
    // Hedefe bu kadar yaklaşınca varılmış sayılır (birim)
    private const float ArriveDistance = 0.3f;

    private Vector3 basePosition;
    // Süzülme hariç asıl konum (kovalama ve dönüş bunu taşır)
    private Vector3 anchor;
    // Her dron farklı anda başlasın (hepsi aynı anda inip kalkmasın)
    private float phase;
    // Devriyenin kendi saati: dron ateş ederken ve kovalarken durur, devam edince sıçramasın
    private float patrolClock;
    private Mode mode = Mode.Patrol;
    private float chaseStartTime;
    // Oyuncu her geçişte sadece bir kez kovalanır; yeniden doğunca sıfırlanır
    private bool chasedThisPass;
    // Sadece oyuncuyu görüp ateş etmiş dron kovalar (dronun ilerisinde yeniden doğan oyuncu kovalanmaz)
    private bool sawPlayer;

    private EnemyShooter shooter;
    private EnemyBody body;
    private PlayerMotor player;

    private bool Patrols => patrolOffset.magnitude > MinPatrolDistance && patrolSpeed > 0f;

    private void Awake()
    {
        basePosition = transform.position;
        anchor = basePosition;
        phase = Random.value * Mathf.PI * 2f;
        shooter = GetComponent<EnemyShooter>();
        body = GetComponent<EnemyBody>();
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMotor>();
    }

    private void OnEnable()
    {
        PlayerDeath.AnyPlayerRespawned += ResetToRoute;
    }

    private void OnDisable()
    {
        PlayerDeath.AnyPlayerRespawned -= ResetToRoute;
    }

    private void Update()
    {
        // Öldürülen dron hareket etmez
        if (body == null || !body.IsDead)
        {
            if (Patrols) UpdatePatrolAndChase(Time.deltaTime);
        }

        float bob = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f + phase) * amplitude;
        transform.position = anchor + Vector3.up * bob;
    }

    private void UpdatePatrolAndChase(float dt)
    {
        switch (mode)
        {
            case Mode.Patrol:
                // Oyuncuyu görünce yerinde ateş eder
                if (shooter != null && shooter.IsEngaged) sawPlayer = true;
                else patrolClock += dt;
                anchor = RoutePoint();

                if (CanStartChase())
                {
                    mode = Mode.Chase;
                    chaseStartTime = Time.time;
                    chasedThisPass = true;
                }
                break;

            case Mode.Chase:
            {
                // Oyuncunun biraz gerisine, kendi yüksekliğinde
                var target = new Vector3(player.Position.x - chaseBehindDistance, anchor.y, 0f);
                anchor = Vector3.MoveTowards(anchor, target, chaseSpeed * dt);
                bool arrived = Mathf.Abs(anchor.x - target.x) <= ArriveDistance;
                if (arrived || Time.time - chaseStartTime >= maxChaseTime)
                {
                    shooter.ForceShot();
                    mode = Mode.Shoot;
                }
                break;
            }

            case Mode.Shoot:
                // Atış ve soğuma bitene kadar yerinde
                if (!shooter.IsEngaged) mode = Mode.Return;
                break;

            case Mode.Return:
            {
                Vector3 routePoint = RoutePoint();
                anchor = Vector3.MoveTowards(anchor, routePoint, returnSpeed * dt);
                if (Vector3.Distance(anchor, routePoint) <= ArriveDistance) mode = Mode.Patrol;
                break;
            }
        }
    }

    private bool CanStartChase()
    {
        if (chasedThisPass || !sawPlayer || shooter == null || player == null) return false;
        return player.Position.x > anchor.x + chaseTriggerDistance;
    }

    // Devriye saatine göre yol üzerindeki nokta (uçlarda yavaşlayarak döner)
    private Vector3 RoutePoint()
    {
        float t = Mathf.PingPong(patrolClock * patrolSpeed / patrolOffset.magnitude, 1f);
        return basePosition + patrolOffset * Mathf.SmoothStep(0f, 1f, t);
    }

    // Oyuncu yeniden doğunca dron devriye yoluna döner ve tekrar kovalayabilir
    private void ResetToRoute()
    {
        mode = Mode.Patrol;
        chasedThisPass = false;
        sawPlayer = false;
        anchor = Patrols ? RoutePoint() : basePosition;
    }

    // Scene görünümünde devriye yolunu gösterir
    private void OnDrawGizmosSelected()
    {
        if (patrolOffset.sqrMagnitude < MinPatrolDistance * MinPatrolDistance) return;
        Vector3 start = Application.isPlaying ? basePosition : transform.position;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(start, start + patrolOffset);
    }
}
