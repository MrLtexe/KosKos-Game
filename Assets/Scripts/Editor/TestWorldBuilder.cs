using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using U = TestSceneBuilderUtils;
using B = SectionBTestSceneBuilder;
using C = SectionCGadgetsTestSceneBuilder;
using DE = SectionDEnemiesTestSceneBuilder;
using DT = SectionDTraversalTestSceneBuilder;

// Tek tıkla tüm sistemleri tek sahnede toplayan test dünyasını üretir: KosKos > Build Test World.
// Oyuncu bir bekleme alanında (hub) hareketsiz başlar. Her sistemin ayrı bir alanı vardır; alanlar üst üste,
// birbirinden uzakta durur (biri diğerinden görünmez). Her alanın başında bir güvenli alan, altında kendi ölüm alanı vardır:
// 1-9 ve 0 tuşları o alana ışınlar, R bulunulan alanı baştan alır, H hub'a döner (TestSectionTeleporter).
// Alanın sonundan düşünce ölünür ve aynı alanın başında doğulur, böylece alan kendiliğinden tekrar eder.
// Sahne her seferinde yeniden oluşturulur; elle düzenlemeyin. Tüm yetenekler açıktır (test loadout'u).
public static class TestWorldBuilder
{
    private const string ScenePath = "Assets/Scenes/Test_World.unity";

    // Alanlar arası dikey mesafe: en yüksek fırlatmalar (sapan ~11, jet-çanta) bir üstteki alanın ölüm alanına ulaşmaz
    private const float AreaHeight = 60f;

    private static readonly Color ThinWallColor = new Color(0.2f, 0.9f, 1f);

    private delegate void AreaBuilder(Transform area, U.Layers layers);

    [MenuItem("KosKos/Build Test World")]
    private static void Build()
    {
        if (!U.BeginScene(out Scene scene, out InputActionAsset actions, out U.Layers layers)) return;

        Transform level = new GameObject("Level").transform;

        // Sıra = tuş: 1..9, 0
        (string name, AreaBuilder build)[] areas =
        {
            ("Movement", BuildMovement),
            ("SwordBoost", BuildSwordBoost),
            ("Hook", BuildHook),
            ("JetBag", BuildJetBag),
            ("MagBoots", BuildMagBoots),
            ("Enemies", BuildEnemies),
            ("LaunchPad", BuildLaunchPad),
            ("Zipline", BuildZipline),
            ("Sling", BuildSling),
            ("Bubble", BuildBubble)
        };

        // Bekleme alanı (hub): küçük platform; oyuncu burada hareketsiz başlar ve alan seçer
        var hub = new GameObject("Hub").transform;
        hub.SetParent(level);
        U.Ground(hub, layers.Ground, "HubGround", -6f, 6f);
        U.SafeZoneAt(hub, TestSectionTeleporter.HubZoneName, 0f, true);

        for (int i = 0; i < areas.Length; i++)
        {
            // Alan x=0 etrafında kurulur, sonra kendi yüksekliğine taşınır (çocuklar da birlikte kayar)
            var area = new GameObject($"Area_{i + 1}_{areas[i].name}").transform;
            area.SetParent(level);
            U.SafeZoneAt(area, "SafeZone_" + areas[i].name, 0f, false);
            areas[i].build(area, layers);
            // Alanın hemen altında kendi ölüm alanı: sonundan düşen oyuncu bu alanın başında doğar
            U.KillZone(area, 100f, 400f);
            area.position = new Vector3(0f, (i + 1) * AreaHeight, 0f);
        }

        GameObject player = U.SpawnPlayer(layers, actions, new Vector3(0f, 1.05f, 0f));
        U.AddFollowCamera(player);
        U.AddLevelUI(scene);
        new GameObject("TestSectionTeleporter").AddComponent<TestSectionTeleporter>();

        U.SaveScene(scene, ScenePath);
    }

