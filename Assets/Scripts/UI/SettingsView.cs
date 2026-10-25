using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Ayarlar paneli (SettingsPanel prefab'ı; ana menü ve duraklatma menüsü ortak kullanır):
// dil, ses, tam ekran, grafik kalitesi, tuş atamaları, ilerlemeyi sıfırlama.
// Açan ekran Open(tuş dosyası, geri) çağırır. Düzen ve görünüm prefab'da düzenlenir; buradaki kod sadece değerleri doldurur.
public class SettingsView : MonoBehaviour
{
    [Tooltip("Dil butonu (her basışta sıradaki dile geçer). Yazısı LocalizedText ile 'settings.language'.")]
    [SerializeField] private Button languageButton;

    [Tooltip("Ses kaydırıcısı (0-1).")]
    [SerializeField] private Slider volumeSlider;

    [Tooltip("Tam ekran aç/kapat butonu.")]
    [SerializeField] private Button fullscreenButton;

    [Tooltip("Grafik kalitesi butonu (her basışta sıradaki seviye).")]
    [SerializeField] private Button qualityButton;

    [Tooltip("Tuş atama satırlarının listesi (şablonu yazılı bir buton).")]
    [SerializeField] private UiRowList keyRows;

    [Tooltip("Tuşları varsayılana döndürme butonu.")]
    [SerializeField] private Button resetControlsButton;

    [Tooltip("Tuş çakışması gibi mesajların yazısı.")]
    [SerializeField] private TMP_Text messageText;

    [Tooltip("İlerlemeyi sıfırlama butonu (iki tıklama ister).")]
    [SerializeField] private Button resetProgressButton;

    [Tooltip("Geri butonu (açan ekrana döner).")]
    [SerializeField] private Button backButton;

    private InputActionAsset asset;
    // İlerlemeyi sıfırlama ikinci tıklamayı bekliyor
    private bool confirmingReset;

    private void Awake()
    {
        languageButton.onClick.AddListener(() =>
        {
            int next = ((int)GameSettings.Language + 1) % System.Enum.GetValues(typeof(Language)).Length;
            // Dil değişince LanguageChanged ile tüm yazılar (bu panel dahil) yenilenir
            GameSettings.SetLanguage((Language)next);
        });
        volumeSlider.onValueChanged.AddListener(GameSettings.SetVolume);
        fullscreenButton.onClick.AddListener(() =>
        {
            GameSettings.SetFullscreen(!GameSettings.Data.fullscreen);
            RefreshButtonLabels();
        });
        qualityButton.onClick.AddListener(() =>
        {
            GameSettings.SetQuality((QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length);
            RefreshButtonLabels();
        });
        resetControlsButton.onClick.AddListener(() =>
        {
            if (asset == null || KeyRebinder.IsRebinding) return;
            GameSettings.ResetBindings(asset);
            RefreshTexts();
        });
        resetProgressButton.onClick.AddListener(() =>
        {
            if (!confirmingReset)
            {
                confirmingReset = true;
                UiKit.ButtonLabel(resetProgressButton).text = Loc.Get("settings.resetConfirm");
                return;
            }
            confirmingReset = false;
            GameProgress.ResetAll();
            UiKit.ButtonLabel(resetProgressButton).text = Loc.Get("settings.resetDone");
        });
    }

    private void OnEnable()
    {
        Loc.LanguageChanged += RefreshTexts;
    }

    private void OnDisable()
    {
        Loc.LanguageChanged -= RefreshTexts;
    }

    // Paneli açan ekran çağırır; klavye seçiminin başlayacağı butonu döndürür
    public Selectable Open(InputActionAsset actions, UnityAction onBack)
    {
        asset = actions;
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(onBack);

        confirmingReset = false;
        volumeSlider.SetValueWithoutNotify(GameSettings.Data.masterVolume);
        RefreshTexts();
        return languageButton;
    }

    // Kodla doldurulan tüm yazılar (sabit yazılar LocalizedText ile kendini yeniler)
    private void RefreshTexts()
    {
        RefreshButtonLabels();
        if (!confirmingReset) UiKit.ButtonLabel(resetProgressButton).text = Loc.Get("settings.resetProgress");
        // Tuş atanırken satırlar yeniden kurulmaz (atama bitince satır kendini günceller)
        if (KeyRebinder.IsRebinding) return;
        messageText.text = asset == null ? Loc.Get("settings.noActions") : "";
        RebuildKeyRows();
    }

    private void RefreshButtonLabels()
    {
        UiKit.ButtonLabel(fullscreenButton).text =
            Loc.Format("settings.fullscreen", Loc.Get(GameSettings.Data.fullscreen ? "common.on" : "common.off"));
        UiKit.ButtonLabel(qualityButton).text =
            Loc.Format("settings.graphics", QualitySettings.names[QualitySettings.GetQualityLevel()]);
    }

    private void RebuildKeyRows()
    {
        keyRows.Clear();
        if (asset == null) return;

        foreach (KeyRebinder.Entry entry in KeyRebinder.Rebindables)
        {
            KeyRebinder.Entry e = entry;
            GameObject row = keyRows.Add();
            var button = row.GetComponent<Button>();
            TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
            label.text = KeyText(e);

            button.onClick.AddListener(() =>
            {
                if (KeyRebinder.IsRebinding) return;
                label.text = Loc.Format("settings.pressKey", e.Label);
                messageText.text = "";
                KeyRebinder.Start(asset, e, error =>
                {
                    // Atama bitmeden panel kapandıysa yazılar artık yok
                    if (label == null) return;
                    label.text = KeyText(e);
                    messageText.text = error ?? "";
                });
            });
        }
    }

    private string KeyText(KeyRebinder.Entry entry) => $"{entry.Label}: {KeyRebinder.DisplayName(asset, entry)}";
}
