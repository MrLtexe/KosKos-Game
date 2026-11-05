using UnityEngine;

// GEÇİCİ: kırılabilir nesne parçalanma efekti. Nesne kırılınca kendi renginde, dönerek saçılan küçük 3B küp parçalar;
// zemine çarpıp seker, sonra küçülerek kaybolur.
// Bileşen değil: ilk kullanımda sahnede tek bir paylaşılan parçacık sistemi kurulur (prefab'a eklemek gerekmez).
// Sahne değişince sistem silinir, bir sonraki kullanımda yeniden kurulur. Gerçek efekt gelince bu dosya silinecek.
public static class BreakDebrisPlaceholder
{
    // Nesnenin kameradan görünen yüzey alanına (genişlik x yükseklik) göre parça sayısı, alt/üst sınırlarla
    private const float ChunksPerUnitArea = 10f;
    private const int MinChunks = 10;
    private const int MaxChunks = 45;
    // Parçalar oyuncunun geldiği yöne (ileri, +X) ve yukarı savrulur
    private static readonly Vector3 PushDirection = new Vector3(1f, 0.8f, 0f);

    private static ParticleSystem debris;
    // Malzeme nesneyle birlikte silinmez; bir kez oluşturulup sahneler arasında tekrar kullanılır
    private static Material material;

    // bounds: kırılan nesnenin kapladığı alan; color: nesnenin rengi
    public static void Play(Bounds bounds, Color color)
    {
        if (debris == null) Create();

        Vector3 size = bounds.size;
        // Bloklar derinlikte (Z) hep aynı kalın; sayıyı görünen alan belirler
        int count = Mathf.Clamp(Mathf.RoundToInt(size.x * size.y * ChunksPerUnitArea), MinChunks, MaxChunks);

        for (int i = 0; i < count; i++)
        {
            // Nesnenin içinde rastgele bir nokta; derinlik (Z) dar tutulur, parçalar oyun düzleminin yakınında kalsın
            var local = new Vector3(Random.Range(-0.5f, 0.5f) * size.x, Random.Range(-0.5f, 0.5f) * size.y,
                Random.Range(-0.3f, 0.3f));
            Vector3 position = bounds.center + local;

            // Merkezden dışarı + ileri-yukarı itme
            Vector3 outward = local.sqrMagnitude > 0.0001f ? local.normalized : Random.onUnitSphere;
            Vector3 velocity = outward * Random.Range(2f, 5f) + PushDirection * Random.Range(2f, 5f);

            var emitParams = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocity,
                startSize = Random.Range(0.18f, 0.38f),
                startLifetime = Random.Range(1.2f, 1.8f),
                rotation3D = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f)),
                angularVelocity3D = new Vector3(Random.Range(-540f, 540f), Random.Range(-540f, 540f), Random.Range(-540f, 540f)),
                // Hafif ton farkı: parçalar tek renk lekesi gibi görünmesin
                startColor = Color.Lerp(color, color * 0.65f, Random.value)
            };
            debris.Emit(emitParams, 1);
        }
    }

    private static void Create()
    {
        var go = new GameObject("BreakDebris");
        debris = go.AddComponent<ParticleSystem>();
        debris.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = debris.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        // Oyunun yerçekimi güçlü; parçalar havada süzülmesin
        main.gravityModifier = 2.5f;
        main.startRotation3D = true;
        main.maxParticles = 300;

        // Konum, hız, dönüş, renk ve ömür her parçaya elle verilir (EmitParams)
        ParticleSystem.EmissionModule emission = debris.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = debris.shape;
        shape.enabled = false;

        // Ömrünün çoğunda tam boy, sonunda küçülerek kaybolur (saydamlık ayarı gerekmez)
        ParticleSystem.SizeOverLifetimeModule size = debris.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0f)));

        // Zemine çarpıp seksin (sadece zemin layer'ı; oyuncuya takılmasın)
        ParticleSystem.CollisionModule collision = debris.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.collidesWith = LayerMask.GetMask("Ground");
        collision.bounce = 0.3f;
        collision.dampen = 0.4f;
        collision.lifetimeLoss = 0f;
        collision.radiusScale = 0.5f;

        // 3B küp parçalar, ışıktan etkilenen malzeme (gölgeli yüzler)
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        renderer.alignment = ParticleSystemRenderSpace.World;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
        if (material == null) material = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
        renderer.sharedMaterial = material;
    }
}
