using UnityEngine;

// Fırlatma alanı (D.5): oyuncu üstüne gelince otomatik olarak yukarı fırlatır ve çift zıplamayı yeniler (açıksa).
// Fırlatınca plaka yay gibi "boing" yapar: yukarı uzayıp sönümlenerek sallanır (alt kenarı yerinde kalır).
// GEÇİCİ: fırlatılan oyuncunun arkasında uçak kondens izi gibi kalın beyaz bir çizgi bırakılır.
[RequireComponent(typeof(Collider))]
public class LaunchPad : MonoBehaviour
{
    [Tooltip("Oyuncunun fırlatılacağı yükseklik (birim).")]
    [SerializeField] private float launchHeight = 6f;

    [Header("Boing")]
    [Tooltip("Plakanın ilk uzama miktarı (1 = yüksekliğinin 2 katına çıkar). 0 = boing yok.")]
    [SerializeField] private float boingStrength = 1.5f;

    [Tooltip("Sallanma hızı (saniyedeki salınım).")]
    [SerializeField] private float boingFrequency = 6f;

    [Tooltip("Sallanmanın sönme hızı. Büyük = çabuk durur.")]
    [SerializeField] private float boingDamping = 6f;

    [Tooltip("Uzarken plakanın ne kadar inceldiği (0 = incelmez, 1 = uzamayla aynı oranda).")]
    [SerializeField] private float boingSquash = 0.3f;

    [Header("Kondens İzi")]
    [Tooltip("Fırlatmadan sonra izin çizilmeye devam ettiği süre (sn). 0 = iz yok.")]
    [SerializeField] private float contrailDuration = 0.7f;

    [Tooltip("Çizginin her noktasının ekranda kalma süresi (sn). Büyük = daha uzun kuyruk.")]
    [SerializeField] private float contrailLifetime = 0.6f;

    [Tooltip("Çizginin başlangıç kalınlığı (birim); kuyruğa doğru incelir.")]
    private float contrailWidth = 0.20f;

    [Tooltip("Çizginin rengi.")]
    [SerializeField] private Color contrailColor = Color.white;

    private Vector3 baseScale;
    private Vector3 basePosition;
    private float boingStartTime = float.NegativeInfinity;
    private bool boinging;

    // Kondens izi: oyuncuya takılan TrailRenderer. Bitiş zamanı tüm plakalarda ortak:
    // iz paylaşıldığı için eski bir plaka, yeni plakanın başlattığı izi erken kesmesin
    private TrailRenderer contrail;
    private PlayerDeath contrailDeath;
    private static float contrailEndTime;
    private const string ContrailName = "LaunchContrail";

    private void Awake()
    {
        baseScale = transform.localScale;
        basePosition = transform.localPosition;
    }

    private void Update()
    {
        UpdateContrails();
        if (!boinging) return;

        // Sönümlü yay: uzama * e^(-sönüm * t) * cos(2π * frekans * t); başta en uzun, sonra sallanarak durur
        float t = Time.time - boingStartTime;
        float envelope = Mathf.Exp(-boingDamping * t);
        float stretch = boingStrength * envelope * Mathf.Cos(2f * Mathf.PI * boingFrequency * t);
        if (envelope < 0.01f)
        {
            boinging = false;
            stretch = 0f;
        }

        float widthFactor = Mathf.Max(0.1f, 1f - stretch * boingSquash);
        transform.localScale = new Vector3(baseScale.x * widthFactor, baseScale.y * (1f + stretch), baseScale.z * widthFactor);
        // Alt kenar yerinde kalsın: uzamanın yarısı kadar yukarı kaydır
        transform.localPosition = basePosition + Vector3.up * (baseScale.y * stretch * 0.5f);
    }

    // Süre dolunca ya da oyuncu ölünce çizim durur; çizilmiş kuyruk kendi süresinde kaybolur (ölümde hemen silinir)
    private void UpdateContrails()
    {
        if (contrail == null || !contrail.emitting) return;

        bool dead = contrailDeath.IsDead;
        if (Time.time < contrailEndTime && !dead) return;

        contrail.emitting = false;
        if (dead) contrail.Clear();
    }

    private void StartContrails(Rigidbody body, PlayerDeath death)
    {
        if (contrailDuration <= 0f) return;

        contrail = GetOrCreateContrail(body.transform);
        contrailDeath = death;
        contrailEndTime = Time.time + contrailDuration;
        // Bu plakanın ayarları uygulanır (iz başka bir plakada oluşturulmuş olabilir)
        contrail.time = contrailLifetime;
        contrail.widthMultiplier = contrailWidth;
        contrail.startColor = contrailColor;
        contrail.endColor = contrailColor;
        contrail.Clear();
        contrail.emitting = true;
    }

    // İz oyuncunun çocuğu olarak bir kez oluşturulur ve tüm plakalarca paylaşılır
    private static TrailRenderer GetOrCreateContrail(Transform player)
    {
        Transform child = player.Find(ContrailName);
        if (child != null) return child.GetComponent<TrailRenderer>();

        child = new GameObject(ContrailName).transform;
        child.SetParent(player, false);
        // Karakterin hemen arkasında (kameradan uzakta)
        child.localPosition = new Vector3(0f, 0f, 0.1f);

        var trail = child.gameObject.AddComponent<TrailRenderer>();
        trail.emitting = false;
        trail.minVertexDistance = 0.1f;
        // Saydamlık ayarı gerektirmesin diye kuyruğa doğru incelerek kaybolur
        trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        return trail;
    }

    private void StartBoing()
    {
        if (boingStrength <= 0f) return;
        boingStartTime = Time.time;
        boinging = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null || !body.TryGetComponent(out PlayerMotor motor)) return;
        if (!body.TryGetComponent(out PlayerDeath death) || !body.TryGetComponent(out JumpAbility jump)) return;

        // Ölüyken veya başka bir yetenek hareketi yönetirken (atılma, zipline, sapan) fırlatma yok
        if (death.IsDead || motor.IsControlLocked) return;
        // Boing'in ilk anında uzayan tetik yükselen oyuncuya tekrar değip ikinci kez fırlatmasın
        if (Time.time < boingStartTime + 0.3f) return;

        motor.Jump(launchHeight);
        jump.RefreshAirJumpAfterLaunch();
        StartBoing();
        StartContrails(body, death);
    }
}
