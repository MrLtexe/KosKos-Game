using UnityEditor;

// Test kolaylığı: kayıt dosyasını (save.json) siler. Ayarlar korunur.
public static class SaveMenu
{
    [MenuItem("KosKos/Save/Reset Save")]
    private static void ResetSave()
    {
        GameProgress.ResetAll();
    }
}
