using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using U = TestSceneBuilderUtils;
using B = SectionBTestSceneBuilder;
using C = SectionCGadgetsTestSceneBuilder;
using DE = SectionDEnemiesTestSceneBuilder;
using DT = SectionDTraversalTestSceneBuilder;

// Tek tıkla 3 prototip bölümü üretir: KosKos > Build Prototype Levels.
// Bölüm sahneleri, her bölümün loadout'u ve bölüm kataloğu her seferinde yeniden yazılır; elle düzenlemeyin.
// Gerçek bölüm tasarımları gelince bu builder emekliye ayrılacak.
public static class PrototypeLevelsBuilder
{
    internal const string CatalogPath = "Assets/Levels/LevelCatalog.asset";
    internal const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

    private static readonly Color KumasColor = new Color(0.6f, 0.25f, 0.9f);
    private static readonly Color BelgeColor = Color.white;
    private static readonly Color ThinWallColor = new Color(0.2f, 0.9f, 1f);
    private static readonly Color GoalColor = new Color(0.2f, 0.85f, 0.3f);
    private static readonly Color FlagColor = new Color(1f, 0.9f, 0.2f);

    // Bölüm tanımları: kimlik, ad, sahne, madalya süreleri, loadout (çift zıplama, sıyrılganlık, savuşturma, kanca, jet, bot)
    private struct LevelDef
    {
        public string id, displayName, sceneName, belgeTitle, belgeText;
        public float silver, gold;
        public bool doubleJump, phase, parry, hook, jetBag, magBoots;
        public System.Action<Transform, U.Layers, string> build;
    }

    private static LevelDef[] Levels => new[]
    {
        new LevelDef
        {
            id = "L1", displayName = "Escape", sceneName = "Level_01", silver = 50f, gold = 38f,
            belgeTitle = "Document 1: The Dome", belgeText = "PLACEHOLDER: The company has claimed the last forests outside the dome cities.",
            build = BuildLevel1
        },
        new LevelDef
        {
            id = "L2", displayName = "Above the City", sceneName = "Level_02", silver = 55f, gold = 42f,
            belgeTitle = "Document 2: The Virus", belgeText = "PLACEHOLDER: The virus implanted in captured activists activates the moment they stop.",
            doubleJump = true, phase = true, parry = true,
            build = BuildLevel2
        },
        new LevelDef
        {
            id = "L3", displayName = "The Tower", sceneName = "Level_03", silver = 70f, gold = 52f,
            belgeTitle = "Document 3: The Tower", belgeText = "PLACEHOLDER: The last piece of the documents is hidden in the company's tower.",
            doubleJump = true, phase = true, parry = true, hook = true, jetBag = true, magBoots = true,
            build = BuildLevel3
        }
    };

    [MenuItem("KosKos/Build Prototype Levels")]
    private static void BuildAll()
    {
        LevelDef[] levels = Levels;
        WriteCatalog(GetOrCreateCatalog(), levels);

        foreach (LevelDef def in levels)
        {
            if (!BuildLevel(def)) return;
        }

        UpdateBuildSettings();
        Debug.Log("[KosKos] Prototip bölümler oluşturuldu.");
    }

    private static bool BuildLevel(LevelDef def)
    {
        if (!U.BeginScene(out Scene scene, out InputActionAsset actions, out U.Layers layers)) return false;
        // Yeni sahne açılınca Unity kullanılmayan asset'leri bellekten atar; katalog sahne açıldıktan sonra yüklenir
        LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);

        // BeginScene test loadout'u ile bir LevelSettings ekler; bu bölümün ayarlarıyla değiştir
        LevelSettings settings = Object.FindFirstObjectByType<LevelSettings>();
        var so = new SerializedObject(settings);
        so.FindProperty("loadout").objectReferenceValue = GetOrCreateLoadout(def);
        so.FindProperty("catalog").objectReferenceValue = catalog;
        so.FindProperty("levelId").stringValue = def.id;
        so.FindProperty("silverTime").floatValue = def.silver;
        so.FindProperty("goldTime").floatValue = def.gold;
        so.ApplyModifiedPropertiesWithoutUndo();
        settings.gameObject.AddComponent<LevelRun>();

        Transform level = new GameObject("Level").transform;
        def.build(level, layers, def.id);

        GameObject player = U.SpawnPlayer(layers, actions, new Vector3(0f, 1.05f, 0f));
        U.AddFollowCamera(player);

        var ui = new GameObject("LevelUI").AddComponent<LevelUI>();
        U.SetField(ui, "actions", actions);

