using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

// Tek tıkla Section B test sahnesini sıfırdan üretir: KosKos > Build Section B Test Scene.
// Sahne her seferinde yeniden oluşturulur; elle düzenlemeyin.
public static class SectionBTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Test_SectionB.unity";
    private const string InputAssetPath = "Assets/Input/KosKosControls.inputactions";
    private const float LevelDepth = 3f;

    [MenuItem("KosKos/Build Section B Test Scene")]
    private static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
        if (actions == null)
        {
            Debug.LogError($"[KosKos] Input dosyası bulunamadı: {InputAssetPath}. Sahne oluşturulmadı.");
            return;
        }

        int playerLayer = EnsureLayer("Player");
        int groundLayer = EnsureLayer("Ground");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        // Yeni sahne açılınca Unity kullanılmayan asset'leri bellekten atar; referans boş kaydedilmesin diye tekrar yükle
        actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);

        Transform level = new GameObject("Level").transform;
        BuildLevel(level, groundLayer);

        GameObject player = BuildPlayer(playerLayer, groundLayer, actions, new Vector3(0f, 1.05f, 0f));

        Camera cam = Camera.main;
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        SetField(follow, "target", player.transform);

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[KosKos] Test sahnesi oluşturuldu: {ScenePath}");
    }

    private static void BuildLevel(Transform parent, int groundLayer)
    {
        // Zemin üst yüzeyi y=0. Her zemin parçası TEK uzun küp (eklem yerlerinde yanlış çarpmayı önler).
        // Bölüm 1: düz koşu + alçak duvar (tek zıplama ile aşılır)
        Ground(parent, groundLayer, "Ground_A", -5f, 40f);
        Block(parent, groundLayer, "LowWall", 20f, 0f, 1f, 1.5f);

        // Boşluk 40-44 (tek zıplama ile geçilir)
        Ground(parent, groundLayer, "Ground_B", 44f, 80f);
        // Yüksek duvar: sadece çift zıplama ile aşılır
        Block(parent, groundLayer, "TallWall", 62f, 0f, 1f, 4.5f);

        // Geniş boşluk 80-87 (zıplama + havada atılma gerekir)
        Ground(parent, groundLayer, "Ground_C", 87f, 130f);
        // Yükseltilmiş platform: üstüne inmek güvenli, yanına çarpmak ölüm
        Block(parent, groundLayer, "RaisedPlatform", 100f, 0f, 10f, 2f);
        // Alçak tavan: altında zıplayınca kafa çarpar ama ölmez
        Block(parent, groundLayer, "LowCeiling", 115f, 3.2f, 10f, 1f);

        SafeZoneAt(parent, "SafeZone_Start", 0f, true);
        SafeZoneAt(parent, "SafeZone_Mid", 48f, false);

        var kill = new GameObject("KillZone");
        kill.transform.SetParent(parent);
        kill.transform.position = new Vector3(60f, -10f, 0f);
        var killCol = kill.AddComponent<BoxCollider>();
        killCol.isTrigger = true;
        killCol.size = new Vector3(220f, 2f, 10f);
        kill.AddComponent<KillZone>();
    }

    private static GameObject BuildPlayer(int playerLayer, int groundLayer, InputActionAsset actions, Vector3 position)
    {
        // Geçici görsel: kapsül (yükseklik 2, yarıçap 0.5). CreatePrimitive CapsuleCollider'ı da ekler.
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.layer = playerLayer;
        player.transform.position = position;

        var inputReader = player.AddComponent<PlayerInputReader>();
        var motor = player.AddComponent<PlayerMotor>();
        player.AddComponent<PlayerDeath>();
        player.AddComponent<JumpAbility>();
        player.AddComponent<DashAbility>();

        SetField(inputReader, "actions", actions);

        var so = new SerializedObject(motor);
        so.FindProperty("groundLayers").intValue = 1 << groundLayer;
        so.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    // xStart-xEnd arası, üst yüzeyi y=0 olan zemin
    private static void Ground(Transform parent, int layer, string name, float xStart, float xEnd)
    {
        float width = xEnd - xStart;
        Cube(parent, layer, name, new Vector3(xStart + width * 0.5f, -0.5f, 0f), new Vector3(width, 1f, LevelDepth));
    }

    // Sol kenarı xLeft, alt kenarı yBottom olan blok
    private static void Block(Transform parent, int layer, string name, float xLeft, float yBottom, float width, float height)
    {
        Cube(parent, layer, name, new Vector3(xLeft + width * 0.5f, yBottom + height * 0.5f, 0f), new Vector3(width, height, LevelDepth));
    }

    private static void Cube(Transform parent, int layer, string name, Vector3 center, Vector3 size)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.layer = layer;
        cube.transform.SetParent(parent);
        cube.transform.position = center;
        cube.transform.localScale = size;
    }

    private static void SafeZoneAt(Transform parent, string name, float x, bool isStart)
    {
        var zone = new GameObject(name);
        zone.transform.SetParent(parent);
        zone.transform.position = new Vector3(x, 3f, 0f);
        var col = zone.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(2f, 6f, LevelDepth);

        var spawn = new GameObject("SpawnPoint").transform;
        spawn.SetParent(zone.transform);
        spawn.position = new Vector3(x, 1.05f, 0f);

        var safeZone = zone.AddComponent<SafeZone>();
        var so = new SerializedObject(safeZone);
        so.FindProperty("spawnPoint").objectReferenceValue = spawn;
        so.FindProperty("isStartZone").boolValue = isStart;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Layer yoksa ilk boş kullanıcı slotuna (8-31) ekler
    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing != -1) return existing;

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                // Layer isimleri diske yazılsın; yoksa commit'te TagManager.asset eski kalabilir
                AssetDatabase.SaveAssets();
                return i;
            }
        }

        throw new System.InvalidOperationException($"'{layerName}' için boş layer slotu kalmadı.");
    }
}
