using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

// Tek tıkla Section B test sahnesini sıfırdan üretir: KosKos > Build Section B Test Scene.
// Sahne her seferinde yeniden oluşturulur; elle düzenlemeyin.
// Player prefab'ı (Assets/Prefabs/Player.prefab) sadece yoksa oluşturulur; varsa olduğu gibi kullanılır.
public static class SectionBTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Test_SectionB.unity";
    private const string InputAssetPath = "Assets/Input/KosKosControls.inputactions";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const float LevelDepth = 3f;

    private static readonly Color BreakableColor = new Color(1f, 0.55f, 0.1f);
    private static readonly Color ThinWallColor = new Color(0.2f, 0.9f, 1f);
    private static readonly Color SwordFlashColor = new Color(1f, 0.95f, 0.3f);

    [MenuItem("KosKos/Build Section B Test Scene")]
    private static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath) == null)
        {
            Debug.LogError($"[KosKos] Input dosyası bulunamadı: {InputAssetPath}. Sahne oluşturulmadı.");
            return;
        }

        int playerLayer = EnsureLayer("Player");
        int groundLayer = EnsureLayer("Ground");
        int phaseableLayer = EnsureLayer("Phaseable");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        // Yeni sahne açılınca Unity kullanılmayan asset'leri bellekten atar; bu yüzden asset'ler sahne açıldıktan sonra yüklenir
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);

        Transform level = new GameObject("Level").transform;
        BuildLevel(level, groundLayer, phaseableLayer);

        GameObject player = SpawnPlayer(playerLayer, groundLayer, phaseableLayer, actions, new Vector3(0f, 1.05f, 0f));

        Camera cam = Camera.main;
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        SetField(follow, "target", player.transform);

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[KosKos] Test sahnesi oluşturuldu: {ScenePath}");
    }

    private static void BuildLevel(Transform parent, int groundLayer, int phaseableLayer)
    {
        // Zemin üst yüzeyi y=0. Her zemin parçası TEK uzun küp (eklem yerlerinde yanlış çarpmayı önler).
        // --- Faz 1 bölümü ---
        // Bölüm 1: düz koşu + alçak duvar (tek zıplama ile aşılır)
        Ground(parent, groundLayer, "Ground_A", -5f, 40f);
        Block(parent, groundLayer, "LowWall", 20f, 0f, 1f, 1.5f);

        // Boşluk 40-44 (tek zıplama ile geçilir)
        Ground(parent, groundLayer, "Ground_B", 44f, 80f);
        // Yüksek duvar: sadece çift zıplama ile aşılır
        Block(parent, groundLayer, "TallWall", 62f, 0f, 1f, 4.5f);

        // Geniş boşluk 80-87 (zıplama + havada atılma gerekir)
        Ground(parent, groundLayer, "Ground_C", 87f, 250f);
        // Yükseltilmiş platform: üstüne inmek güvenli, yanına çarpmak ölüm
        Block(parent, groundLayer, "RaisedPlatform", 100f, 0f, 10f, 2f);
        // Alçak tavan: altında zıplayınca kafa çarpar ama ölmez
        Block(parent, groundLayer, "LowCeiling", 115f, 3.2f, 10f, 1f);

        // --- Faz 2 bölümü ---
        // Yerdeki kırılabilir: kılıçla kır ya da çarpıp öl
        BreakableBlock(parent, phaseableLayer, "Breakable_Ground", 145f, 0f, 1f, 2f);
        // Havadaki kırılabilir: havada kılıçla vurunca atılma yenilenir (yerden kılıçla erişilemeyecek yükseklikte)
        BreakableBlock(parent, phaseableLayer, "Breakable_Floating", 160f, 3f, 1f, 1f);
        // 175-195 arası düz alan: Boost şarjı denemek için
        // Yüksek kırılabilir duvar: zıplanamaz; boost'la çarp veya kılıçla kır
        BreakableBlock(parent, phaseableLayer, "Breakable_TallWall", 195f, 0f, 1f, 6f);
        // İnce duvar: sadece sıyrılganlık ile atılarak geçilir
        GameObject thinWall = Block(parent, phaseableLayer, "ThinWall_Phase", 225f, 0f, 0.3f, 6f);
        Colorize(thinWall, ThinWallColor);

        SafeZoneAt(parent, "SafeZone_Start", 0f, true);
        SafeZoneAt(parent, "SafeZone_Mid", 48f, false);
        SafeZoneAt(parent, "SafeZone_Phase2", 135f, false);

        var kill = new GameObject("KillZone");
        kill.transform.SetParent(parent);
        kill.transform.position = new Vector3(120f, -10f, 0f);
        var killCol = kill.AddComponent<BoxCollider>();
        killCol.isTrigger = true;
        killCol.size = new Vector3(400f, 2f, 10f);
        kill.AddComponent<KillZone>();
    }

    // Prefab varsa onu kullanır (sanat/ayar değişiklikleri korunur), yoksa oluşturup kaydeder
    private static GameObject SpawnPlayer(int playerLayer, int groundLayer, int phaseableLayer, InputActionAsset actions, Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            GameObject built = BuildPlayer(playerLayer, groundLayer, phaseableLayer, actions);
            prefab = PrefabUtility.SaveAsPrefabAsset(built, PlayerPrefabPath);
            Object.DestroyImmediate(built);
            Debug.Log($"[KosKos] Player prefab'ı oluşturuldu: {PlayerPrefabPath}");
        }
        else
        {
            Debug.Log($"[KosKos] Var olan Player prefab'ı kullanıldı. Koda yeni bileşen eklendiyse prefab'ı silip sahneyi yeniden oluşturun.");
        }

        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.transform.position = position;
        return player;
    }

    private static GameObject BuildPlayer(int playerLayer, int groundLayer, int phaseableLayer, InputActionAsset actions)
    {
        // Geçici görsel: kapsül (yükseklik 2, yarıçap 0.5). CreatePrimitive CapsuleCollider'ı da ekler.
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.layer = playerLayer;

        var inputReader = player.AddComponent<PlayerInputReader>();
        var motor = player.AddComponent<PlayerMotor>();
        player.AddComponent<PlayerDeath>();
        player.AddComponent<JumpAbility>();
        var dash = player.AddComponent<DashAbility>();
        var sword = player.AddComponent<SwordAttack>();
        player.AddComponent<BoostChargeAbility>();
        var tint = player.AddComponent<ChargeTintPlaceholder>();

        SetField(inputReader, "actions", actions);
        SetLayerMask(motor, "groundLayers", (1 << groundLayer) | (1 << phaseableLayer));
        SetLayerMask(dash, "phaseableLayers", 1 << phaseableLayer);
        SetLayerMask(sword, "hitLayers", ~(1 << playerLayer));
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
    private static GameObject Ground(Transform parent, int layer, string name, float xStart, float xEnd)
    {
        float width = xEnd - xStart;
        return Cube(parent, layer, name, new Vector3(xStart + width * 0.5f, -0.5f, 0f), new Vector3(width, 1f, LevelDepth));
    }

    // Sol kenarı xLeft, alt kenarı yBottom olan blok
    private static GameObject Block(Transform parent, int layer, string name, float xLeft, float yBottom, float width, float height)
    {
        return Cube(parent, layer, name, new Vector3(xLeft + width * 0.5f, yBottom + height * 0.5f, 0f), new Vector3(width, height, LevelDepth));
    }

    private static void BreakableBlock(Transform parent, int phaseableLayer, string name, float xLeft, float yBottom, float width, float height)
    {
        GameObject block = Block(parent, phaseableLayer, name, xLeft, yBottom, width, height);
        block.AddComponent<Breakable>();
        Colorize(block, BreakableColor);
    }

    private static GameObject Cube(Transform parent, int layer, string name, Vector3 center, Vector3 size)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.layer = layer;
        cube.transform.SetParent(parent);
        cube.transform.position = center;
        cube.transform.localScale = size;
        return cube;
    }

    private static void Colorize(GameObject target, Color color)
    {
        var placeholder = target.AddComponent<PlaceholderColor>();
        var so = new SerializedObject(placeholder);
        so.FindProperty("color").colorValue = color;
        so.ApplyModifiedPropertiesWithoutUndo();
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

    private static void SetLayerMask(Object target, string fieldName, int mask)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).intValue = mask;
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
