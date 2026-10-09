using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// GEÇİCİ ayarlar ekranı (ana menü ve duraklatma menüsü ortak kullanır):
// dil, ses, tam ekran, grafik kalitesi, tuş atamaları, ilerlemeyi sıfırlama.
public static class SettingsView
{
    // Ayarlar listesi uzun; ekrana sığsın diye butonlar daha alçak
    private const float RowHeight = 44f;

    public static void Build(Transform parent, InputActionAsset asset, UnityAction onBack)
    {
        RectTransform list = UiKit.VerticalList(parent, 6f);
        UiKit.Line(list, Loc.Get("settings.title"), 48);

        // Dil değişince ekran yeni dille baştan çizilir
        Button language = UiKit.Button(list, Loc.Get("settings.language"), () =>
        {
            int next = ((int)GameSettings.Language + 1) % System.Enum.GetValues(typeof(Language)).Length;
            GameSettings.SetLanguage((Language)next);
            UiKit.Clear(parent);
            Build(parent, asset, onBack);
        }, RowHeight);

        UiKit.Line(list, Loc.Get("settings.volume"), 26);
        UiKit.Slider(list, GameSettings.Data.masterVolume, GameSettings.SetVolume);

        Button fullscreen = UiKit.Button(list, FullscreenText(), null, RowHeight);
        fullscreen.onClick.AddListener(() =>
        {
            GameSettings.SetFullscreen(!GameSettings.Data.fullscreen);
            UiKit.ButtonLabel(fullscreen).text = FullscreenText();
        });

        Button quality = UiKit.Button(list, QualityText(), null, RowHeight);
        quality.onClick.AddListener(() =>
        {
            GameSettings.SetQuality((QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length);
            UiKit.ButtonLabel(quality).text = QualityText();
        });

        UiKit.Line(list, Loc.Get("settings.controls"), 30);
        // Çakışma gibi mesajlar tuş listesinin altında görünür
        Text message = null;
        if (asset == null)
        {
            UiKit.Line(list, Loc.Get("settings.noActions"), 24);
        }
        else
        {
            foreach (KeyRebinder.Entry entry in KeyRebinder.Rebindables)
            {
                KeyRebinder.Entry e = entry;
                Button key = UiKit.Button(list, KeyText(asset, e), null, RowHeight);
                key.onClick.AddListener(() =>
                {
                    if (KeyRebinder.IsRebinding) return;
                    UiKit.ButtonLabel(key).text = Loc.Format("settings.pressKey", e.Label);
                    message.text = "";
                    KeyRebinder.Start(asset, e, error =>
                    {
                        UiKit.ButtonLabel(key).text = KeyText(asset, e);
                        message.text = error ?? "";
                    });
                });
            }

            UiKit.Button(list, Loc.Get("settings.resetControls"), () =>
            {
                GameSettings.ResetBindings(asset);
                UiKit.Clear(parent);
                Build(parent, asset, onBack);
            }, RowHeight);
        }
        message = UiKit.Line(list, "", 24);
        message.color = new Color(1f, 0.6f, 0.4f);

        // İlerlemeyi sıfırlama iki tıklama ister (yanlışlıkla silinmesin)
        bool confirming = false;
        Button reset = UiKit.Button(list, Loc.Get("settings.resetProgress"), null, RowHeight);
        reset.onClick.AddListener(() =>
        {
            if (!confirming)
            {
                confirming = true;
                UiKit.ButtonLabel(reset).text = Loc.Get("settings.resetConfirm");
                return;
            }
            confirming = false;
            GameProgress.ResetAll();
            UiKit.ButtonLabel(reset).text = Loc.Get("settings.resetDone");
        });

        UiKit.Button(list, Loc.Get("common.back"), onBack, RowHeight);
        UiKit.Select(language);
    }

    private static string FullscreenText() =>
        Loc.Format("settings.fullscreen", Loc.Get(GameSettings.Data.fullscreen ? "common.on" : "common.off"));

    private static string QualityText() => Loc.Format("settings.graphics", QualitySettings.names[QualitySettings.GetQualityLevel()]);

    private static string KeyText(InputActionAsset asset, KeyRebinder.Entry entry) => $"{entry.Label}: {KeyRebinder.DisplayName(asset, entry)}";
}
