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

// Tek tıkla 3 prototip bölümü üretir: KosKos > Scenes > Build Prototype Levels.
// Bölüm sahneleri, her bölümün loadout'u ve bölüm kataloğu her seferinde yeniden yazılır; elle düzenlemeyin.
// Gerçek bölüm tasarımları gelince bu builder emekliye ayrılacak.
public static class PrototypeLevelsBuilder
{
    internal const string CatalogPath = "Assets/Levels/LevelCatalog.asset";
    internal const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

    // Bölüm sonu diyaloğu (Yunus'un diyalog sistemi): her bölüme C_Dialogue prefab'ı konur, hedefe diyalog verisi atanır
    private const string EndDialogueDataPath = LevelPieces.DefaultDialogueDataPath;

    // Bölüm kiti de (ince duvar parçası) aynı rengi kullanır
    internal static readonly Color ThinWallColor = new Color(0.2f, 0.9f, 1f);

    // Builder'ın kendi bölümleri; katalogda bunların dışındaki girişler (bölüm editörüyle yapılanlar) korunur
    private static readonly string[] PrototypeIds = { "L1", "L2", "L3" };

    // Bölüm tanımları: kimlik, ad, sahne, madalya süreleri, loadout (çift zıplama, sıyrılganlık, savuşturma, kanca, jet, bot)
    private struct LevelDef
    {
        public string id, displayName, sceneName, belgeTitle, belgeText;
        // Türkçe metinler (menü dil seçimine göre gösterilir)
        public string displayNameTr, belgeTitleTr, belgeTextTr;
        public float silver, gold;
        public bool doubleJump, phase, parry, hook, jetBag, magBoots;
        // Bölüm sonu diyalog verisi (boş = diyalog yok) ve diyalogdaki T tuşlu test tetikleyicisinin kalıp kalmayacağı
        public string endDialogue;
        public bool keepDialogueTestTrigger;
        public System.Action<Transform, U.Layers, string> build;
    }

    private static LevelDef[] Levels => new[]
    {
        new LevelDef
        {
            id = "L1", displayName = "Escape", sceneName = "Level_01", silver = 50f, gold = 38f,
            belgeTitle = "Document 1: The Dome", belgeText = "PLACEHOLDER: The company has claimed the last forests outside the dome cities.",
            displayNameTr = "Kaçış", belgeTitleTr = "Belge 1: Kubbe",
            belgeTextTr = "GEÇİCİ METİN: Şirket, kubbe şehirlerin dışındaki son ormanları kendi zimmetine geçirdi.",
            endDialogue = EndDialogueDataPath,
            build = BuildLevel1
        },
        new LevelDef
        {
            id = "L2", displayName = "Above the City", sceneName = "Level_02", silver = 55f, gold = 42f,
            belgeTitle = "Document 2: The Virus", belgeText = "PLACEHOLDER: The virus implanted in captured activists activates the moment they stop.",
            displayNameTr = "Şehir Üstü", belgeTitleTr = "Belge 2: Virüs",
            belgeTextTr = "GEÇİCİ METİN: Yakalanan aktivistlere eklenen virüs, durdukları anda harekete geçiyor.",
            doubleJump = true, phase = true, parry = true,
            endDialogue = EndDialogueDataPath, keepDialogueTestTrigger = true,
            build = BuildLevel2
        },
        new LevelDef
        {
            id = "L3", displayName = "The Tower", sceneName = "Level_03", silver = 70f, gold = 52f,
            belgeTitle = "Document 3: The Tower", belgeText = "PLACEHOLDER: The last piece of the documents is hidden in the company's tower.",
            displayNameTr = "Kule", belgeTitleTr = "Belge 3: Kule",
            belgeTextTr = "GEÇİCİ METİN: Dokümanların son parçası şirketin kulesinde saklanıyor.",
            doubleJump = true, phase = true, parry = true, hook = true, jetBag = true, magBoots = true,
            endDialogue = EndDialogueDataPath, keepDialogueTestTrigger = true,
            build = BuildLevel3
        }
    };

