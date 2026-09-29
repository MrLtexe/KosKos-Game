using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Test sahnesi builder'larının ortak yardımcıları (layer'lar, küpler, güvenli alanlar, oyuncu prefab'ı).
public static class TestSceneBuilderUtils
{
    public const string InputAssetPath = "Assets/Input/KosKosControls.inputactions";
    public const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    public const float LevelDepth = 3f;

    private static readonly Color SwordFlashColor = new Color(1f, 0.95f, 0.3f);

    public struct Layers
    {
        public int Player;
        public int Ground;
        public int Phaseable;
        public int Enemy;
    }

    // Kaydetme sorusu, input kontrolü, layer'lar ve yeni boş sahne. false = iptal.
    public static bool BeginScene(out Scene scene, out InputActionAsset actions, out Layers layers)
    {
        scene = default;
        actions = null;
        layers = default;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

        if (AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath) == null)
        {
            Debug.LogError($"[KosKos] Input dosyası bulunamadı: {InputAssetPath}. Sahne oluşturulmadı.");
            return false;
        }

        layers = new Layers
        {
            Player = EnsureLayer("Player"),
            Ground = EnsureLayer("Ground"),
            Phaseable = EnsureLayer("Phaseable"),
            Enemy = EnsureLayer("Enemy")
        };

        scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        // Yeni sahne açılınca Unity kullanılmayan asset'leri bellekten atar; bu yüzden asset'ler sahne açıldıktan sonra yüklenir
        actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
        return true;
    }

    public static void SaveScene(Scene scene, string path)
    {
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"[KosKos] Test sahnesi oluşturuldu: {path}");
    }

    public static int LevelMask(Layers layers) => (1 << layers.Ground) | (1 << layers.Phaseable);

    // Prefab varsa onu kullanır (sanat/ayar değişiklikleri korunur), yoksa oluşturup kaydeder
    public static GameObject SpawnPlayer(Layers layers, InputActionAsset actions, Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            GameObject built = BuildPlayer(layers, actions);
            prefab = PrefabUtility.SaveAsPrefabAsset(built, PlayerPrefabPath);
            Object.DestroyImmediate(built);
            Debug.Log($"[KosKos] Player prefab'ı oluşturuldu: {PlayerPrefabPath}");
        }
        else
        {
            Debug.Log("[KosKos] Var olan Player prefab'ı kullanıldı. Koda yeni bileşen eklendiyse prefab'ı silip sahneyi yeniden oluşturun.");
        }

        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.transform.position = position;
        return player;
    }

    public static void AddFollowCamera(GameObject player)
    {
        var follow = Camera.main.gameObject.AddComponent<CameraFollow>();
        SetField(follow, "target", player.transform);
    }

    private static GameObject BuildPlayer(Layers layers, InputActionAsset actions)
    {
        // Geçici görsel: kapsül (yükseklik 2, yarıçap 0.5). CreatePrimitive CapsuleCollider'ı da ekler.
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.layer = layers.Player;

        var inputReader = player.AddComponent<PlayerInputReader>();
        var motor = player.AddComponent<PlayerMotor>();
        player.AddComponent<PlayerDeath>();
        player.AddComponent<JumpAbility>();
        var dash = player.AddComponent<DashAbility>();
        var sword = player.AddComponent<SwordAttack>();
        player.AddComponent<BoostChargeAbility>();
        var tint = player.AddComponent<ChargeTintPlaceholder>();

        SetField(inputReader, "actions", actions);
        SetLayerMask(motor, "groundLayers", LevelMask(layers));
        SetLayerMask(dash, "phaseableLayers", 1 << layers.Phaseable);
        SetLayerMask(sword, "hitLayers", ~(1 << layers.Player));
        SetField(tint, "targetRenderer", player.GetComponent<Renderer>());

        // GEÇİCİ kılıç görseli: oyuncunun önünde, kapsülün arkasında sarı bir panel
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Quad);
        flash.name = "SwordHitboxVisual";
        Object.DestroyImmediate(flash.GetComponent<Collider>());
        flash.transform.SetParent(player.transform, false);
        Colorize(flash, SwordFlashColor);
        var flashRenderer = flash.GetComponent<Renderer>();
        flashRenderer.enabled = false;
        SetField(sword, "hitboxVisual", flashRenderer);

        return player;
    }

    // xStart-xEnd arası, üst yüzeyi y=0 olan zemin
    public static GameObject Ground(Transform parent, int layer, string name, float xStart, float xEnd)
    {
        float width = xEnd - xStart;
        return Cube(parent, layer, name, new Vector3(xStart + width * 0.5f, -0.5f, 0f), new Vector3(width, 1f, LevelDepth));
    }

    // Sol kenarı xLeft, alt kenarı yBottom olan blok
    public static GameObject Block(Transform parent, int layer, string name, float xLeft, float yBottom, float width, float height)
    {
        return Cube(parent, layer, name, new Vector3(xLeft + width * 0.5f, yBottom + height * 0.5f, 0f), new Vector3(width, height, LevelDepth));
    }

    public static GameObject Cube(Transform parent, int layer, string name, Vector3 center, Vector3 size)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.layer = layer;
        cube.transform.SetParent(parent);
        cube.transform.position = center;
        cube.transform.localScale = size;
        return cube;
    }

    public static void Colorize(GameObject target, Color color)
    {
        var placeholder = target.AddComponent<PlaceholderColor>();
        var so = new SerializedObject(placeholder);
        so.FindProperty("color").colorValue = color;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SafeZoneAt(Transform parent, string name, float x, bool isStart)
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

    // Seviyenin altında, centerX etrafında width genişliğinde görünmez ölüm alanı
    public static void KillZone(Transform parent, float centerX, float width)
    {
        var kill = new GameObject("KillZone");
        kill.transform.SetParent(parent);
        kill.transform.position = new Vector3(centerX, -10f, 0f);
        var killCol = kill.AddComponent<BoxCollider>();
        killCol.isTrigger = true;
        killCol.size = new Vector3(width, 2f, 10f);
        kill.AddComponent<KillZone>();
    }

    public static void SetField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetLayerMask(Object target, string fieldName, int mask)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).intValue = mask;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetFloat(Object target, string fieldName, float value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetInt(Object target, string fieldName, int value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetBool(Object target, string fieldName, bool value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetEnum(Object target, string fieldName, int index)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).enumValueIndex = index;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
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
