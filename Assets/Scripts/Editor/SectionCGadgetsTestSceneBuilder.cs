using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using U = TestSceneBuilderUtils;

// Tek tıkla alet (kanca, jet-çanta, manyetik bot) test sahnesini üretir: KosKos > Build Section C Gadgets Test Scene.
// Sahne her seferinde yeniden oluşturulur; elle düzenlemeyin.
public static class SectionCGadgetsTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Test_SectionC_Gadgets.unity";

    internal static readonly Color SteelColor = new Color(0.55f, 0.65f, 0.8f);
    private static readonly Color BatteryColor = new Color(0.3f, 0.9f, 0.35f);

    [MenuItem("KosKos/Build Section C Gadgets Test Scene")]
    private static void Build()
    {
        if (!U.BeginScene(out Scene scene, out InputActionAsset actions, out U.Layers layers)) return;

        Transform level = new GameObject("Level").transform;

        // Kanca, sallanma: 24-36 arası boşluk. Zıpla, havada E basılı tut, doğru anda bırak.
        U.Ground(level, layers.Ground, "Ground_A", -5f, 24f);
        HookPointAt(level, "HookPoint_Swing", new Vector3(30f, 9f, 0f));
        // Tavan çarpması testi: Hierarchy'de açınca sallanmanın ön tarafına alçak bir tavan koyar (varsayılan kapalı)
        U.Block(level, layers.Ground, "SwingCeilingTest", 33f, 4.6f, 4f, 1f).SetActive(false);

        // Kanca, çekilme: 52-57 arası boşluk. Karşıdaki çıkıntı zıplamayı engeller; çekilme altından geçer.
        U.Ground(level, layers.Ground, "Ground_B", 36f, 52f);
        HookPointAt(level, "HookPoint_Pull", new Vector3(58f, 1f, 0f));
        U.Block(level, layers.Ground, "PullOverhang", 56f, 2.2f, 6f, 6f);

        // Jet-çanta: 7 yüksek duvar, sonra 130-165 arası uzun boşluk
        U.Ground(level, layers.Ground, "Ground_C", 57f, 130f);
        U.Block(level, layers.Ground, "JetWall", 90f, 0f, 1f, 7f);
        U.Ground(level, layers.Ground, "Ground_D", 165f, 195f);
        BatteryAt(level, "Battery", new Vector3(148f, 5f, 0f));

        // Manyetik bot, tavan: 185-215 arası metal tavan (alt yüzü y=5), altında 195-210 arası boşluk
        GameObject ceiling = U.Block(level, layers.Ground, "MetalCeiling", 185f, 5f, 30f, 1f);
        U.Colorize(ceiling, SteelColor);
        ceiling.AddComponent<MetalCeiling>();
        U.Ground(level, layers.Ground, "Ground_E", 210f, 230f);

        // Manyetik bot, duvar: 230-260 arası boşluk; arkada metal duvar alanı
        U.Ground(level, layers.Ground, "Ground_F", 260f, 300f);
        MetalWallBetween(level, "MetalWall", 228f, 262f, 0f, 7f);

        U.SafeZoneAt(level, "SafeZone_Start", 0f, true);
        U.SafeZoneAt(level, "SafeZone_Pull", 42f, false);
        U.SafeZoneAt(level, "SafeZone_Jet", 70f, false);
        U.SafeZoneAt(level, "SafeZone_JetGap", 120f, false);
        U.SafeZoneAt(level, "SafeZone_Ceiling", 175f, false);
        U.SafeZoneAt(level, "SafeZone_Wall", 222f, false);
        U.SafeZoneAt(level, "SafeZone_End", 285f, false);
        U.KillZone(level, 150f, 400f);

        GameObject player = U.SpawnPlayer(layers, actions, new Vector3(0f, 1.05f, 0f));
        U.AddFollowCamera(player);

        U.SaveScene(scene, ScenePath);
    }

    // Collider'sız küre; rengi HookPoint kendisi verir
    internal static void HookPointAt(Transform parent, string name, Vector3 position)
    {
        GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        point.name = name;
        Object.DestroyImmediate(point.GetComponent<Collider>());
        point.transform.SetParent(parent);
        point.transform.position = position;
        point.transform.localScale = Vector3.one * 0.6f;
        point.AddComponent<HookPoint>();
    }

    // Yeşil kapsül tetik
    internal static void BatteryAt(Transform parent, string name, Vector3 position)
    {
        GameObject battery = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        battery.name = name;
        battery.transform.SetParent(parent);
        battery.transform.position = position;
        battery.transform.localScale = new Vector3(0.8f, 0.6f, 0.8f);
        battery.GetComponent<Collider>().isTrigger = true;
        battery.AddComponent<Battery>();
        U.Colorize(battery, BatteryColor);
    }

    // Oyun düzlemindeki tetik alanı + arkada ince çelik panel (görsel, collider yok)
    internal static void MetalWallBetween(Transform parent, string name, float xStart, float xEnd, float yBottom, float yTop)
    {
        float width = xEnd - xStart;
        float height = yTop - yBottom;

        var area = new GameObject(name);
        area.transform.SetParent(parent);
        area.transform.position = new Vector3(xStart + width * 0.5f, yBottom + height * 0.5f, 0f);
        var trigger = area.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(width, height, U.LevelDepth);
        area.AddComponent<MetalWall>();

        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Panel";
        Object.DestroyImmediate(panel.GetComponent<Collider>());
        panel.transform.SetParent(area.transform, false);
        panel.transform.localPosition = new Vector3(0f, 0f, U.LevelDepth * 0.5f + 0.1f);
        panel.transform.localScale = new Vector3(width, height, 0.2f);
        U.Colorize(panel, SteelColor);
    }
}
