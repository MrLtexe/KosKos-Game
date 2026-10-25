using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Arayüz prefab'larının İLK sürümünü üretir: KosKos > Build UI Prefabs.
// SettingsPanel, MainMenuUI ve LevelUI (ikisi de SettingsPanel'i içerir). Var olan prefab'ların üzerine YAZMAZ
// (menüden çalıştırınca sorar; sahne builder'ları hiç sormadan sadece eksikleri üretir). Üretildikten sonra prefab'lar ekibindir:
// düzen, renk, yazı tipi Unity'de düzenlenir. Prefab'lar geçici bir önizleme sahnesinde kurulur ve kaydedilir
// (yeni nesneler önce açık sahnede doğup oraya taşındığı için açık sahne "değişti" görünebilir; içinde bir şey kalmaz).
public static class UiPrefabBuilder
{
    private const string Folder = "Assets/Prefabs/UI";
    internal const string SettingsPath = Folder + "/SettingsPanel.prefab";
    internal const string MainMenuPath = Folder + "/MainMenuUI.prefab";
    internal const string LevelUiPath = Folder + "/LevelUI.prefab";

    private const float ButtonHeight = 56f;
    private const float SmallButtonHeight = 44f;
    private const float ListWidth = 900f;
    private static readonly Color HudColor = new Color(0f, 0f, 0f, 0.45f);
    private static readonly Color MessageColor = new Color(1f, 0.6f, 0.4f);
    private static readonly Color RoleColor = new Color(0.8f, 0.82f, 0.85f);

    // Prefab'lar bu geçici sahnede kurulur
    private static Scene previewScene;

    [MenuItem("KosKos/Build UI Prefabs")]
    private static void BuildFromMenu()
    {
        Build(true);
    }

    // Sahne builder'ları çağırır: sadece eksik prefab'ları üretir, var olanlara dokunmaz
    internal static void EnsurePrefabs()
    {
        Build(false);
    }

    private static void Build(bool askBeforeOverwrite)
    {
        TestSceneBuilderUtils.EnsureFolder("Assets", "Prefabs");
        TestSceneBuilderUtils.EnsureFolder("Assets/Prefabs", "UI");

        previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            bool settingsExisted = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPath) != null;
            bool rebuildSettings = ShouldBuild(SettingsPath, askBeforeOverwrite,
                "\n\nDİKKAT: MainMenuUI ve LevelUI bu paneli içerir; yeniden oluşturulursa onlar da yeniden oluşturulur (bağlantıları kopmasın diye).");
            if (rebuildSettings) Save(BuildSettingsPanel(), SettingsPath);

