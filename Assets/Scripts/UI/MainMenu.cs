using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// GEÇİCİ ana menü: Oyna (bölüm seçimi), Mağaza, Belgeler, Ayarlar, Çıkış.
// Tüm ekranlar oyun çalışırken kodla oluşturulur; MainMenu sahnesinde bu bileşeni taşıyan tek bir nesne yeterli.
public class MainMenu : MonoBehaviour
{
    [Tooltip("Bölüm listesi (Assets/Levels/LevelCatalog).")]
    [SerializeField] private LevelCatalog catalog;

    [Tooltip("Oyuncu kontrollerini içeren Input Actions dosyası (KosKosControls). Tuş ayarları için.")]
    [SerializeField] private InputActionAsset actions;

    private static readonly Color BackgroundColor = new Color(0.08f, 0.1f, 0.12f, 1f);

    private RectTransform content;

    private void Awake()
    {
        // Bir bölümden duraklatılmış / sonuç ekranından gelinmiş olabilir
        SceneFlow.ResetTime();
        UiKit.EnsureEventSystem();
        // Ayarlar ekranı kayıtlı tuşları göstersin
        GameSettings.ApplyBindings(actions);

        Canvas canvas = UiKit.CreateCanvas("MenuCanvas", 0);
        canvas.transform.SetParent(transform, false);
        content = UiKit.Panel(canvas.transform, BackgroundColor, Vector2.zero, Vector2.one);
        ShowRoot();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Test hileleri: sadece Editor ve development build'lerde
        BuildCheatsButton(canvas.transform);
#endif
    }

    // Sol üst köşede üç çizgili (hamburger) buton
    private void BuildCheatsButton(Transform parent)
    {
        Button button = UiKit.Button(parent, "", ShowCheats);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -20f);
        rect.sizeDelta = new Vector2(64f, 64f);

        for (int i = 0; i < 3; i++)
        {
            RectTransform bar = UiKit.Panel(rect, UiKit.TextColor, new Vector2(0.2f, 0.28f + i * 0.2f), new Vector2(0.8f, 0.32f + i * 0.2f));
            bar.GetComponent<Image>().raycastTarget = false;
        }
    }

    private void ShowSettings()
    {
        UiKit.Clear(content);
        SettingsView.Build(content, actions, ShowRoot);
    }

    private void ShowCheats()
    {
        UiKit.Clear(content);
        CheatsView.Build(content, catalog, ShowRoot);
    }

    private void ShowRoot()
    {
        UiKit.Clear(content);
        RectTransform list = UiKit.VerticalList(content, 14f);
        UiKit.Line(list, "KOŞKOŞ", 96);
        Button play = UiKit.Button(list, "Play", ShowLevels);
        UiKit.Button(list, "Shop", ShowShop);
        UiKit.Button(list, "Documents", ShowLore);
        UiKit.Button(list, "Settings", ShowSettings);
        UiKit.Button(list, "Quit", Quit);
        UiKit.Select(play);
    }

    private void ShowLevels()
    {
        UiKit.Clear(content);
        LevelSelectView.Build(content, catalog, ShowRoot);
    }

    private void ShowShop()
    {
        UiKit.Clear(content);
        ShopView.Build(content, ShowRoot);
    }

    private void ShowLore()
    {
        UiKit.Clear(content);
        LoreView.Build(content, catalog, ShowRoot);
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
