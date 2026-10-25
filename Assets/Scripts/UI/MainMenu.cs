using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Ana menü (MainMenuUI prefab'ının kökü): Oyna (bölüm seçimi), Mağaza, Belgeler, Ayarlar, Yapımcılar, Çıkış.
// Ekranlar prefab'da hazır panellerdir; bu kod sadece paneller arasında geçiş yapar ve butonları bağlar.
// Düzen ve görünüm Assets/Prefabs/UI/MainMenuUI.prefab içinde düzenlenir.
public class MainMenu : MonoBehaviour
{
    [Header("Veri")]
    [Tooltip("Bölüm listesi (Assets/Levels/LevelCatalog).")]
    [SerializeField] private LevelCatalog catalog;

    [Tooltip("Oyuncu kontrollerini içeren Input Actions dosyası (KosKosControls). Tuş ayarları için.")]
    [SerializeField] private InputActionAsset actions;

    [Header("Paneller")]
    [Tooltip("Ana butonların bulunduğu panel.")]
    [SerializeField] private GameObject rootPanel;

    [Tooltip("Bölüm seçim paneli.")]
    [SerializeField] private LevelSelectView levelSelect;

    [Tooltip("Mağaza paneli.")]
    [SerializeField] private ShopView shop;

    [Tooltip("Belgeler paneli.")]
    [SerializeField] private LoreView documents;

    [Tooltip("Yapımcılar paneli.")]
    [SerializeField] private CreditsView credits;

    [Tooltip("Ayarlar paneli (SettingsPanel prefab'ı).")]
    [SerializeField] private SettingsView settings;

    [Tooltip("Hile panelinin kodla kurulacağı boş kap (sadece Editor/development build).")]
    [SerializeField] private RectTransform cheatsPanel;

    [Header("Ana Butonlar")]
    [Tooltip("Oyna butonu.")]
    [SerializeField] private Button playButton;

    [Tooltip("Mağaza butonu.")]
    [SerializeField] private Button shopButton;

    [Tooltip("Belgeler butonu.")]
    [SerializeField] private Button documentsButton;

    [Tooltip("Ayarlar butonu.")]
    [SerializeField] private Button settingsButton;

    [Tooltip("Yapımcılar butonu.")]
    [SerializeField] private Button creditsButton;

    [Tooltip("Çıkış butonu.")]
    [SerializeField] private Button quitButton;

    [Tooltip("Sol üstteki hile (hamburger) butonu; release build'de gizlenir.")]
    [SerializeField] private Button cheatsButton;

    [Tooltip("Başlık yazısı. Kodla kurulan hile paneli bu yazının yazı tipini kullanır.")]
    [SerializeField] private TMP_Text titleText;

    private void Awake()
    {
        // Bir bölümden duraklatılmış / sonuç ekranından gelinmiş olabilir
        SceneFlow.ResetTime();
        UiKit.EnsureEventSystem();
        // Ayarlar ekranı kayıtlı tuşları göstersin
        GameSettings.ApplyBindings(actions);
        if (titleText != null) UiKit.Font = titleText.font;

        playButton.onClick.AddListener(ShowLevels);
        shopButton.onClick.AddListener(ShowShop);
        documentsButton.onClick.AddListener(ShowDocuments);
        settingsButton.onClick.AddListener(ShowSettings);
        creditsButton.onClick.AddListener(ShowCredits);
        quitButton.onClick.AddListener(Quit);

        levelSelect.BackButton.onClick.AddListener(ShowRoot);
        shop.BackButton.onClick.AddListener(ShowRoot);
        documents.BackButton.onClick.AddListener(ShowRoot);
        credits.BackButton.onClick.AddListener(ShowRoot);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Test hileleri: sadece Editor ve development build'lerde
        cheatsButton.onClick.AddListener(ShowCheats);
#else
        cheatsButton.gameObject.SetActive(false);
#endif

        ShowRoot();
    }

    // Sadece verilen paneli açar
    private void Show(GameObject panel)
    {
        rootPanel.SetActive(panel == rootPanel);
        levelSelect.gameObject.SetActive(panel == levelSelect.gameObject);
        shop.gameObject.SetActive(panel == shop.gameObject);
        documents.gameObject.SetActive(panel == documents.gameObject);
        credits.gameObject.SetActive(panel == credits.gameObject);
        settings.gameObject.SetActive(panel == settings.gameObject);
        cheatsPanel.gameObject.SetActive(panel == cheatsPanel.gameObject);
    }

    private void ShowRoot()
    {
        Show(rootPanel);
        UiKit.Select(playButton);
    }

    private void ShowLevels()
    {
        Show(levelSelect.gameObject);
        UiKit.Select(levelSelect.Open(catalog));
    }

    private void ShowShop()
    {
        Show(shop.gameObject);
        UiKit.Select(shop.Open());
    }

    private void ShowDocuments()
    {
        Show(documents.gameObject);
        UiKit.Select(documents.Open(catalog));
    }

    private void ShowCredits()
    {
        Show(credits.gameObject);
        UiKit.Select(credits.Open());
    }

    private void ShowSettings()
    {
        Show(settings.gameObject);
        UiKit.Select(settings.Open(actions, ShowRoot));
    }

    private void ShowCheats()
    {
        Show(cheatsPanel.gameObject);
        UiKit.Clear(cheatsPanel);
        CheatsView.Build(cheatsPanel, catalog, ShowRoot);
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
