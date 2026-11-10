using UnityEngine;

// SADECE TEST: oyuncunun başının üstünde, mermiden atılmayla kaçma zamanını gösteren küre.
// Gizli: üstüne gelen mermi yok. Sarı: mermi geliyor, ama şimdi atılmak kurtarmaz (erken, geç ya da atılma beklemede).
// Yeşil: şimdi atılırsan mermi arkanda kalır.
// Sıyrılganlıktan bağımsızdır: sadece atılmanın hızıyla kaçışı hesaplar (oyuncu ve mermilerin yolu birkaç adım ileri simüle edilir).
// Test dünyası builder'ı sahneye koyar; oyuncuyu kendisi bulur.
public class DodgeTimingIndicator : MonoBehaviour
{
    [Tooltip("Göstergenin oyuncu merkezinin ne kadar üstünde durduğu (birim).")]
    [SerializeField] private float heightAbovePlayer = 1.7f;

    [Tooltip("Gösterge küresinin çapı (birim).")]
    [SerializeField] private float markerSize = 0.4f;

    [Tooltip("Ne kadar ilerisi simüle edilir (sn). Bu süre içinde çarpmayacak mermiler yok sayılır.")]
    [SerializeField] private float lookAheadTime = 1.2f;

    [Tooltip("Simülasyon adımı (sn). Küçük = daha doğru, daha pahalı.")]
    [SerializeField] private float simulationStep = 0.01f;

    [Tooltip("Mermi geliyor ama şimdi atılmak kurtarmaz rengi.")]
    [SerializeField] private Color waitColor = new Color(1f, 0.8f, 0.1f);

    [Tooltip("Şimdi atılırsan mermi arkanda kalır rengi.")]
    [SerializeField] private Color dashNowColor = new Color(0.2f, 1f, 0.3f);

    // Oyuncu kapsülünün yarı genişliği/yarı yüksekliği ve mermi yarıçapı (çarpışma kutusu yaklaşımı)
    private const float PlayerHalfWidth = 0.5f;
    private const float PlayerHalfHeight = 1f;
    private const float BulletRadius = 0.2f;

    private PlayerMotor player;
    private PlayerDeath playerDeath;
    private DashAbility dash;
    private Transform marker;
    private Material material;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMotor>();
        if (player == null)
        {
            enabled = false;
            return;
        }
        playerDeath = player.GetComponent<PlayerDeath>();
        dash = player.GetComponent<DashAbility>();

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "DodgeTimingMarker";
        Destroy(sphere.GetComponent<Collider>());
        sphere.transform.SetParent(transform, false);
        sphere.transform.localScale = Vector3.one * markerSize;
        // Işıktan etkilenmeyen düz renk
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        sphere.GetComponent<Renderer>().sharedMaterial = material;
        marker = sphere.transform;
        marker.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    private void LateUpdate()
    {
        if (player == null) return;

        // Ölüyken ya da zaten atılırken gösterilecek bir şey yok
        if (playerDeath.IsDead || dash.IsDashing)
        {
            marker.gameObject.SetActive(false);
            return;
        }

        bool threatened = false;
        bool dashSaves = false;
        foreach (Bullet bullet in Bullet.Active)
        {
            if (bullet.IsReflected || !WouldHit(bullet, false)) continue;
            threatened = true;
            // Üstüne gelen birden fazla mermi varsa hepsinden kurtarmalı; atılma bekleme süresindeyse kurtaramaz
            dashSaves = dash.CooldownReady && !WouldHit(bullet, true);
            if (!dashSaves) break;
        }

        marker.gameObject.SetActive(threatened);
        if (!threatened) return;
        marker.position = player.Position + Vector3.up * heightAbovePlayer;
        material.color = dashSaves ? dashNowColor : waitColor;
    }

    // Oyuncu şimdi atılırsa (ya da düz koşmaya devam ederse) mermi ona çarpar mı. Oyuncunun yüksekliği sabit kabul edilir
    private bool WouldHit(Bullet bullet, bool dashNow)
    {
        Vector3 playerStart = player.Position;
        float runSpeed = player.CurrentSpeed;
        Vector3 bulletStart = bullet.Position;
        Vector3 bulletVelocity = bullet.Velocity;

        for (float t = 0f; t <= lookAheadTime; t += simulationStep)
        {
            float playerX = playerStart.x + PlayerDistance(t, runSpeed, dashNow);
            Vector3 b = bulletStart + bulletVelocity * t;
            if (Mathf.Abs(b.x - playerX) < PlayerHalfWidth + BulletRadius &&
                Mathf.Abs(b.y - playerStart.y) < PlayerHalfHeight + BulletRadius)
            {
                return true;
            }
        }
        return false;
    }

    // t saniyede alınan yatay yol: atılırsa önce atılma hızı, sonra koşu hızı
    private float PlayerDistance(float t, float runSpeed, bool dashNow)
    {
        if (!dashNow) return runSpeed * t;
        float dashTime = Mathf.Min(t, dash.DashDuration);
        return dash.DashSpeed * dashTime + runSpeed * (t - dashTime);
    }
}
