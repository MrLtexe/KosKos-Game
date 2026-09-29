using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

// Ayarlar: settings.json dosyasına SADECE bu sınıf dokunur. İlerleme sıfırlansa da ayarlar korunur.
// Ses, tam ekran ve kalite oyun başında uygulanır; tuş atamaları Input Actions asset'i kullanılmadan önce uygulanır.
public static class GameSettings
{
    private const string FileName = "settings.json";

    private static SettingsData data;

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static SettingsData Data
    {
        get
        {
            if (data == null) Load();
            return data;
        }
    }

    // Domain reload kapatılırsa önbellek Play oturumları arasında kalır; dosyadan tekrar okunsun
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        data = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyStartup()
    {
        AudioListener.volume = Data.masterVolume;
        Screen.fullScreen = Data.fullscreen;
        if (Data.qualityLevel >= 0 && Data.qualityLevel < QualitySettings.names.Length)
        {
            QualitySettings.SetQualityLevel(Data.qualityLevel, true);
        }
    }

    private static void Load()
    {
        data = null;
        if (File.Exists(FilePath))
        {
            try
            {
                data = JsonUtility.FromJson<SettingsData>(File.ReadAllText(FilePath));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[KosKos] Ayar dosyası okunamadı, varsayılanlar kullanılıyor: {e.Message}");
            }
        }
        if (data == null) data = new SettingsData();
    }

    private static void Save()
    {
        File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
    }

    public static void SetVolume(float volume)
    {
        Data.masterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = Data.masterVolume;
        Save();
    }

    public static void SetFullscreen(bool fullscreen)
    {
        Data.fullscreen = fullscreen;
        Screen.fullScreen = fullscreen;
        Save();
    }

    public static void SetQuality(int level)
    {
        Data.qualityLevel = level;
        QualitySettings.SetQualityLevel(level, true);
        Save();
    }

    // Kayıtlı tuş atamalarını asset'e uygular (önce eskileri temizler)
    public static void ApplyBindings(InputActionAsset asset)
    {
        if (asset == null) return;
        asset.RemoveAllBindingOverrides();
        if (!string.IsNullOrEmpty(Data.bindingOverrides)) asset.LoadBindingOverridesFromJson(Data.bindingOverrides);
    }

    public static void SaveBindings(InputActionAsset asset)
    {
        Data.bindingOverrides = asset.SaveBindingOverridesAsJson();
        Save();
    }

    public static void ResetBindings(InputActionAsset asset)
    {
        asset.RemoveAllBindingOverrides();
        Data.bindingOverrides = "";
        Save();
    }
}
