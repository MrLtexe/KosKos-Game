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
    private static readonly Color BalanceBarColor = new Color(0.15f, 0.15f, 0.15f);

    // Güncel Player prefab'ında olması gereken bileşenler; eksikse prefab eski demektir
    private static readonly System.Type[] RequiredPlayerComponents =
    {
        typeof(PlayerInputReader), typeof(PlayerMotor), typeof(PlayerDeath), typeof(JumpAbility),
        typeof(DashAbility), typeof(SwordAttack), typeof(BoostChargeAbility), typeof(ZiplineRider),
        typeof(SlingAbility), typeof(ChargeTintPlaceholder), typeof(BalanceBarPlaceholder), typeof(SlingArrowPlaceholder)
    };

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
        if (prefab == null || ShouldRebuildOutdated(prefab))
        {
            // Aynı yola kaydetmek prefab'ın GUID'ini korur; diğer test sahneleri oyuncuyu kaybetmez
            GameObject built = BuildPlayer(layers, actions);
            prefab = PrefabUtility.SaveAsPrefabAsset(built, PlayerPrefabPath);
            Object.DestroyImmediate(built);
            Debug.Log($"[KosKos] Player prefab'ı oluşturuldu: {PlayerPrefabPath}");
        }

        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.transform.position = position;
        return player;
    }

    // Prefab eksik bileşen içeriyorsa kullanıcıya sorar; true = yeniden oluştur
    private static bool ShouldRebuildOutdated(GameObject prefab)
    {
        var missing = new System.Text.StringBuilder();
        foreach (System.Type type in RequiredPlayerComponents)
        {
            if (prefab.GetComponent(type) == null) missing.Append(type.Name).Append(' ');
        }

        if (missing.Length == 0)
        {
            Debug.Log("[KosKos] Var olan Player prefab'ı kullanıldı.");
            return false;
        }

        bool rebuild = EditorUtility.DisplayDialog(
            "Player prefab'ı eski",
            $"Eksik bileşenler: {missing}\n\nPrefab şimdi yeniden oluşturulsun mu?\n" +
            "(Kilit açma tiklerini — çift zıplama, sıyrılganlık, savuşturma — tekrar işaretlemen gerekir.)",
            "Yeniden oluştur",
            "Eski prefab ile devam et");

        if (!rebuild)
        {
            Debug.LogWarning($"[KosKos] Eski Player prefab'ı kullanıldı; eksik bileşenler: {missing}");
        }
        return rebuild;
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
        player.AddComponent<ZiplineRider>();
        player.AddComponent<SlingAbility>();
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

        BuildBalanceBar(player);
        BuildSlingArrow(player);

        return player;
    }

    // GEÇİCİ zipline denge çubuğu: karakterin üstünde, kameraya doğru hafif önde
    private static void BuildBalanceBar(GameObject player)
    {
        var barRoot = new GameObject("BalanceBar");
        barRoot.transform.SetParent(player.transform, false);
        barRoot.transform.localPosition = new Vector3(0f, 1.6f, -0.6f);

        GameObject bar = Quad(barRoot.transform, "Bar", Vector3.zero, new Vector3(1.5f, 0.15f, 1f));
        Colorize(bar, BalanceBarColor);
        GameObject cursor = Quad(barRoot.transform, "Cursor", new Vector3(0f, 0f, -0.01f), new Vector3(0.1f, 0.3f, 1f));

        TextMesh left = Text(barRoot.transform, "KeyA", "A", new Vector3(-1f, 0f, 0f));
        TextMesh right = Text(barRoot.transform, "KeyD", "D", new Vector3(1f, 0f, 0f));

        var view = player.AddComponent<BalanceBarPlaceholder>();
        SetField(view, "barRoot", barRoot);
        SetField(view, "cursor", cursor.GetComponent<Renderer>());
        SetField(view, "leftKey", left);
        SetField(view, "rightKey", right);

        barRoot.SetActive(false);
    }

    // GEÇİCİ sapan oku ve "Space: fırlat" ipucu
    private static void BuildSlingArrow(GameObject player)
    {
        var arrowObject = new GameObject("SlingArrow");
        arrowObject.transform.SetParent(player.transform, false);
        var line = arrowObject.AddComponent<LineRenderer>();
        line.enabled = false;

        TextMesh hint = Text(player.transform, "SlingHint", "Space: fırlat", new Vector3(0f, 1.8f, -0.6f));
        hint.gameObject.SetActive(false);

        var view = player.AddComponent<SlingArrowPlaceholder>();
        SetField(view, "arrow", line);
        SetField(view, "hint", hint.gameObject);
    }

    private static GameObject Quad(Transform parent, string name, Vector3 localPosition, Vector3 localScale)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = localPosition;
        quad.transform.localScale = localScale;
        return quad;
    }

    // Unity'nin yerleşik fontuyla basit 3B yazı (TextMeshPro kurulumu gerekmez)
    public static TextMesh Text(Transform parent, string name, string text, Vector3 localPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        var textMesh = go.AddComponent<TextMesh>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textMesh.font = font;
        textMesh.text = text;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 48;
        textMesh.characterSize = 0.05f;

        MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = font.material;
        return textMesh;
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
