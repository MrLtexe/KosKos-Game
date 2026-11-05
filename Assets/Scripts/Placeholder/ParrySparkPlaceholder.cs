using UnityEngine;

// GEÇİCİ: savuşturma kıvılcımı. Kılıç bir mermiyi geri gönderdiğinde, mermi neredeyse orada küçük bir kıvılcım patlar.
// Bileşen değil: ilk kullanımda sahnede tek bir paylaşılan parçacık sistemi kurulur (prefab'a eklemek gerekmez).
// Sahne değişince sistem silinir, bir sonraki kullanımda yeniden kurulur. Gerçek efekt gelince bu dosya silinecek.
public static class ParrySparkPlaceholder
{
    private const int SparkCount = 12;
    private static readonly Color ColorA = new Color(1f, 0.95f, 0.6f);
    private static readonly Color ColorB = Color.white;
    // Kıvılcımın verilen noktaya göre kaydırılması (X pozitif = sağa)
    private static readonly Vector3 Offset = new Vector3(0.5f, 0f, 0f);

    private static ParticleSystem sparks;
    // Malzeme nesneyle birlikte silinmez; bir kez oluşturulup sahneler arasında tekrar kullanılır
    private static Material material;

    public static void Play(Vector3 position)
    {
        if (sparks == null) Create();

        for (int i = 0; i < SparkCount; i++)
        {
            // Ekran düzleminde her yöne (derinlik az)
            Vector3 direction = Random.onUnitSphere;
            direction.z *= 0.2f;
            direction.Normalize();

            var emitParams = new ParticleSystem.EmitParams
            {
                position = position + Offset,
                velocity = direction * Random.Range(8f, 15f),
                startSize = Random.Range(0.05f, 0.09f),
                startLifetime = Random.Range(0.15f, 0.22f),
                startColor = Color.Lerp(ColorA, ColorB, Random.value)
            };
            sparks.Emit(emitParams, 1);
        }
    }

    private static void Create()
    {
        var go = new GameObject("ParrySparks");
        sparks = go.AddComponent<ParticleSystem>();
        sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Konum, hız, renk ve ömür her kıvılcıma elle verilir (EmitParams)
        ParticleSystem.MainModule main = sparks.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;

        ParticleSystem.EmissionModule emission = sparks.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = sparks.shape;
        shape.enabled = false;

        // Saydamlık ayarı gerektirmesin diye sönme yerine küçülerek kaybolur
        ParticleSystem.SizeOverLifetimeModule size = sparks.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Hız yönünde uzatılmış ince çizgiler: kıvılcım gibi görünür
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.04f;
        renderer.lengthScale = 1f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (material == null) material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        renderer.sharedMaterial = material;
    }
}
