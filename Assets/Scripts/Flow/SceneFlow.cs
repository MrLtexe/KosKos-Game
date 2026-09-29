using UnityEngine;
using UnityEngine.SceneManagement;

// Sahne geçişleri. Her yüklemede zaman ölçeği normale döner (duraklatma, sonuç ekranı, sapan yavaşlaması kalmasın).
public static class SceneFlow
{
    public const string MenuScene = "MainMenu";

    // Projenin varsayılan fizik adımı; sapan yavaşlaması bunu değiştirebilir
    private static float defaultFixedDeltaTime = 0.02f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void CacheDefaults()
    {
        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    public static void ResetTime()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }

    public static void LoadMenu()
    {
        ResetTime();
        SceneManager.LoadScene(MenuScene);
    }

    public static void LoadLevel(LevelCatalog catalog, int index)
    {
        ResetTime();
        SceneManager.LoadScene(catalog.Entries[index].sceneName);
    }

    public static void ReloadCurrent()
    {
        ResetTime();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
