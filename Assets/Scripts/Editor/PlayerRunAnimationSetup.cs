using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// DENEME: 2B koşu ve zıplama animasyonlarını oyuncuya bağlar: KosKos > Animation > Setup Player Run Animation.
// 1) Assets/Player/RunningAnimationTest ve Assets/Player/jump try içindeki kareleri sprite olarak içe aktarır (alt-orta pivot, ~2 birim boy).
// 2) Koşu karelerini sırayla (1, 2, ... 10) döngülü Run.anim'e koyar, PlayerRun.controller'a bağlar.
// 3) Player.prefab'a "Visual" çocuğu (SpriteRenderer + Animator + PlayerSpriteAnimator) ekler/günceller ve kapsülün MeshRenderer'ını gizler
//    (çarpıştırıcı ve fizik aynen kalır). Zıplama kareleri PlayerSpriteAnimator'a atanır: kalkış = koşu 3, yükseliş = 1-2, tepe = 3,
//    geçiş = 4, düşüş = 5. Atılma kareleri (Assets/Player/dash try): geçiş = 1-3, tutulan kare = 4.
//    Kareler kullanıcının verdiği gibidir; hiçbir kare eklenmez / çoğaltılmaz.
//    Kareler değişince tekrar çalıştırmak yeterli; her şeyi günceller.
public static class PlayerRunAnimationSetup
{
    private const string FrameFolder = "Assets/Player/RunningAnimationTest";
    private const string JumpFolder = "Assets/Player/jump try";
    private const int JumpFrameCount = 5;
    private const string DashFolder = "Assets/Player/dash try";
    private const int DashFrameCount = 4;
    // Zıplamaya geçiş karesi: koşunun 3. karesi
    private const int TakeoffRunFrame = 3;
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
        Sprite[] frames = ImportFrames(FrameFolder);
        if (frames.Length == 0)
        {
            EditorUtility.DisplayDialog("Koşu animasyonu", $"{FrameFolder} içinde kare (PNG) bulunamadı.", "Tamam");
            return;
        }

        // Zıplama: klasör yoksa ya da kare sayısı beklenenden farklıysa atlanır (karakter havada da koşu döngüsünü oynatır)
        Sprite[] jumpFrames = AssetDatabase.IsValidFolder(JumpFolder) ? ImportFrames(JumpFolder) : new Sprite[0];
        if (jumpFrames.Length != JumpFrameCount)
        {
            Debug.LogWarning($"[KosKos] Zıplama atlandı: {JumpFolder} içinde {JumpFrameCount} kare bekleniyordu, {jumpFrames.Length} bulundu.");
            jumpFrames = null;
        }
        Sprite takeoff = frames.Length >= TakeoffRunFrame ? frames[TakeoffRunFrame - 1] : null;

        // Atılma: klasör yoksa ya da kare sayısı farklıysa atlanır (atılmada koşu / havadaki kareler görünür)
        Sprite[] dashFrames = AssetDatabase.IsValidFolder(DashFolder) ? ImportFrames(DashFolder) : new Sprite[0];
        if (dashFrames.Length != DashFrameCount)
        {
            Debug.LogWarning($"[KosKos] Atılma atlandı: {DashFolder} içinde {DashFrameCount} kare bekleniyordu, {dashFrames.Length} bulundu.");
            dashFrames = null;
        }

        AnimationClip clip = CreateOrUpdateClip(frames);
        AnimatorController controller = GetOrCreateController(clip);
        SetupPlayerPrefab(frames[0], controller, takeoff, jumpFrames, dashFrames);
        AssetDatabase.SaveAssets();
        Debug.Log($"[KosKos] Koşu animasyonu oyuncuya bağlandı: {frames.Length} kare, {FramesPerSecond} fps." +
            (jumpFrames != null ? $" Zıplama: koşu {TakeoffRunFrame} + {jumpFrames.Length} kare." : "") +
            (jumpFrames != null && dashFrames != null ? $" Atılma: {dashFrames.Length} kare." : ""));
    }

    // Kareleri sprite olarak ayarlar ve dosya adının sonundaki sayıya göre sıralı döndürür
    // ("JUMP 1", "jump 4", "10" gibi; "10" "2"den sonra gelsin diye sayıyla sıralanır)
    private static Sprite[] ImportFrames(string folder)
    {
        string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
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
        System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(name, @"(\d+)\s*$");
        return m.Success && int.TryParse(m.Groups[1].Value, out int number) ? number : int.MaxValue;
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

    private static void SetupPlayerPrefab(Sprite firstFrame, AnimatorController controller, Sprite takeoff, Sprite[] jumpFrames, Sprite[] dashFrames)
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

            // Havadaki kareleri seçen bileşen; zıplama kareleri yoksa havada da koşu döngüsü oynar
            var spriteAnimator = visual.GetComponent<PlayerSpriteAnimator>();
            if (jumpFrames != null)
            {
                if (spriteAnimator == null) spriteAnimator = visual.gameObject.AddComponent<PlayerSpriteAnimator>();
                var so = new SerializedObject(spriteAnimator);
                so.FindProperty("takeoffFrame").objectReferenceValue = takeoff;
                SerializedProperty rise = so.FindProperty("riseFrames");
                rise.arraySize = 2;
                rise.GetArrayElementAtIndex(0).objectReferenceValue = jumpFrames[0];
                rise.GetArrayElementAtIndex(1).objectReferenceValue = jumpFrames[1];
                so.FindProperty("peakFrame").objectReferenceValue = jumpFrames[2];
                so.FindProperty("peakToFallFrame").objectReferenceValue = jumpFrames[3];
                so.FindProperty("fallFrame").objectReferenceValue = jumpFrames[4];
                // Atılma kareleri yoksa boş bırakılır: bileşen atılmada kare değiştirmez
                SerializedProperty dashTransition = so.FindProperty("dashTransitionFrames");
                dashTransition.arraySize = dashFrames != null ? DashFrameCount - 1 : 0;
                for (int i = 0; i < dashTransition.arraySize; i++)
                {
                    dashTransition.GetArrayElementAtIndex(i).objectReferenceValue = dashFrames[i];
                }
                so.FindProperty("dashHoldFrame").objectReferenceValue = dashFrames != null ? dashFrames[DashFrameCount - 1] : null;
                so.FindProperty("runStateName").stringValue = controller.layers[0].stateMachine.defaultState.name;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else if (spriteAnimator != null)
            {
                Object.DestroyImmediate(spriteAnimator);
            }

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
