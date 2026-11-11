using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Bölüm editörü penceresi: KosKos > Level Editor. Kod bilmeyen bölüm tasarımcısı için tek pencere:
// üstte bölüm seçici + New / Test / Save, ortada parça paleti (tıkla → sahneye koy), altta bölüm ayarları ve uyarılar.
// Pencere sadece bir formdur: veriler LevelDocument üzerinden zaten var olan yerlere yazılır.
public class LevelEditorWindow : EditorWindow
{
    private const int PaletteColumns = 4;
    private const float PaletteButtonHeight = 36f;

    private struct PaletteItem
    {
        public GameObject prefab;
        public Color swatch;
    }

    private LevelDocument doc;
    private List<LevelDocument.Message> messages = new List<LevelDocument.Message>();
    private readonly SortedDictionary<string, List<PaletteItem>> palette = new SortedDictionary<string, List<PaletteItem>>();
    private readonly Dictionary<string, bool> foldouts = new Dictionary<string, bool>();
    private Vector2 scroll;
    private int testZoneIndex;

    [MenuItem("KosKos/Level Editor")]
    private static void Open()
    {
        GetWindow<LevelEditorWindow>("Level Editor");
    }

    private void OnEnable()
    {
        EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.hierarchyChanged += Revalidate;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Reload();
    }