    // 1. Hareket: alçak duvar, boşluk, çift zıplama duvarı, atılma boşluğu, yükseltilmiş platform, alçak tavan, sıyrılganlık duvarı
    private static void BuildMovement(Transform a, U.Layers l)
    {
        int g = l.Ground;
        U.Ground(a, g, "Ground_A", -5f, 40f);
        U.Block(a, g, "LowWall", 20f, 0f, 1f, 1.5f);
        // Boşluk 40-44 (tek zıplama)
        U.Ground(a, g, "Ground_B", 44f, 80f);
        U.Block(a, g, "TallWall", 62f, 0f, 1f, 4.5f);
        // Boşluk 80-87 (zıplama + havada atılma)
        U.Ground(a, g, "Ground_C", 87f, 160f);
        U.Block(a, g, "RaisedPlatform", 100f, 0f, 10f, 2f);
        U.Block(a, g, "LowCeiling", 115f, 3.2f, 10f, 1f);
        GameObject thinWall = U.Block(a, l.Phaseable, "ThinWall_Phase", 140f, 0f, 0.3f, 6f);
        U.Colorize(thinWall, ThinWallColor);
    }

    // 2. Kılıç ve boost: yerdeki ve havadaki kırılabilir, boost için düz alan, yüksek kırılabilir duvar
    private static void BuildSwordBoost(Transform a, U.Layers l)
    {
        int g = l.Ground;
        U.Ground(a, g, "Ground", -5f, 120f);
        B.BreakableBlock(a, g, "Breakable_Ground", 20f, 0f, 1f, 2f);
        B.BreakableBlock(a, g, "Breakable_Floating", 35f, 3f, 1f, 1f);
        // 45-80 arası düz alan: boost şarjı
        B.BreakableBlock(a, g, "Breakable_TallWall", 85f, 0f, 1f, 6f);
    }

    // 3. Kanca: sallanma (24-36 boşluk), çekilme (52-57 boşluk, karşıda çıkıntı)
    private static void BuildHook(Transform a, U.Layers l)
    {
        int g = l.Ground;
        U.Ground(a, g, "Ground_A", -5f, 24f);
        C.HookPointAt(a, "HookPoint_Swing", new Vector3(30f, 9f, 0f));
        U.Ground(a, g, "Ground_B", 36f, 52f);
        C.HookPointAt(a, "HookPoint_Pull", new Vector3(58f, 1f, 0f));
        U.Block(a, g, "PullOverhang", 56f, 2.2f, 6f, 6f);
        U.Ground(a, g, "Ground_C", 57f, 100f);
    }

    // 4. Jet-çanta: 7 yüksek duvar, sonra bataryalı uzun boşluk (60-95)
    private static void BuildJetBag(Transform a, U.Layers l)
    {
        int g = l.Ground;
        U.Ground(a, g, "Ground_A", -5f, 60f);
        U.Block(a, g, "JetWall", 25f, 0f, 1f, 7f);
        C.BatteryAt(a, "Battery", new Vector3(78f, 5f, 0f));
        U.Ground(a, g, "Ground_B", 95f, 120f);
    }

    // 5. Manyetik bot: metal tavan (altında 30-45 boşluk), sonra metal duvar (65-95 boşluk)
    private static void BuildMagBoots(Transform a, U.Layers l)
    {
        int g = l.Ground;
        U.Ground(a, g, "Ground_A", -5f, 30f);
        GameObject ceiling = U.Block(a, g, "MetalCeiling", 20f, 5f, 30f, 1f);
        U.Colorize(ceiling, C.SteelColor);
        ceiling.AddComponent<MetalCeiling>();
        U.Ground(a, g, "Ground_B", 45f, 65f);
        C.MetalWallBetween(a, "MetalWall", 63f, 97f, 0f, 7f);
        U.Ground(a, g, "Ground_C", 95f, 120f);
    }

