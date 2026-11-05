using UnityEngine;

// GEÇİCİ: zıplama parçacıkları. Yer zıplamasında ayaktan sağa-sola savrulan toz bulutu,
// hava zıplamasında ayağın altında yatay açılan halka. Parçacık sistemi oyunda koddan kurulur.
// Gerçek efekt gelince bu bileşen silinecek.
[RequireComponent(typeof(JumpAbility))]
public class JumpDustPlaceholder : MonoBehaviour
{
    [Header("Yer Zıplaması (Toz)")]
    [Tooltip("Yer zıplamasında çıkan toz bulutu sayısı.")]
    [SerializeField] private int dustCount = 10;

    [Tooltip("Toz renkleri (iki renk arasından rastgele).")]
    [SerializeField] private Color dustColorA = new Color(0.78f, 0.75f, 0.7f);

    [Tooltip("Toz renkleri (iki renk arasından rastgele).")]
    [SerializeField] private Color dustColorB = new Color(0.55f, 0.52f, 0.5f);

    [Tooltip("Tozun yana savrulma hızı (en az, en çok; birim/sn).")]
    [SerializeField] private Vector2 dustSpeed = new Vector2(2f, 4f);

    [Tooltip("Toz bulutunun en büyük boyutu (birim).")]
    [SerializeField] private float dustSize = 0.3f;

    [Header("Hava Zıplaması (Halka)")]
    [Tooltip("Hava zıplamasında halkayı oluşturan parçacık sayısı.")]
    [SerializeField] private int ringCount = 16;

    [Tooltip("Halkanın rengi.")]
    [SerializeField] private Color ringColor = new Color(0.85f, 0.95f, 1f);

    [Tooltip("Halkanın açılma hızı (birim/sn).")]
    [SerializeField] private float ringSpeed = 4f;

    // Oyuncu merkezinden ayak hizasına (kapsül yüksekliği 2)
    private static readonly Vector3 FeetOffset = new Vector3(0f, -0.9f, 0f);

    private JumpAbility jump;
    private ParticleSystem particles;
    private Material material;

    private void Awake()
    {
        jump = GetComponent<JumpAbility>();
        CreateParticles();
    }

    private void OnEnable()
    {
        jump.Jumped += OnJumped;
    }

    private void OnDisable()
    {
        jump.Jumped -= OnJumped;
    }

    private void OnDestroy()
    {
        if (particles != null) Destroy(particles.gameObject);
        if (material != null) Destroy(material);
    }

    // Oyuncunun çocuğu değil: toz çıktığı yerde kalsın, ölümde gizlenen görsellere dahil olmasın
    private void CreateParticles()
    {
        var go = new GameObject("JumpDust");
        particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Konum, hız, boyut, renk ve ömür her parçacığa elle verilir (EmitParams)
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.1f;
        main.maxParticles = 200;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = false;

        // Saydamlık ayarı gerektirmesin diye: küçük çıkar, büyür, sonunda küçülerek kaybolur
        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.5f), new Keyframe(0.4f, 1f), new Keyframe(1f, 0f)));

        // Hızla çıkıp havada yavaşlar
        ParticleSystem.LimitVelocityOverLifetimeModule drag = particles.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.limit = 1000f;
        drag.drag = 4f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        renderer.sharedMaterial = material;
    }

    private void OnJumped(bool airJump)
    {
        // Karakterin dönüşüne göre ayak (tavanda ters çevrilince de ayak tarafı)
        Vector3 feet = transform.TransformPoint(FeetOffset);
        if (airJump) EmitRing(feet);
        else EmitDust(feet);
    }

    // Ayaktan sağa ve sola, yere yakın savrulan toz
    private void EmitDust(Vector3 feet)
    {
        for (int i = 0; i < dustCount; i++)
        {
            float side = i % 2 == 0 ? 1f : -1f;
            Vector3 direction = transform.TransformDirection(
                new Vector3(side * Random.Range(0.6f, 1f), Random.Range(0.05f, 0.4f), 0f)).normalized;

            var emitParams = new ParticleSystem.EmitParams
            {
                position = feet + direction * 0.2f,
                velocity = direction * Random.Range(dustSpeed.x, dustSpeed.y),
                startSize = dustSize * Random.Range(0.5f, 1f),
                startLifetime = Random.Range(0.3f, 0.5f),
                startColor = Color.Lerp(dustColorA, dustColorB, Random.value)
            };
            particles.Emit(emitParams, 1);
        }
    }

    // Ayağın altında yatay (zemine paralel) açılan halka; kamera hafif yukarıdan baktığı için ince bir elips görünür
    private void EmitRing(Vector3 feet)
    {
        for (int i = 0; i < ringCount; i++)
        {
            float angle = i * Mathf.PI * 2f / ringCount;
            Vector3 direction = transform.TransformDirection(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));

            var emitParams = new ParticleSystem.EmitParams
            {
                position = feet + direction * 0.15f,
                velocity = direction * ringSpeed,
                startSize = 0.12f,
                startLifetime = 0.25f,
                startColor = ringColor
            };
            particles.Emit(emitParams, 1);
        }
    }
}