    private void OnDisable()
    {
        EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorApplication.hierarchyChanged -= Revalidate;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    // Play'den çıkınca form yeniden okunur (Play sırasında boş tutulur)
    private void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode) Reload();
    }

    // Seçili parça değişince "Selected piece" bölümü yenilensin
    private void OnSelectionChange() => Repaint();

    // Parça sahnede elle ölçeklenip döndürülürken de alanlar güncel kalsın (saniyede ~10 kez)
    private void OnInspectorUpdate() => Repaint();

    private void OnSceneChanged(Scene from, Scene to) => Reload();
    private void OnSceneOpened(Scene scene, OpenSceneMode mode) => Reload();

    private void Reload()
    {
        doc = EditorApplication.isPlayingOrWillChangePlaymode ? null : LevelDocument.FromActiveScene();
        testZoneIndex = 0;
        RefreshPalette();
        Revalidate();
    }

    private void Revalidate()
    {
        messages = doc != null && !EditorApplication.isPlaying ? doc.Validate(out _) : new List<LevelDocument.Message>();
        Repaint();
    }

    private void OnGUI()
    {
        if (EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Stop Play mode to edit the level.", MessageType.Info);
            return;
        }

        if (!AssetDatabase.IsValidFolder(LevelKitBuilder.KitRoot))
        {
            EditorGUILayout.HelpBox("The Level Kit (piece prefabs) has not been built yet.", MessageType.Warning);
            if (GUILayout.Button(new GUIContent("Build Level Kit", "Parça prefab'larını Assets/LevelKit içine üretir (bir kez)."), GUILayout.Height(30)))
            {
                LevelKitBuilder.BuildKit();
                Reload();
            }
            return;
        }

        DrawTopBar();

        if (doc == null)
        {
            EditorGUILayout.HelpBox("Not a level scene. Press '+ New Level' or pick a level above.", MessageType.Info);
            return;
        }

        DrawLevelHeader();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawPalette();
        EditorGUILayout.Space(8);
        EditorGUI.BeginChangeCheck();
        LevelPieceInspector.Draw();
        // Ör. başlangıç alanı değişince uyarılar güncellensin
        if (EditorGUI.EndChangeCheck()) Revalidate();
        EditorGUILayout.Space(8);
        DrawSettings();
        EditorGUILayout.Space(8);
        foreach (LevelDocument.Message m in messages)
        {
            EditorGUILayout.HelpBox(m.text, m.isError ? MessageType.Error : MessageType.Warning);
        }
        EditorGUILayout.EndScrollView();
    }

    // ---------------- Üst çubuk ----------------

    private void DrawTopBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(PrototypeLevelsBuilder.CatalogPath);
        var options = new List<string> { "Open level…" };
        if (catalog != null) options.AddRange(catalog.Entries.Select(e => $"{e.sceneName}  ({e.id} {e.displayName})"));
        int picked = EditorGUILayout.Popup(0, options.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(220));
        if (picked > 0) OpenLevel(catalog.Entries[picked - 1].sceneName);

        if (GUILayout.Button(new GUIContent("+ New Level", "Oynanabilir boş bir bölüm sahnesi açar (Save'e kadar oyuna eklenmez)."), EditorStyles.toolbarButton))
        {
            // Erteleme: sahne değişimi GUI çiziminin ortasında olmasın
            EditorApplication.delayCall += () =>
            {
                if (LevelSceneFactory.CreateNewLevelScene()) Reload();
            };
        }

        GUILayout.FlexibleSpace();

        using (new EditorGUI.DisabledScope(doc == null))
        {
            if (GUILayout.Button(new GUIContent("▶ Test", "Seçili güvenli alandan Play'e girer."), EditorStyles.toolbarButton)) Test();
            if (GUILayout.Button(new GUIContent("Save", "Bölümü kaydeder; yeni bölüm sıradaki Level_NN olarak oyuna eklenir."), EditorStyles.toolbarButton)) Save();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLevelHeader()
    {
        string title = doc.IsNew ? $"New level (will be saved as {doc.sceneName}, {doc.id})" : $"{doc.sceneName}  ({doc.id})";
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        if (doc.IsPrototype)
        {
            EditorGUILayout.HelpBox("Prototype level — 'Build Prototype Levels' will overwrite your edits.", MessageType.Warning);
        }

        SafeZone[] zones = Zones();
        if (zones.Length > 0)
        {
            testZoneIndex = Mathf.Clamp(testZoneIndex, 0, zones.Length - 1);
            testZoneIndex = EditorGUILayout.Popup(new GUIContent("Test from", "▶ Test'in başlayacağı güvenli alan."),
                testZoneIndex, zones.Select((z, i) =>
                    new GUIContent($"{i + 1}. {(IsStartZone(z) ? "Start" : "SafeZone")} at x {z.transform.position.x:0.#}")).ToArray());
        }

        if (messages.Any(m => m.text.Contains("Fix IDs")) &&
            GUILayout.Button(new GUIContent("Fix IDs", "Kopyalanan / kimliksiz toplanabilirlere yeni kimlik verir; asıllar değişmez.")))
        {
            int changed = doc.FixDuplicateIds();
            ShowNotification(new GUIContent($"{changed} pickup ID(s) fixed"));
            Revalidate();
        }
    }

    // ---------------- Palet ----------------

    private void RefreshPalette()
    {
        palette.Clear();
        if (!AssetDatabase.IsValidFolder(LevelKitBuilder.KitRoot)) return;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { LevelKitBuilder.KitRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string folder = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!palette.TryGetValue(folder, out List<PaletteItem> items)) palette[folder] = items = new List<PaletteItem>();
            items.Add(new PaletteItem { prefab = prefab, swatch = Swatch(prefab) });
        }
    }

    private void DrawPalette()
    {
        foreach (KeyValuePair<string, List<PaletteItem>> group in palette)
        {
            if (!foldouts.ContainsKey(group.Key)) foldouts[group.Key] = true;
            foldouts[group.Key] = EditorGUILayout.Foldout(foldouts[group.Key], group.Key, true, EditorStyles.foldoutHeader);
            if (!foldouts[group.Key]) continue;

            for (int i = 0; i < group.Value.Count; i += PaletteColumns)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = i; j < Mathf.Min(i + PaletteColumns, group.Value.Count); j++)
                {
                    PaletteItem item = group.Value[j];
                    var content = new GUIContent("   " + item.prefab.name, $"{item.prefab.name}: sahne görünümünün ortasına koyar.");
                    if (GUILayout.Button(content, GUILayout.Height(PaletteButtonHeight)))
                    {
                        LevelSceneFactory.Place(item.prefab);
                        Revalidate();
                    }
                    Rect r = GUILayoutUtility.GetLastRect();
                    EditorGUI.DrawRect(new Rect(r.x + 6, r.y + r.height * 0.5f - 6, 12, 12), item.swatch);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    // Düğmedeki renk: parçanın geçici renk bileşeni ya da toplanabilir rengi; yoksa gri
    private static Color Swatch(GameObject prefab)
    {
        var placeholder = prefab.GetComponentInChildren<PlaceholderColor>(true);
        if (placeholder != null) return new SerializedObject(placeholder).FindProperty("color").colorValue;
        var collectible = prefab.GetComponentInChildren<Collectible>(true);
        if (collectible != null) return new SerializedObject(collectible).FindProperty("color").colorValue;
        return Color.gray;
    }

    // ---------------- Ayarlar ----------------

    private void DrawSettings()
    {
        EditorGUILayout.LabelField("Level settings", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();

        doc.nameEn = EditorGUILayout.TextField(new GUIContent("Name (EN)", "Menüde görünen bölüm adı (İngilizce)."), doc.nameEn);
        doc.nameTr = EditorGUILayout.TextField(new GUIContent("Name (TR)", "Menüde görünen bölüm adı (Türkçe). Boşsa İngilizce gösterilir."), doc.nameTr);
        doc.silverTime = EditorGUILayout.FloatField(new GUIContent("Silver time (s)", "Gümüş madalya için en uzun süre (sn)."), doc.silverTime);
        doc.goldTime = EditorGUILayout.FloatField(new GUIContent("Gold time (s)", "Altın madalya için en uzun süre (sn); gümüşten kısa olmalı."), doc.goldTime);

        EditorGUILayout.LabelField(new GUIContent("Unlocks", "Bu bölümde oyuncunun sahip olduğu yetenekler."));
        EditorGUI.indentLevel++;
        doc.doubleJump = EditorGUILayout.Toggle(new GUIContent("Double jump", "Çift zıplama açık."), doc.doubleJump);
        doc.phase = EditorGUILayout.Toggle(new GUIContent("Phase (Sıyrılganlık)", "Atılmada ince duvar / mermi içinden geçme açık."), doc.phase);
        doc.parry = EditorGUILayout.Toggle(new GUIContent("Parry", "Kılıçla mermi savuşturma açık."), doc.parry);
        doc.hook = EditorGUILayout.Toggle(new GUIContent("Hook", "Kanca açık."), doc.hook);
        doc.jetBag = EditorGUILayout.Toggle(new GUIContent("Jet-Bag", "Jet-çanta açık."), doc.jetBag);
        doc.magBoots = EditorGUILayout.Toggle(new GUIContent("Mag boots", "Manyetik botlar açık."), doc.magBoots);
        EditorGUI.indentLevel--;
        if (doc.IsNew)
        {
            EditorGUILayout.HelpBox("Unlocks take effect in Test after the first Save (until then Test has everything unlocked).", MessageType.None);
        }

        doc.endDialogue = (DialogueData)EditorGUILayout.ObjectField(
            new GUIContent("End dialogue", "Hedefe varınca oynayan diyalog. Boş = diyalog yok."), doc.endDialogue, typeof(DialogueData), false);

        EditorGUILayout.LabelField(new GUIContent("Belge (document)", "Bu bölümün Belgesi toplanınca Belgeler ekranında görünen yazı."));
        EditorGUI.indentLevel++;
        doc.belgeTitleEn = EditorGUILayout.TextField(new GUIContent("Title (EN)", "Belge başlığı (İngilizce)."), doc.belgeTitleEn);
        doc.belgeTitleTr = EditorGUILayout.TextField(new GUIContent("Title (TR)", "Belge başlığı (Türkçe)."), doc.belgeTitleTr);
        EditorGUILayout.LabelField(new GUIContent("Text (EN)", "Belge metni (İngilizce)."));
        doc.belgeTextEn = EditorGUILayout.TextArea(doc.belgeTextEn ?? "", GUILayout.MinHeight(40));
        EditorGUILayout.LabelField(new GUIContent("Text (TR)", "Belge metni (Türkçe)."));
        doc.belgeTextTr = EditorGUILayout.TextArea(doc.belgeTextTr ?? "", GUILayout.MinHeight(40));
        EditorGUI.indentLevel--;

        if (EditorGUI.EndChangeCheck()) Revalidate();
    }

    // ---------------- Eylemler ----------------

    private void OpenLevel(string sceneName)
    {
        string path = $"Assets/Scenes/{sceneName}.unity";
        EditorApplication.delayCall += () =>
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Reload();
        };
    }

    private void Test()
    {
        doc.ApplyToScene();
        SafeZone[] zones = Zones();
        // Başlangıç alanı seçiliyse bir şey istenmez: oyun zaten oradan başlar
        if (testZoneIndex < zones.Length && !IsStartZone(zones[testZoneIndex]))
        {
            LevelEditorTestSpawn.RequestStartZone(zones[testZoneIndex].transform.position.x);
        }
        EditorApplication.EnterPlaymode();
    }

    private static bool IsStartZone(SafeZone zone) => new SerializedObject(zone).FindProperty("isStartZone").boolValue;

    private void Save()
    {
        if (doc.Save())
        {
            ShowNotification(new GUIContent($"Saved {doc.sceneName}"));
            Reload();
        }
        else
        {
            ShowNotification(new GUIContent("Fix the errors first"));
            Revalidate();
        }
    }

    // Sahnedeki güvenli alanlar, soldan sağa (ilki başlangıç alanı olur)
    private static SafeZone[] Zones()
    {
        return SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<SafeZone>(true))
            .OrderBy(z => z.transform.position.x).ToArray();
    }
}
