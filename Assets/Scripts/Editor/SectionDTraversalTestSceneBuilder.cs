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
        SlingZoneAt(level, "SlingZone", new Vector3(120f, 4f, 0f), tutorial: true);
        U.Block(level, layers.Ground, "SlingWall", 128f, 0f, 1f, 8f);

        // Yolculuk baloncuğu 1: eğri rota ile 180-198 arası boşluğun üstünden taşır, ileri-yukarı fırlatır
        BubbleAt(level, "TravelBubble_Curve", new Vector3(152f, 1.5f, 0f),
            new[] { new Vector3(165f, 6f, 0f), new Vector3(180f, 8f, 0f), new Vector3(194f, 5f, 0f) },
            new Vector3(1f, 0.4f, 0f), 16f);
        U.Ground(level, layers.Ground, "Ground_C", 198f, 250f);

        // Yolculuk baloncuğu 2: kısa rota, sonra neredeyse dik yukarı fırlatıp yüksek bir kenara çıkarır
        BubbleAt(level, "TravelBubble_Up", new Vector3(215f, 1.5f, 0f),
            new[] { new Vector3(222f, 3f, 0f), new Vector3(230f, 4f, 0f) },
            new Vector3(0.4f, 1f, 0f), 24f);
        U.Block(level, layers.Ground, "BubbleLedge", 234f, 6.5f, 20f, 1f);

        U.SafeZoneAt(level, "SafeZone_Start", 0f, true);
        U.SafeZoneAt(level, "SafeZone_Zipline", 40f, false);
        U.SafeZoneAt(level, "SafeZone_Sling", 95f, false);
        U.SafeZoneAt(level, "SafeZone_Bubble", 140f, false);
        U.SafeZoneAt(level, "SafeZone_Bubble2", 205f, false);
        U.KillZone(level, 130f, 400f);

        GameObject player = U.SpawnPlayer(layers, actions, new Vector3(0f, 1.05f, 0f));
        U.AddFollowCamera(player);

        U.SaveScene(scene, ScenePath);
    }

    // Yolculuk baloncuğu: kökte giriş tetiği, çocuklarda rota noktaları (boş nesneler), fırlatma yönü ve hareket eden görsel.
    // ejectTowards: son noktadan fırlatma yönü (birim vektör gibi; nesne son noktadan bu kadar uzağa konur)
    internal static void BubbleAt(Transform parent, string name, Vector3 start, Vector3[] waypointPositions,
        Vector3 ejectTowards, float ejectSpeed)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = start;
        var trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.2f;

        var bubble = root.AddComponent<TravelBubble>();
        var so = new SerializedObject(bubble);

        SerializedProperty list = so.FindProperty("waypoints");
        list.arraySize = waypointPositions.Length;
        for (int i = 0; i < waypointPositions.Length; i++)
        {
            var waypoint = new GameObject($"Waypoint_{i + 1}").transform;
            waypoint.SetParent(root.transform);
            waypoint.position = waypointPositions[i];
            list.GetArrayElementAtIndex(i).objectReferenceValue = waypoint;
        }

        var eject = new GameObject("EjectDirection").transform;
        eject.SetParent(root.transform);
        eject.position = waypointPositions[waypointPositions.Length - 1] + ejectTowards.normalized * 3f;
        so.FindProperty("ejectDirection").objectReferenceValue = eject;
        so.FindProperty("ejectSpeed").floatValue = ejectSpeed;

        // GEÇİCİ görsel: oyuncuyu içine alan küre (renk ve saydamlık oyunda TravelBubble tarafından verilir)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "BubbleVisual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = Vector3.one * 2.6f;
        so.FindProperty("visual").objectReferenceValue = visual.transform;

        so.ApplyModifiedPropertiesWithoutUndo();
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

    // Yakalama alanı 3x3 (kolay girilsin); görünen kısım oyuncunun arkasında küçük bir küre (oyuncu ve ok görünsün).
    // tutorial: işaretliyse oyuncuya fareyle nişan alma + fırlatma tuşu uzun ipucuyla anlatılır (bölümdeki ilk sapan alanı)
    internal static void SlingZoneAt(Transform parent, string name, Vector3 center, bool tutorial = false)
    {
        var zone = new GameObject(name);
        zone.transform.SetParent(parent);
        zone.transform.position = center;
        var trigger = zone.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(3f, 3f, 1f);
        var slingZone = zone.AddComponent<SlingZone>();
        U.SetBool(slingZone, "showTutorialHint", tutorial);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(zone.transform, false);
        // Kameradan uzakta (z +): oyuncunun ve nişan okunun arkasında kalır
        visual.transform.localPosition = new Vector3(0f, 0f, 0.8f);
        visual.transform.localScale = Vector3.one * 1.2f;
        U.Colorize(visual, SlingColor);
    }
}