            // Var olan SettingsPanel yeniden oluşturulduysa içindeki iç içe kopyaların bağlantısı kopar: diğer ikisi de baştan kurulur
            bool forceOthers = rebuildSettings && settingsExisted;
            if (forceOthers || ShouldBuild(MainMenuPath, askBeforeOverwrite)) Save(BuildMainMenu(), MainMenuPath);
            if (forceOthers || ShouldBuild(LevelUiPath, askBeforeOverwrite)) Save(BuildLevelUI(), LevelUiPath);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
        AssetDatabase.SaveAssets();
    }

    private static bool ShouldBuild(string path, bool ask, string extraWarning = "")
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return true;
        if (!ask) return false;
        return EditorUtility.DisplayDialog("UI prefab'ı zaten var",
            $"{path} zaten var.\n\nYeniden oluşturulursa bu prefab üzerindeki tüm düzenlemeler kaybolur.{extraWarning}",
            "Yeniden oluştur", "Benimkini koru");
    }

    private static void Save(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[KosKos] UI prefab'ı oluşturuldu: {path}");
    }

    // ---------------- SettingsPanel ----------------

    private static GameObject BuildSettingsPanel()
    {
        RectTransform root = NewRoot("SettingsPanel");
        var view = root.gameObject.AddComponent<SettingsView>();

        RectTransform list = VList(root, "List", 6f, ListWidth);
        Text(list, "Title", 48, TextAlignmentOptions.Center, "settings.title");
        Button language = MakeButton(list, "LanguageButton", "settings.language", SmallButtonHeight);
        Text(list, "VolumeLabel", 26, TextAlignmentOptions.Center, "settings.volume");
        Slider volume = MakeSlider(list);
        Button fullscreen = MakeButton(list, "FullscreenButton", null, SmallButtonHeight);
        Button quality = MakeButton(list, "QualityButton", null, SmallButtonHeight);
        Text(list, "ControlsLabel", 30, TextAlignmentOptions.Center, "settings.controls");
        UiRowList keyRows = RowList(list, "KeyRows", 6f, row => MakeButton(row, "KeyRow", null, SmallButtonHeight).gameObject);
        Button resetControls = MakeButton(list, "ResetControlsButton", "settings.resetControls", SmallButtonHeight);
        TMP_Text message = Text(list, "Message", 24, TextAlignmentOptions.Center, null, 38f);
        message.color = MessageColor;
        Button resetProgress = MakeButton(list, "ResetProgressButton", null, SmallButtonHeight);
        Button back = MakeButton(list, "BackButton", "common.back", SmallButtonHeight);

        Wire(view, so =>
        {
            Set(so, "languageButton", language);
            Set(so, "volumeSlider", volume);
            Set(so, "fullscreenButton", fullscreen);
            Set(so, "qualityButton", quality);
            Set(so, "keyRows", keyRows);
            Set(so, "resetControlsButton", resetControls);
            Set(so, "messageText", message);
            Set(so, "resetProgressButton", resetProgress);
            Set(so, "backButton", back);
        });
        return root.gameObject;
    }

    // ---------------- MainMenuUI ----------------

    private static GameObject BuildMainMenu()
    {
        GameObject root = NewCanvas("MainMenuUI", 0);
        var menu = root.AddComponent<MainMenu>();

        Panel(root.transform, "Background", UiKit.BackgroundColor);

        // Ana butonlar
        RectTransform rootPanel = Stretch(NewRect("RootPanel", root.transform));
        RectTransform rootList = VList(rootPanel, "List", 14f, ListWidth);
        TMP_Text title = Text(rootList, "Title", 96, TextAlignmentOptions.Center, null, 120f);
        title.text = "Strider Bound";
        Button play = MakeButton(rootList, "PlayButton", "menu.play");
        Button shopButton = MakeButton(rootList, "ShopButton", "menu.shop");
        Button documentsButton = MakeButton(rootList, "DocumentsButton", "menu.documents");
        Button settingsButton = MakeButton(rootList, "SettingsButton", "menu.settings");
        Button creditsButton = MakeButton(rootList, "CreditsButton", "menu.credits");
        Button quit = MakeButton(rootList, "QuitButton", "menu.quit");

        // Bölüm seçimi
        RectTransform levelPanel = HiddenPanel(root.transform, "LevelSelectPanel");
        var levelSelect = levelPanel.gameObject.AddComponent<LevelSelectView>();
        RectTransform levelList = VList(levelPanel, "List", 10f, 1300f);
        Text(levelList, "Title", 56, TextAlignmentOptions.Center, "levels.title");
        UiRowList levelRows = RowList(levelList, "Rows", 10f, row => MakeButton(row, "LevelRow", null).gameObject);
        Button levelBack = MakeButton(levelList, "BackButton", "common.back");
        Wire(levelSelect, so =>
        {
            Set(so, "rows", levelRows);
            Set(so, "backButton", levelBack);
        });

        // Mağaza
        RectTransform shopPanel = HiddenPanel(root.transform, "ShopPanel");
        var shop = shopPanel.gameObject.AddComponent<ShopView>();
        RectTransform shopList = VList(shopPanel, "List", 10f, ListWidth);
        Text(shopList, "Title", 56, TextAlignmentOptions.Center, "shop.title");
        TMP_Text balance = Text(shopList, "Balance", 34, TextAlignmentOptions.Center);
        UiRowList shopRows = RowList(shopList, "Rows", 10f, row => MakeButton(row, "CostumeRow", null).gameObject);
        Button shopBack = MakeButton(shopList, "BackButton", "common.back");
        Wire(shop, so =>
        {
            Set(so, "balanceText", balance);
            Set(so, "rows", shopRows);
            Set(so, "backButton", shopBack);
        });

        // Belgeler: her satırda başlık ve metin
        RectTransform documentsPanel = HiddenPanel(root.transform, "DocumentsPanel");
        var documents = documentsPanel.gameObject.AddComponent<LoreView>();
        RectTransform documentsList = VList(documentsPanel, "List", 10f, ListWidth);
        Text(documentsList, "Title", 56, TextAlignmentOptions.Center, "lore.title");
        UiRowList documentRows = RowList(documentsList, "Rows", 12f, row =>
        {
            RectTransform entry = NewRect("DocumentRow", row);
            AddVerticalLayout(entry.gameObject, 4f);
            Text(entry, "Title", 34, TextAlignmentOptions.Center);
            TMP_Text body = Text(entry, "Body", 26, TextAlignmentOptions.Top, null, -1f);
            body.color = RoleColor;
            return entry.gameObject;
        });
        Button documentsBack = MakeButton(documentsList, "BackButton", "common.back");
        Wire(documents, so =>
        {
            Set(so, "rows", documentRows);
            Set(so, "backButton", documentsBack);
        });

        // Yapımcılar: solda rol (sağa yaslı, küçük, soluk), sağda isim
        RectTransform creditsPanel = HiddenPanel(root.transform, "CreditsPanel");
        var credits = creditsPanel.gameObject.AddComponent<CreditsView>();
        RectTransform creditsList = VList(creditsPanel, "List", 4f, 1500f);
        Text(creditsList, "Title", 56, TextAlignmentOptions.Center, "credits.title", 90f);
        UiRowList creditRows = RowList(creditsList, "Rows", 4f, row =>
        {
            RectTransform line = NewRect("CreditRow", row);
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = 76f;
            var layout = line.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 80f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            TMP_Text role = Text(line, "Role", 28, TextAlignmentOptions.MidlineRight, null, -1f);
            role.color = RoleColor;
            EqualColumn(role);
            EqualColumn(Text(line, "Name", 40, TextAlignmentOptions.MidlineLeft, null, -1f));
            return line.gameObject;
        });
        Button creditsBack = MakeButton(creditsList, "BackButton", "common.back");
        Wire(credits, so =>
        {
            Set(so, "rows", creditRows);
            Set(so, "backButton", creditsBack);
        });

        // Ayarlar (iç içe SettingsPanel prefab'ı) ve kodla kurulan hile paneli için boş kap
        SettingsView settings = NestSettings(root.transform);
        RectTransform cheatsPanel = HiddenPanel(root.transform, "CheatsPanel");

        // Sol üstte hamburger (hile) butonu
        Button cheats = MakeButton(root.transform, "CheatsButton", null);
        var cheatsRect = (RectTransform)cheats.transform;
        cheatsRect.anchorMin = cheatsRect.anchorMax = new Vector2(0f, 1f);
        cheatsRect.pivot = new Vector2(0f, 1f);
        cheatsRect.anchoredPosition = new Vector2(20f, -20f);
        cheatsRect.sizeDelta = new Vector2(64f, 64f);
        Object.DestroyImmediate(cheats.GetComponentInChildren<TMP_Text>().gameObject);
        for (int i = 0; i < 3; i++)
        {
            RectTransform bar = NewRect($"Bar{i + 1}", cheatsRect);
            bar.anchorMin = new Vector2(0.2f, 0.28f + i * 0.2f);
            bar.anchorMax = new Vector2(0.8f, 0.32f + i * 0.2f);
            bar.offsetMin = bar.offsetMax = Vector2.zero;
            bar.gameObject.AddComponent<Image>().raycastTarget = false;
        }

        Wire(menu, so =>
        {
            Set(so, "catalog", PrototypeLevelsBuilder.GetOrCreateCatalog());
            Set(so, "actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(TestSceneBuilderUtils.InputAssetPath));
            Set(so, "rootPanel", rootPanel.gameObject);
            Set(so, "levelSelect", levelSelect);
            Set(so, "shop", shop);
            Set(so, "documents", documents);
            Set(so, "credits", credits);
            Set(so, "settings", settings);
            Set(so, "cheatsPanel", cheatsPanel);
            Set(so, "playButton", play);
            Set(so, "shopButton", shopButton);
            Set(so, "documentsButton", documentsButton);
            Set(so, "settingsButton", settingsButton);
            Set(so, "creditsButton", creditsButton);
            Set(so, "quitButton", quit);
            Set(so, "cheatsButton", cheats);
            Set(so, "titleText", title);
        });
        return root;
    }

    // ---------------- LevelUI ----------------

    private static GameObject BuildLevelUI()
    {
        GameObject root = NewCanvas("LevelUI", 10);
        // LevelUI; LevelHud, PauseMenu ve ResultsScreen'i de ekler (RequireComponent)
        var levelUi = root.AddComponent<LevelUI>();
        var hud = root.GetComponent<LevelHud>();
        var pause = root.GetComponent<PauseMenu>();
        var results = root.GetComponent<ResultsScreen>();

        // Gösterge (sol üst)
        RectTransform hudBox = NewRect("Hud", root.transform);
        hudBox.anchorMin = hudBox.anchorMax = hudBox.pivot = new Vector2(0f, 1f);
        hudBox.anchoredPosition = new Vector2(20f, -20f);
        hudBox.sizeDelta = new Vector2(330f, 170f);
        hudBox.gameObject.AddComponent<Image>().color = HudColor;
        var hudLayout = AddVerticalLayout(hudBox.gameObject, 2f);
        hudLayout.padding = new RectOffset(16, 16, 10, 10);
        hudLayout.childForceExpandHeight = true;
        TMP_Text time = Text(hudBox, "Time", 30, TextAlignmentOptions.MidlineLeft, null, -1f);
        time.text = "00:00.0";
        TMP_Text deaths = Text(hudBox, "Deaths", 30, TextAlignmentOptions.MidlineLeft, null, -1f);
        TMP_Text fabric = Text(hudBox, "Fabric", 30, TextAlignmentOptions.MidlineLeft, null, -1f);
        TMP_Text document = Text(hudBox, "Document", 30, TextAlignmentOptions.MidlineLeft, null, -1f);
        Wire(hud, so =>
        {
            Set(so, "root", hudBox.gameObject);
            Set(so, "timeText", time);
            Set(so, "deathsText", deaths);
            Set(so, "fabricText", fabric);
            Set(so, "documentText", document);
        });

        // Duraklatma
        RectTransform pausePanel = Panel(root.transform, "PausePanel", UiKit.PanelColor);
        RectTransform mainGroup = Stretch(NewRect("MainGroup", pausePanel));
        RectTransform pauseList = VList(mainGroup, "List", 12f, ListWidth);
        Text(pauseList, "Title", 56, TextAlignmentOptions.Center, "pause.title");
        Button resume = MakeButton(pauseList, "ResumeButton", "pause.resume");
        Button restart = MakeButton(pauseList, "RestartButton", "pause.restart");
        Button pauseSettings = MakeButton(pauseList, "SettingsButton", "menu.settings");
        Button pauseMenu = MakeButton(pauseList, "MainMenuButton", "pause.mainMenu");
        SettingsView settings = NestSettings(pausePanel);
        pausePanel.gameObject.SetActive(false);
        Wire(pause, so =>
        {
            Set(so, "panel", pausePanel.gameObject);
            Set(so, "mainGroup", mainGroup.gameObject);
            Set(so, "settings", settings);
            Set(so, "resumeButton", resume);
            Set(so, "restartButton", restart);
            Set(so, "settingsButton", pauseSettings);
            Set(so, "mainMenuButton", pauseMenu);
        });

        // Sonuç ekranı
        RectTransform resultsPanel = Panel(root.transform, "ResultsPanel", UiKit.PanelColor);
        RectTransform resultsList = VList(resultsPanel, "List", 10f, ListWidth);
        Text(resultsList, "Title", 56, TextAlignmentOptions.Center, "results.title");
        TMP_Text resultTime = Text(resultsList, "Time", 32, TextAlignmentOptions.Center);
        TMP_Text resultDeaths = Text(resultsList, "Deaths", 32, TextAlignmentOptions.Center);
        TMP_Text collectibles = Text(resultsList, "Collectibles", 32, TextAlignmentOptions.Center);
        TMP_Text medal = Text(resultsList, "Medal", 40, TextAlignmentOptions.Center);
        TMP_Text extra = Text(resultsList, "Extra", 30, TextAlignmentOptions.Center);
        Button next = MakeButton(resultsList, "NextButton", "results.next");
        Button replay = MakeButton(resultsList, "ReplayButton", "results.replay");
        Button resultsMenu = MakeButton(resultsList, "MainMenuButton", "pause.mainMenu");
        resultsPanel.gameObject.SetActive(false);
        Wire(results, so =>
        {
            Set(so, "panel", resultsPanel.gameObject);
            Set(so, "timeText", resultTime);
            Set(so, "deathsText", resultDeaths);
            Set(so, "collectiblesText", collectibles);
            Set(so, "medalText", medal);
            Set(so, "extraText", extra);
            Set(so, "nextButton", next);
            Set(so, "replayButton", replay);
            Set(so, "mainMenuButton", resultsMenu);
        });

        Wire(levelUi, so =>
            Set(so, "actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(TestSceneBuilderUtils.InputAssetPath)));
        return root;
    }

    // ---------------- Yardımcılar ----------------

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    // Prefab kökü (canvas'sız; iç içe kullanılan SettingsPanel gibi)
    private static RectTransform NewRoot(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(go, previewScene);
        return Stretch((RectTransform)go.transform);
    }

    private static GameObject NewCanvas(string name, int sortOrder)
    {
        var go = new GameObject(name, typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(go, previewScene);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static RectTransform Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    // Tam ekran renkli panel
    private static RectTransform Panel(Transform parent, string name, Color color)
    {
        RectTransform rect = Stretch(NewRect(name, parent));
        rect.gameObject.AddComponent<Image>().color = color;
        return rect;
    }

    // Tam ekran, arka plansız, başta kapalı panel
    private static RectTransform HiddenPanel(Transform parent, string name)
    {
        RectTransform rect = Stretch(NewRect(name, parent));
        rect.gameObject.SetActive(false);
        return rect;
    }

    private static VerticalLayoutGroup AddVerticalLayout(GameObject go, float spacing)
    {
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    // Ortada, içindekileri alt alta dizen liste (yüksekliği içeriğe göre)
    private static RectTransform VList(Transform parent, string name, float spacing, float width)
    {
        RectTransform rect = NewRect(name, parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, 0f);
        AddVerticalLayout(rect.gameObject, spacing);
        rect.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rect;
    }

    // Satır listesi: şablonu buildRow üretir; şablon kapalı tutulur
    private static UiRowList RowList(Transform parent, string name, float spacing, Func<Transform, GameObject> buildRow)
    {
        RectTransform rect = NewRect(name, parent);
        AddVerticalLayout(rect.gameObject, spacing);
        var list = rect.gameObject.AddComponent<UiRowList>();
        GameObject template = buildRow(rect);
        template.name = template.name + "_Template";
        template.SetActive(false);
        Wire(list, so => Set(so, "template", template));
        return list;
    }

    // TextMeshPro yazısı. key verilirse LocalizedText eklenir. height: >0 sabit yükseklik, 0 = boyuta göre, <0 = yerleşim belirler
    private static TMP_Text Text(Transform parent, string name, int size, TextAlignmentOptions alignment,
        string key = null, float height = 0f)
    {
        RectTransform rect = NewRect(name, parent);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = UiKit.TextColor;
        text.raycastTarget = false;
        if (height >= 0f) rect.gameObject.AddComponent<LayoutElement>().preferredHeight = height > 0f ? height : size + 14f;

        if (!string.IsNullOrEmpty(key))
        {
            text.text = Loc.Get(key);
            var localized = rect.gameObject.AddComponent<LocalizedText>();
            Wire(localized, so => so.FindProperty("key").stringValue = key);
        }
        return text;
    }

    // Tam genişlikli buton; labelKey verilirse yazısı LocalizedText ile, yoksa kodla doldurulur
    private static Button MakeButton(Transform parent, string name, string labelKey, float height = ButtonHeight)
    {
        RectTransform rect = NewRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = UiKit.ButtonColor;
        colors.highlightedColor = UiKit.ButtonHighlight;
        colors.selectedColor = UiKit.ButtonHighlight;
        colors.pressedColor = UiKit.ButtonHighlight * 0.8f;
        colors.disabledColor = UiKit.ButtonDisabled;
        button.colors = colors;
        rect.gameObject.AddComponent<LayoutElement>().preferredHeight = height;

        TMP_Text label = Text(rect, "Label", height < ButtonHeight ? 24 : 28, TextAlignmentOptions.Center, labelKey, -1f);
        Stretch((RectTransform)label.transform);
        return button;
    }

    private static Slider MakeSlider(Transform parent)
    {
        GameObject go = DefaultControls.CreateSlider(new DefaultControls.Resources());
        go.name = "VolumeSlider";
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = 36f;
        var slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    // Satırdaki iki sütunun eşit genişlikte olması için (yazı uzunluğu genişliği etkilemesin)
    private static void EqualColumn(TMP_Text text)
    {
        var element = text.gameObject.AddComponent<LayoutElement>();
        element.minWidth = 0f;
        element.preferredWidth = 0f;
        element.flexibleWidth = 1f;
    }

    // SettingsPanel prefab'ını iç içe yerleştirir (tam ekran, başta kapalı)
    private static SettingsView NestSettings(Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Stretch((RectTransform)instance.transform);
        instance.SetActive(false);
        return instance.GetComponent<SettingsView>();
    }

    private static void Wire(Object target, Action<SerializedObject> assign)
    {
        var so = new SerializedObject(target);
        assign(so);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(SerializedObject so, string field, Object value)
    {
        SerializedProperty property = so.FindProperty(field);
        if (property == null)
        {
            Debug.LogError($"[KosKos] UiPrefabBuilder: '{so.targetObject.GetType().Name}.{field}' alanı bulunamadı.");
            return;
        }
        property.objectReferenceValue = value;
    }
}