    // 6. Düşmanlar: önce birbirinden ayrı üç tavan silahı (her biri yaklaşırken menzil boyunca tek tek atış; aralar ekrana sığmayacak kadar uzun,
    // karşılaşmalar karışmaz), sonra (henüz yenilenmemiş) dronlar.
    // Sıradan atılmayı denemek için Play sırasında Test_AllUnlocked asset'inde Phase tikini kaldır (Play bitince geri tikle)
    private static void BuildEnemies(Transform a, U.Layers l)
    {
        U.Ground(a, l.Ground, "Ground", -5f, 225f);
        Bullet turretBullet = DE.TurretBullet(l);
        Bullet droneBullet = DE.DroneBullet(l);
        // Zorluk: kolay = küçük menzil, erken susar; zor = büyük menzil (daha çok atış), silaha daha yakın kadar ateş eder
        DE.Turret(a, l, "Turret_Easy", 35f, turretBullet, range: 14f, passTolerance: -3f);
        DE.Turret(a, l, "Turret_Normal", 70f, turretBullet);
        DE.Turret(a, l, "Turret_Hard", 110f, turretBullet, range: 24f, passTolerance: -1f);

        // Sadece test: oyuncunun üstünde "şimdi atıl" göstergesi (sarı = mermi geliyor, yeşil = şimdi atılırsan arkanda kalır)
        new GameObject("DodgeTimingIndicator").AddComponent<DodgeTimingIndicator>().transform.SetParent(a, false);
        DE.Drone(a, l, "Drone_Hover", 145f, 4.5f, droneBullet, 10f);
        // Devriye: 165 ile 175 arasında gidip gelir
        DE.Drone(a, l, "Drone_Patrol", 165f, 5f, droneBullet, 10f, 10f);
        DE.Drone(a, l, "Drone_Phase", 205f, 5f, droneBullet, 12f);
    }

    // 7. Fırlatma alanı: 6.5 yüksek duvar fırlatma + çift zıplama ile aşılır
    private static void BuildLaunchPad(Transform a, U.Layers l)
    {
        int g = l.Ground;
        U.Ground(a, g, "Ground", -5f, 60f);
        DT.LaunchPadAt(a, "LaunchPad", 15f);
        U.Block(a, g, "PadWall", 24f, 0f, 1f, 6.5f);
    }

    // 8. Zipline: 15-57 arası boşluğun üstünde
    private static void BuildZipline(Transform a, U.Layers l)
    {
        int g = l.Ground;
        U.Ground(a, g, "Ground_A", -5f, 15f);
        DT.ZiplineBetween(a, "Zipline", new Vector3(15f, 4f, 0f), new Vector3(59f, 3f, 0f));
        U.Ground(a, g, "Ground_B", 57f, 90f);
    }

    // 9. Sapan: havadaki alan ve 8 yüksek duvar
    private static void BuildSling(Transform a, U.Layers l)
    {
        int g = l.Ground;
        U.Ground(a, g, "Ground", -5f, 70f);
        DT.SlingZoneAt(a, "SlingZone", new Vector3(25f, 4f, 0f), tutorial: true);
        U.Block(a, g, "SlingWall", 33f, 0f, 1f, 8f);
    }

    // 0. Yolculuk baloncuğu: eğri rota ile boşluğun (40-58) üstünden, sonra dik yukarı yüksek kenara
    private static void BuildBubble(Transform a, U.Layers l)
    {
        int g = l.Ground;
        U.Ground(a, g, "Ground_A", -5f, 40f);
        DT.BubbleAt(a, "TravelBubble_Curve", new Vector3(12f, 1.5f, 0f),
            new[] { new Vector3(25f, 6f, 0f), new Vector3(40f, 8f, 0f), new Vector3(54f, 5f, 0f) },
            new Vector3(1f, 0.4f, 0f), 16f);
        U.Ground(a, g, "Ground_B", 58f, 115f);
        DT.BubbleAt(a, "TravelBubble_Up", new Vector3(75f, 1.5f, 0f),
            new[] { new Vector3(82f, 3f, 0f), new Vector3(90f, 4f, 0f) },
            new Vector3(0.4f, 1f, 0f), 24f);
        U.Block(a, g, "BubbleLedge", 94f, 6.5f, 20f, 1f);
    }
}
