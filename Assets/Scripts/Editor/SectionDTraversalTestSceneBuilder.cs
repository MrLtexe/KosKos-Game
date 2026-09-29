using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using U = TestSceneBuilderUtils;

// Tek tıkla fırlatma alanı + zipline + sapan test sahnesini üretir: KosKos > Build Section D Traversal Test Scene.
// Sahne her seferinde yeniden oluşturulur; elle düzenlemeyin.
public static class SectionDTraversalTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Test_SectionD_Traversal.unity";

    private static readonly Color PadColor = new Color(0.2f, 0.85f, 0.3f);
    private static readonly Color CableColor = new Color(0.2f, 0.2f, 0.2f);
    private static readonly Color SlingColor = new Color(0.55f, 0.8f, 1f);

    [MenuItem("KosKos/Build Section D Traversal Test Scene")]
    private static void Build()
    {
        if (!U.BeginScene(out Scene scene, out InputActionAsset actions, out U.Layers layers)) return;

        Transform level = new GameObject("Level").transform;

        // Fırlatma alanı bölümü: 6.5 yüksek duvar sadece fırlatma + ek hava zıplaması ile aşılır
        U.Ground(level, layers.Ground, "Ground_A", -5f, 48f);
        LaunchPadAt(level, "LaunchPad", 15f);
        U.Block(level, layers.Ground, "PadWall", 24f, 0f, 1f, 6.5f);

        // Zipline bölümü: 48-90 arası boşluk; hat zeminin üstünde bitiyor (90'dan sonra)
        U.Ground(level, layers.Ground, "Ground_B", 90f, 180f);
        ZiplineBetween(level, "Zipline", new Vector3(48f, 4f, 0f), new Vector3(92f, 3f, 0f));

        // Sapan bölümü: havadaki alan ve 8 yüksek duvar
        SlingZoneAt(level, "SlingZone", new Vector3(120f, 4f, 0f));
        U.Block(level, layers.Ground, "SlingWall", 128f, 0f, 1f, 8f);

        U.SafeZoneAt(level, "SafeZone_Start", 0f, true);
        U.SafeZoneAt(level, "SafeZone_Zipline", 40f, false);
        U.SafeZoneAt(level, "SafeZone_Sling", 95f, false);
        U.SafeZoneAt(level, "SafeZone_End", 140f, false);
        U.KillZone(level, 90f, 300f);

        GameObject player = U.SpawnPlayer(layers, actions, new Vector3(0f, 1.05f, 0f));
        U.AddFollowCamera(player);

        U.SaveScene(scene, ScenePath);
    }

    internal static void LaunchPadAt(Transform parent, string name, float xLeft)
    {
        // Zeminde ince yeşil plaka; trigger olduğu için üstünden koşarken takılmaz
        GameObject pad = U.Block(parent, 0, name, xLeft, 0f, 1.5f, 0.2f);
        pad.GetComponent<Collider>().isTrigger = true;
        pad.AddComponent<LaunchPad>();
        U.Colorize(pad, PadColor);
    }

    internal static void ZiplineBetween(Transform parent, string name, Vector3 start, Vector3 end)
    {
        Vector3 middle = (start + end) * 0.5f;
        Vector3 delta = end - start;
        float length = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        var root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = middle;
        root.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Tutunma tetiği: hattın etrafında 1.5 kalınlığında
        var trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(length, 1.5f, U.LevelDepth);

        // Görsel kablo (collider yok)
        GameObject cable = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cable.name = "Cable";
        Object.DestroyImmediate(cable.GetComponent<Collider>());
        cable.transform.SetParent(root.transform, false);
        cable.transform.localScale = new Vector3(length, 0.06f, 0.06f);
        U.Colorize(cable, CableColor);

        var startPoint = new GameObject("Start").transform;
        startPoint.SetParent(root.transform);
        startPoint.position = start;
        var endPoint = new GameObject("End").transform;
        endPoint.SetParent(root.transform);
        endPoint.position = end;

        var zipline = root.AddComponent<Zipline>();
        U.SetField(zipline, "start", startPoint);
        U.SetField(zipline, "end", endPoint);
    }

    internal static void SlingZoneAt(Transform parent, string name, Vector3 center)
    {
        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = name;
        zone.transform.SetParent(parent);
        zone.transform.position = center;
        zone.transform.localScale = new Vector3(3f, 3f, 1f);
        zone.GetComponent<Collider>().isTrigger = true;
        zone.AddComponent<SlingZone>();
        U.Colorize(zone, SlingColor);
    }
}