    [MenuItem("KosKos/Scenes/Build Prototype Levels")]
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
        // Bölümlerde gökyüzü yerine düz siyah arka plan (ortam ışığı yine gökyüzünden gelir)
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = Color.black;

        // Bölüm arayüzü: LevelUI prefab'ı (yoksa önce üretilir); düzeni prefab'da düzenlenir, tüm bölümlere yansır
        U.AddLevelUI(scene);
        AddEndDialogue(scene, def);

        U.SaveScene(scene, $"Assets/Scenes/{def.sceneName}.unity");
        return true;
    }

    // Bölüm sonu diyaloğu: C_Dialogue prefab'ı sahneye konur, LevelGoal'a diyalog verisi atanır
    // (hedefe varınca önce diyalog oynar, bitince sonuç ekranı açılır; bkz. LevelGoal)
    private static void AddEndDialogue(Scene scene, LevelDef def)
    {
        if (string.IsNullOrEmpty(def.endDialogue)) return;

        var data = AssetDatabase.LoadAssetAtPath<DialogueData>(def.endDialogue);
        if (data == null)
        {
            Debug.LogWarning($"[KosKos] {def.sceneName}: diyalog verisi bulunamadı ({def.endDialogue}); bölüm diyalogsuz kuruldu.");
            return;
        }
        // T tuşuyla diyalog başlatan test tetikleyicisi bölüm içinde istenmiyorsa kaldırılır
        if (LevelPieces.AddDialogueObject(scene, def.keepDialogueTestTrigger) == null) return;

        LevelGoal goal = Object.FindFirstObjectByType<LevelGoal>();
        var so = new SerializedObject(goal);
        so.FindProperty("dialogueData").objectReferenceValue = data;
        so.ApplyModifiedPropertiesWithoutUndo();
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
        // Bu bölümde çift zıplama yok: fırlatma alanı tek başına 4 yüksek çıkıntının üstüne çıkarır.
        // Alansız koşulursa çıkıntının önüne çarpılır (ölüm)
        DT.LaunchPadAt(p, "LaunchPad", 112f);
        U.Block(p, g, "PadLedge", 118f, 0f, 17f, 4f);
        // Belge: fırlatma yayının tepesinde
        Belge(p, id, new Vector3(117f, 7.5f, 0f));
        // İlk bölüm: yumuşak silah (uzun uyarı, yavaş mermi, geçince ateş yok); fırlatma alanı inişinden uzakta
        DE.Turret(p, l, "Turret_Dodge", 150f, DE.TurretBullet(l),
            windUp: 0.7f, aimLock: 0.35f, bulletSpeed: 14f, passTolerance: 0.3f);
        Kumas(p, id, 4, new Vector3(156f, 1.2f, 0f));
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
        // Bölümdeki ilk (ve tek) sapan alanı: nasıl kullanılacağını anlatan uzun ipucu
        DT.SlingZoneAt(p, "SlingZone", new Vector3(160f, 4f, 0f), tutorial: true);
        U.Block(p, g, "SlingWall", 168f, 0f, 1f, 8f);
        // Belge: sapanla yukarı nişan alınca ulaşılır
        Belge(p, id, new Vector3(176f, 10f, 0f));

        U.SafeZoneAt(p, "SafeZone_4", 180f, false);
        Bullet droneBullet = DE.DroneBullet(l);
        DE.Drone(p, l, "Drone_Hover", 192f, 4.5f, droneBullet, 10f);
        // Devriye: 200 ile 208 arasında gidip gelir
        DE.Drone(p, l, "Drone_Patrol", 200f, 5f, droneBullet, 10f, 8f);
        Kumas(p, id, 5, new Vector3(212f, 1.2f, 0f));
        DE.Drone(p, l, "Drone_Phase", 228f, 5f, droneBullet, 12f);

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
        DE.Drone(p, l, "Drone_Final", 292f, 4.5f, DE.DroneBullet(l), 10f);
        GoalAt(p, 302f);
        U.KillZone(p, 150f, 420f);
    }

    // Parça yapıcıları LevelPieces'ta (bölüm editörüyle ortak); kimlik biçimi kayıt dosyasıyla aynı kalır
    private static void Kumas(Transform parent, string levelId, int number, Vector3 position)
    {
        LevelPieces.Kumas(parent, $"{levelId}{GameProgress.KumasTag}{number}", position, $"Kumas_{number}");
    }

    private static void Belge(Transform parent, string levelId, Vector3 position)
    {
        LevelPieces.Belge(parent, $"{levelId}{GameProgress.BelgeTag}1", position);
    }

    private static void GoalAt(Transform parent, float x)
    {
        LevelPieces.Goal(parent, x);
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

    // Önce prototip bölümler (L1-L3) yazılır, ardından katalogda zaten olan diğer bölümler (bölüm editörüyle yapılanlar) aynı sırayla korunur
    private static void WriteCatalog(LevelCatalog catalog, LevelDef[] levels)
    {
        var kept = new List<LevelCatalog.Entry>();
        foreach (LevelCatalog.Entry entry in catalog.Entries)
        {
            if (System.Array.IndexOf(PrototypeIds, entry.id) >= 0) continue;
            // Değerler kopyalanır: aşağıda dizi yeniden yazılınca asıl nesneler değişebilir
            kept.Add(new LevelCatalog.Entry
            {
                id = entry.id, displayName = entry.displayName, sceneName = entry.sceneName,
                belgeTitle = entry.belgeTitle, belgeText = entry.belgeText,
                displayNameTr = entry.displayNameTr, belgeTitleTr = entry.belgeTitleTr, belgeTextTr = entry.belgeTextTr
            });
        }

        var so = new SerializedObject(catalog);
        SerializedProperty entries = so.FindProperty("entries");
        entries.arraySize = levels.Length + kept.Count;
        for (int i = 0; i < levels.Length; i++)
        {
            LevelDef d = levels[i];
            WriteEntry(entries.GetArrayElementAtIndex(i), d.id, d.displayName, d.sceneName, d.belgeTitle, d.belgeText,
                d.displayNameTr, d.belgeTitleTr, d.belgeTextTr);
        }
        for (int i = 0; i < kept.Count; i++)
        {
            LevelCatalog.Entry k = kept[i];
            WriteEntry(entries.GetArrayElementAtIndex(levels.Length + i), k.id, k.displayName, k.sceneName, k.belgeTitle,
                k.belgeText, k.displayNameTr, k.belgeTitleTr, k.belgeTextTr);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    // Katalog girişinin alanlarını yazar (bölüm editörü de kullanır)
    internal static void WriteEntry(SerializedProperty e, string id, string displayName, string sceneName, string belgeTitle,
        string belgeText, string displayNameTr, string belgeTitleTr, string belgeTextTr)
    {
        e.FindPropertyRelative("id").stringValue = id;
        e.FindPropertyRelative("displayName").stringValue = displayName;
        e.FindPropertyRelative("sceneName").stringValue = sceneName;
        e.FindPropertyRelative("belgeTitle").stringValue = belgeTitle;
        e.FindPropertyRelative("belgeText").stringValue = belgeText;
        e.FindPropertyRelative("displayNameTr").stringValue = displayNameTr;
        e.FindPropertyRelative("belgeTitleTr").stringValue = belgeTitleTr;
        e.FindPropertyRelative("belgeTextTr").stringValue = belgeTextTr;
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
        // Değişiklik diske yazılsın; yoksa commit'te eski liste kalabilir
        AssetDatabase.SaveAssets();
        Debug.Log($"[KosKos] Build Settings güncellendi: {scenes.Count} sahne.");
    }
}