        U.SaveScene(scene, $"Assets/Scenes/{def.sceneName}.unity");
        return true;
    }

    // --- Bölüm 1 "Kaçış": temel set (çift zıplama, sıyrılganlık, savuşturma yok) ---
    private static void BuildLevel1(Transform p, U.Layers l, string id)
    {
        int g = l.Ground;
        U.Ground(p, g, "Ground_A", -5f, 40f);
        U.SafeZoneAt(p, "SafeZone_Start", 0f, true);
        Kumas(p, id, 1, new Vector3(12f, 1.2f, 0f));
        U.Block(p, g, "LowWall", 20f, 0f, 1f, 1.5f);
        Kumas(p, id, 2, new Vector3(20.5f, 3.5f, 0f));

        // Boşluk 40-44 (tek zıplama)
        U.Ground(p, g, "Ground_B", 44f, 90f);
        U.SafeZoneAt(p, "SafeZone_1", 48f, false);
        B.BreakableBlock(p, g, "Breakable_Ground", 58f, 0f, 1f, 2f);
        Kumas(p, id, 3, new Vector3(70f, 1.2f, 0f));

        // Boşluk 90-97 (zıplama + havada atılma)
        U.Ground(p, g, "Ground_C", 97f, 170f);
        U.SafeZoneAt(p, "SafeZone_2", 102f, false);
        DT.LaunchPadAt(p, "LaunchPad", 112f);
        U.Block(p, g, "PadWall", 122f, 0f, 1f, 6.5f);
        // Belge: fırlatma + ek zıplamanın tepesinde, kolay yolun biraz dışında
        Belge(p, id, new Vector3(119.5f, 9.5f, 0f));
        DE.Turret(p, l, "Turret_Dodge", 140f, DE.TurretBullet(l));
        Kumas(p, id, 4, new Vector3(146f, 1.2f, 0f));
        B.BreakableBlock(p, g, "Breakable_TallWall", 162f, 0f, 1f, 6f);

        // Boşluk 170-175
        U.Ground(p, g, "Ground_D", 175f, 200f);
        U.SafeZoneAt(p, "SafeZone_3", 180f, false);
        B.BreakableBlock(p, g, "Breakable_Floating", 188f, 3f, 1f, 1f);
        Kumas(p, id, 5, new Vector3(204f, 3.5f, 0f));

        // Boşluk 200-208 (zıplama + havada atılma)
        U.Ground(p, g, "Ground_E", 208f, 245f);
        GoalAt(p, 238f);
        U.KillZone(p, 120f, 400f);
    }

    // --- Bölüm 2 "Şehir Üstü": + çift zıplama, sıyrılganlık, savuşturma ---
    private static void BuildLevel2(Transform p, U.Layers l, string id)
    {
        int g = l.Ground;
        U.Ground(p, g, "Ground_A", -5f, 60f);
        U.SafeZoneAt(p, "SafeZone_Start", 0f, true);
        Kumas(p, id, 1, new Vector3(10f, 1.2f, 0f));
        U.Block(p, g, "TallWall", 20f, 0f, 1f, 4.5f);
        Kumas(p, id, 2, new Vector3(20.5f, 6f, 0f));

        U.SafeZoneAt(p, "SafeZone_1", 32f, false);
        GameObject thinWall = U.Block(p, l.Phaseable, "ThinWall_Phase", 45f, 0f, 0.3f, 8f);
        U.Colorize(thinWall, ThinWallColor);
        Kumas(p, id, 3, new Vector3(47f, 1.2f, 0f));

        // Boşluk 60-70 (çift zıplama)
        U.Ground(p, g, "Ground_B", 70f, 100f);
        U.SafeZoneAt(p, "SafeZone_2", 75f, false);
        DE.Turret(p, l, "Turret_Parry", 88f, DE.TurretBullet(l));

        // Zipline: 100-138 arası boşluğun üstünde
        DT.ZiplineBetween(p, "Zipline", new Vector3(100f, 4f, 0f), new Vector3(140f, 3f, 0f));
        Kumas(p, id, 4, new Vector3(120f, 2.4f, 0f));
        U.Ground(p, g, "Ground_C", 138f, 250f);

        U.SafeZoneAt(p, "SafeZone_3", 145f, false);
        DT.SlingZoneAt(p, "SlingZone", new Vector3(160f, 4f, 0f));
        U.Block(p, g, "SlingWall", 168f, 0f, 1f, 8f);
        // Belge: sapanla yukarı nişan alınca ulaşılır
        Belge(p, id, new Vector3(176f, 10f, 0f));

        U.SafeZoneAt(p, "SafeZone_4", 180f, false);
        Bullet droneBullet = DE.DroneBullet(l);
        DE.Drone(p, l, "Drone_Low", 192f, 1.5f, droneBullet, 5f);
        DE.Drone(p, l, "Drone_High", 205f, 4f, droneBullet, 5f);
        Kumas(p, id, 5, new Vector3(212f, 1.2f, 0f));
        DE.Drone(p, l, "Drone_Phase", 228f, 4.5f, droneBullet, 12f);

        GoalAt(p, 242f);
        U.KillZone(p, 120f, 400f);
    }

    // --- Bölüm 3 "Kule": + kanca, jet-çanta, manyetik botlar ---
    private static void BuildLevel3(Transform p, U.Layers l, string id)
    {
        int g = l.Ground;
        // Kanca sallanma: 24-36 arası boşluk
        U.Ground(p, g, "Ground_A", -5f, 24f);
        U.SafeZoneAt(p, "SafeZone_Start", 0f, true);
        Kumas(p, id, 1, new Vector3(12f, 1.2f, 0f));
        C.HookPointAt(p, "HookPoint_Swing", new Vector3(30f, 9f, 0f));
        Kumas(p, id, 2, new Vector3(30f, 3f, 0f));

        // Kanca çekilme: 52-57 arası boşluk, karşıda çıkıntı
        U.Ground(p, g, "Ground_B", 36f, 52f);
        U.SafeZoneAt(p, "SafeZone_1", 42f, false);
        C.HookPointAt(p, "HookPoint_Pull", new Vector3(58f, 1f, 0f));
        U.Block(p, g, "PullOverhang", 56f, 2.2f, 6f, 6f);

        // Jet-çanta: 7 yüksek duvar, sonra 130-165 arası uzun boşluk (bataryalarla)
        U.Ground(p, g, "Ground_C", 57f, 130f);
        Kumas(p, id, 3, new Vector3(66f, 1.2f, 0f));
        U.SafeZoneAt(p, "SafeZone_2", 70f, false);
        U.Block(p, g, "JetWall", 90f, 0f, 1f, 7f);
        // Belge: duvarın arkasında yüksekte, jet-çanta ile ulaşılır
        Belge(p, id, new Vector3(100f, 12f, 0f));
        U.SafeZoneAt(p, "SafeZone_3", 115f, false);
        C.BatteryAt(p, "Battery_1", new Vector3(124f, 3f, 0f));
        C.BatteryAt(p, "Battery_2", new Vector3(148f, 5f, 0f));
        Kumas(p, id, 4, new Vector3(140f, 6f, 0f));

        // Manyetik bot, tavan: 185-215 arası metal tavan, altında 195-210 boşluk
        U.Ground(p, g, "Ground_D", 165f, 195f);
        U.SafeZoneAt(p, "SafeZone_4", 175f, false);
        GameObject ceiling = U.Block(p, g, "MetalCeiling", 185f, 5f, 30f, 1f);
        U.Colorize(ceiling, C.SteelColor);
        ceiling.AddComponent<MetalCeiling>();
        Kumas(p, id, 5, new Vector3(203f, 4f, 0f));

        // Manyetik bot, duvar: 230-260 arası boşluk
        U.Ground(p, g, "Ground_E", 210f, 230f);
        U.SafeZoneAt(p, "SafeZone_5", 222f, false);
        C.MetalWallBetween(p, "MetalWall", 228f, 262f, 0f, 7f);

        // Final: karışık kısa bölüm
        U.Ground(p, g, "Ground_F", 260f, 310f);
        U.SafeZoneAt(p, "SafeZone_6", 268f, false);
        B.BreakableBlock(p, g, "Breakable_Final", 280f, 0f, 1f, 2f);
        DE.Drone(p, l, "Drone_Final", 292f, 1.5f, DE.DroneBullet(l), 5f);
        GoalAt(p, 302f);
        U.KillZone(p, 150f, 420f);
    }

    // Mor, dönen para (silindir kameraya bakar)
    private static void Kumas(Transform parent, string levelId, int number, Vector3 position)
    {
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = $"Kumas_{number}";
        coin.transform.SetParent(parent);
        coin.transform.position = position;
        coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        coin.transform.localScale = new Vector3(0.6f, 0.08f, 0.6f);
        coin.GetComponent<Collider>().isTrigger = true;
        SetupCollectible(coin, Collectible.Kind.Kumas, $"{levelId}{GameProgress.KumasTag}{number}", KumasColor, 120f);
    }

    // Beyaz, havada duran sayfa
    private static void Belge(Transform parent, string levelId, Vector3 position)
    {
        GameObject page = GameObject.CreatePrimitive(PrimitiveType.Cube);
        page.name = "Belge";
        page.transform.SetParent(parent);
        page.transform.position = position;
        page.transform.localScale = new Vector3(0.6f, 0.8f, 0.05f);
        page.GetComponent<Collider>().isTrigger = true;
        SetupCollectible(page, Collectible.Kind.Belge, $"{levelId}{GameProgress.BelgeTag}1", BelgeColor, 0f);
    }

    private static void SetupCollectible(GameObject go, Collectible.Kind kind, string id, Color color, float spin)
    {
        var collectible = go.AddComponent<Collectible>();
        var so = new SerializedObject(collectible);
        so.FindProperty("kind").enumValueIndex = (int)kind;
        so.FindProperty("id").stringValue = id;
        so.FindProperty("color").colorValue = color;
        so.FindProperty("spinSpeed").floatValue = spin;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Bölüm sonu: yüksek görünmez tetik + yeşil direk ve sarı bayrak
    private static void GoalAt(Transform parent, float x)
    {
        var goal = new GameObject("LevelGoal");
        goal.transform.SetParent(parent);
        goal.transform.position = new Vector3(x, 10f, 0f);
        var trigger = goal.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(2f, 20f, U.LevelDepth);
        goal.AddComponent<LevelGoal>();

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pole.name = "Pole";
        Object.DestroyImmediate(pole.GetComponent<Collider>());
        pole.transform.SetParent(goal.transform);
        pole.transform.position = new Vector3(x, 3f, 0f);
        pole.transform.localScale = new Vector3(0.3f, 6f, 0.3f);
        U.Colorize(pole, GoalColor);

        GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flag.name = "Flag";
        Object.DestroyImmediate(flag.GetComponent<Collider>());
        flag.transform.SetParent(goal.transform);
        flag.transform.position = new Vector3(x + 0.8f, 5.4f, 0f);
        flag.transform.localScale = new Vector3(1.4f, 0.9f, 0.1f);
        U.Colorize(flag, FlagColor);
    }

    // Katalog asset'i varsa onu kullanır (GUID korunur, sahnelerdeki referanslar kopmaz)
    internal static LevelCatalog GetOrCreateCatalog()
    {
        var existing = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
        if (existing != null) return existing;

        U.EnsureFolder("Assets", "Levels");
        var catalog = ScriptableObject.CreateInstance<LevelCatalog>();
        AssetDatabase.CreateAsset(catalog, CatalogPath);
        return catalog;
    }

    private static void WriteCatalog(LevelCatalog catalog, LevelDef[] levels)
    {
        var so = new SerializedObject(catalog);
        SerializedProperty entries = so.FindProperty("entries");
        entries.arraySize = levels.Length;
        for (int i = 0; i < levels.Length; i++)
        {
            SerializedProperty e = entries.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("id").stringValue = levels[i].id;
            e.FindPropertyRelative("displayName").stringValue = levels[i].displayName;
            e.FindPropertyRelative("sceneName").stringValue = levels[i].sceneName;
            e.FindPropertyRelative("belgeTitle").stringValue = levels[i].belgeTitle;
            e.FindPropertyRelative("belgeText").stringValue = levels[i].belgeText;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    // Bölüm loadout'u: builder her seferinde tablodaki değerleri yazar
    private static AbilityLoadout GetOrCreateLoadout(LevelDef def)
    {
        string path = $"Assets/Loadouts/{def.id}_Loadout.asset";
        var loadout = AssetDatabase.LoadAssetAtPath<AbilityLoadout>(path);
        if (loadout == null)
        {
            U.EnsureFolder("Assets", "Loadouts");
            loadout = ScriptableObject.CreateInstance<AbilityLoadout>();
            AssetDatabase.CreateAsset(loadout, path);
        }

        var so = new SerializedObject(loadout);
        so.FindProperty("doubleJump").boolValue = def.doubleJump;
        so.FindProperty("phase").boolValue = def.phase;
        so.FindProperty("parry").boolValue = def.parry;
        so.FindProperty("hook").boolValue = def.hook;
        so.FindProperty("jetBag").boolValue = def.jetBag;
        so.FindProperty("magBoots").boolValue = def.magBoots;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(loadout);
        AssetDatabase.SaveAssets();
        return loadout;
    }

    // Build Settings: önce ana menü, sonra katalogdaki sıraya göre bölümler (sahne yükleme sadece bu listedekilerle çalışır)
    internal static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) != null)
        {
            scenes.Add(new EditorBuildSettingsScene(MenuScenePath, true));
        }

        LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
        if (catalog != null)
        {
            foreach (LevelCatalog.Entry entry in catalog.Entries)
            {
                string path = $"Assets/Scenes/{entry.sceneName}.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) scenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[KosKos] Build Settings güncellendi: {scenes.Count} sahne.");
    }
}
