using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// DENEME: 2B koşu animasyonunu oyuncuya bağlar: KosKos > Animation > Setup Player Run Animation.
// 1) Assets/Player/RunningAnimationTest içindeki kareleri sprite olarak içe aktarır (alt-orta pivot, ~2 birim boy).
// 2) Kareleri sırayla (1, 2, ... 10) döngülü Run.anim'e koyar, PlayerRun.controller'a bağlar.
// 3) Player.prefab'a "Visual" çocuğu (SpriteRenderer + Animator) ekler/günceller ve kapsülün MeshRenderer'ını gizler
//    (çarpıştırıcı ve fizik aynen kalır). Kareler değişince tekrar çalıştırmak yeterli; her şeyi günceller.
public static class PlayerRunAnimationSetup
{
    private const string FrameFolder = "Assets/Player/RunningAnimationTest";
    private const string ClipPath = FrameFolder + "/Run.anim";
    private const string ControllerPath = FrameFolder + "/PlayerRun.controller";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string SpriteMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
    private const string VisualName = "Visual";

    // Kare tuvali 2730 piksel yüksek, karakter ~2200 piksel: 1000 piksel = 1 birim → karakter ~2.2 birim (kapsül 2)
    private const float PixelsPerUnit = 1000f;
    private const int MaxTextureSize = 2048;
    private const float FramesPerSecond = 12f;
    // Kapsülün tabanı (merkezden 1 birim aşağı): sprite'ın alt-orta pivotu buraya oturur
    private static readonly Vector3 VisualOffset = new Vector3(0f, -1f, 0f);

    [MenuItem("KosKos/Animation/Setup Player Run Animation")]
    private static void Setup()
    {
        Sprite[] frames = ImportFrames();
        if (frames.Length == 0)
        {
            EditorUtility.DisplayDialog("Koşu animasyonu", $"{FrameFolder} içinde kare (PNG) bulunamadı.", "Tamam");
            return;
        }

        AnimationClip clip = CreateOrUpdateClip(frames);
        AnimatorController controller = GetOrCreateController(clip);
        SetupPlayerPrefab(frames[0], controller);
        AssetDatabase.SaveAssets();
        Debug.Log($"[KosKos] Koşu animasyonu oyuncuya bağlandı: {frames.Length} kare, {FramesPerSecond} fps.");
    }

    // Kareleri sprite olarak ayarlar ve sayı sırasına göre döndürür (1, 2, ..., 10; "10" "2"den sonra gelsin diye sayıyla sıralanır)
    private static Sprite[] ImportFrames()
    {
        string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { FrameFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(FrameNumber)
            .ToArray();

        foreach (string path in paths)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            // 4096'lık kareler küçültülüp sıkıştırılır (her kare sıkıştırmasız ~64 MB olurdu); dünyadaki boyut değişmez
            importer.maxTextureSize = MaxTextureSize;
            importer.textureCompression = TextureImporterCompression.Compressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            // Tüm karelerin tuvali aynı: alt-orta pivot = karakterin yere bastığı çizgi
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        return paths.Select(AssetDatabase.LoadAssetAtPath<Sprite>).Where(s => s != null).ToArray();
    }

    private static int FrameNumber(string path)
    {
        string name = System.IO.Path.GetFileNameWithoutExtension(path);
        return int.TryParse(name, out int number) ? number : int.MaxValue;
    }

    private static AnimationClip CreateOrUpdateClip(Sprite[] frames)
    {
        var clip = new AnimationClip { frameRate = FramesPerSecond };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        // Unity son kareyi kendiliğinden bir kare süresi gösterir; sona ilk kareyi tekrar eklemek döngüde takılma (çift kare) yapar
        var keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i / FramesPerSecond, value = frames[i] };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

        // Varsa üzerine kopyalanır: GUID ve controller bağlantısı korunur
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(clip, ClipPath);
            return clip;
        }
        EditorUtility.CopySerialized(clip, existing);
        Object.DestroyImmediate(clip);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    private static AnimatorController GetOrCreateController(AnimationClip clip)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(ControllerPath, clip);
        return controller;
    }

    private static void SetupPlayerPrefab(Sprite firstFrame, AnimatorController controller)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            Transform visual = root.transform.Find(VisualName);
            if (visual == null)
            {
                visual = new GameObject(VisualName).transform;
                visual.SetParent(root.transform, false);
            }
            visual.localPosition = VisualOffset;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;
            visual.gameObject.layer = root.layer;

            var spriteRenderer = visual.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = firstFrame;
            // Işıktan etkilenmeyen sprite malzemesi (sahne ışığı karakteri karartmasın); bulunamazsa varsayılan kalır
            var material = AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath);
            if (material != null) spriteRenderer.sharedMaterial = material;

            var animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            // Kapsül görünmez olur; çarpıştırıcısı ve tüm fizik aynen kalır
            var capsuleRenderer = root.GetComponent<MeshRenderer>();
            if (capsuleRenderer != null) capsuleRenderer.enabled = false;

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
