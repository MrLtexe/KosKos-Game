using UnityEngine;
using U = TestSceneBuilderUtils;

// Aletlerin (kanca, jet-çanta, manyetik bot; Section C) ortak sahne parçaları: test dünyası ve prototip bölümler kullanır.
// (Eski ayrı "Section C Gadgets Test Scene" sahnesi kaldırıldı; testler KosKos > Scenes > Build Test World ile yapılır.)
public static class SectionCGadgetsTestSceneBuilder
{
    internal static readonly Color SteelColor = new Color(0.55f, 0.65f, 0.8f);
    private static readonly Color BatteryColor = new Color(0.3f, 0.9f, 0.35f);

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
