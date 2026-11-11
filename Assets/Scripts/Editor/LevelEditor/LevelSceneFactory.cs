using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using U = TestSceneBuilderUtils;

// Bölüm editörünün sahne işleri: oynanabilir boş yeni bölüm sahnesi kurmak ve kit parçalarını sahneye yerleştirmek.
public static class LevelSceneFactory
{
    private const string LevelRootName = "Level";
    private const float GridSnap = 0.5f;

    // Yeni bölüm: oyuncu, kamera, arayüz, diyalog, LevelSettings + LevelRun ve başlangıç parçaları (zemin, başlangıç alanı, hedef, ölüm alanı).
    // Sahne kaydedilmez: bölüm ancak editörde Save'e basılınca oyuna eklenir. false = kullanıcı iptal etti
    public static bool CreateNewLevelScene()
    {
        if (!U.BeginScene(out Scene scene, out InputActionAsset actions, out U.Layers layers)) return false;

        // BeginScene test loadout'lu bir LevelSettings ekler; bölüm kimliği boş kalır (= kaydedilmemiş yeni bölüm)
        Object.FindFirstObjectByType<LevelSettings>().gameObject.AddComponent<LevelRun>();

        GameObject player = U.SpawnPlayer(layers, actions, new Vector3(0f, 1.05f, 0f));
        U.AddFollowCamera(player);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = Color.black;
        U.AddLevelUI(scene);
        LevelPieces.AddDialogueObject(scene, false);

        Transform root = LevelRoot(scene);
        // Zemin -5..45 (kit zemini 10 genişliğinde, ortası kökte): X ölçeği 5, merkez x=20
        GameObject ground = Spawn(LevelKitBuilder.GroundCategory, LevelKitBuilder.GroundPiece, root, new Vector3(20f, 0f, 0f));
        if (ground != null) ground.transform.localScale = new Vector3(5f, 1f, 1f);

        GameObject start = Spawn(LevelKitBuilder.FlowCategory, LevelKitBuilder.SafeZonePiece, root, Vector3.zero);
        if (start != null)
        {
            start.name = "SafeZone_Start";
            var so = new SerializedObject(start.GetComponentInChildren<SafeZone>());
            so.FindProperty("isStartZone").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Spawn(LevelKitBuilder.FlowCategory, LevelKitBuilder.GoalPiece, root, new Vector3(40f, 0f, 0f));
        // Kit ölüm alanı 40 genişliğinde: 2 kat = 80, merkez 20
        GameObject kill = Spawn(LevelKitBuilder.FlowCategory, LevelKitBuilder.KillZonePiece, root, new Vector3(20f, 0f, 0f));
        if (kill != null) kill.transform.localScale = new Vector3(2f, 1f, 1f);

        // Ardından gelen "Undo" yeni sahnenin kurulumunu geri almasın
        Undo.ClearAll();
        return true;
    }

    // Kit parçasını sahne görünümünün ortasına (0.5 ızgaraya oturtup z=0) koyar ve seçer. Geri alınabilir (Ctrl+Z)
    public static GameObject Place(GameObject kitPrefab)
    {
        Scene scene = SceneManager.GetActiveScene();
        Vector3 pivot = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        var position = new Vector3(Snap(pivot.x), Snap(pivot.y), 0f);

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(kitPrefab, scene);
        Undo.RegisterCreatedObjectUndo(instance, $"Place {kitPrefab.name}");
        Undo.SetTransformParent(instance.transform, LevelRoot(scene), "Place");
        instance.transform.position = position;

        AssignCollectibleIds(instance, scene);
        Selection.activeGameObject = instance;
        return instance;
    }

    // Sahnedeki "Level" kökü (yoksa oluşturulur); tüm bölüm parçaları bunun altında durur
    public static Transform LevelRoot(Scene scene)
    {
        GameObject existing = scene.GetRootGameObjects().FirstOrDefault(g => g.name == LevelRootName);
        if (existing != null) return existing.transform;

        var root = new GameObject(LevelRootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        Undo.RegisterCreatedObjectUndo(root, "Create Level root");
        return root.transform;
    }

    // Sahnedeki en büyük Kumaş numarası + 1 (silinen numaralar tekrar kullanılmaz; kimlikler hiç yeniden numaralanmaz)
    public static string NextKumasId(Scene scene, string levelId)
    {
        string prefix = levelId + GameProgress.KumasTag;
        int highest = 0;
        foreach (Collectible c in Collectibles(scene))
        {
            if (c.Id == null || !c.Id.StartsWith(prefix)) continue;
            if (int.TryParse(c.Id.Substring(prefix.Length), out int n) && n > highest) highest = n;
        }
        return prefix + (highest + 1);
    }

    public static string BelgeId(string levelId) => levelId + GameProgress.BelgeTag + "1";

    public static Collectible[] Collectibles(Scene scene)
    {
        return scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Collectible>(true)).ToArray();
    }

    // Bölüm kimliği: kayıtlı bölümde LevelSettings'teki; kaydedilmemiş yeni bölümde kaydedilince alacağı kimlik
    public static string CurrentLevelId(Scene scene)
    {
        LevelSettings settings = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<LevelSettings>(true)).FirstOrDefault();
        string id = settings != null ? settings.LevelId : null;
        return string.IsNullOrEmpty(id) ? LevelDocument.NextFreeLevel().id : id;
    }

    private static void AssignCollectibleIds(GameObject instance, Scene scene)
    {
        string levelId = CurrentLevelId(scene);
        foreach (Collectible c in instance.GetComponentsInChildren<Collectible>(true))
        {
            string id = c.Type == Collectible.Kind.Kumas ? NextKumasId(scene, levelId) : BelgeId(levelId);
            LevelPieces.SetCollectibleId(c, id);
        }
    }

    private static GameObject Spawn(string category, string piece, Transform parent, Vector3 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelKitBuilder.PrefabPath(category, piece));
        if (prefab == null)
        {
            Debug.LogWarning($"[KosKos] Kit parçası bulunamadı: {category}/{piece}. Önce KosKos > Level Kit > Build Kit Prefabs.");
            return null;
        }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.position = position;
        return instance;
    }

    private static float Snap(float value) => Mathf.Round(value / GridSnap) * GridSnap;
}
