using UnityEngine;

// SADECE EDİTÖR: bölüm editöründe "▶ Test" ile seçilen güvenli alandan başlatma.
// Editör, Play'e girmeden önce alanın x konumunu saklar (kit alanlarının adları aynı olabilir); sahne yüklenince oyuncu
// o konumdaki alana alınır ve kayıt hemen silinir (sonraki normal Play yine başlangıç alanından başlar).
// Oyun build'inde bu dosya hiçbir şey yapmaz.
public static class LevelEditorTestSpawn
{
#if UNITY_EDITOR
    private const string SessionKey = "KosKos.TestStartZoneX";

    // Bölüm editörü çağırır: bir sonraki Play, x konumu bu olan güvenli alandan başlar
    public static void RequestStartZone(float zoneX)
    {
        UnityEditor.SessionState.SetFloat(SessionKey, zoneX);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyRequestedZone()
    {
        float zoneX = UnityEditor.SessionState.GetFloat(SessionKey, float.NaN);
        // Tek kullanımlık: bulunsun bulunmasın hemen silinir
        UnityEditor.SessionState.EraseFloat(SessionKey);
        if (float.IsNaN(zoneX)) return;

        // Başlangıç alanı Awake'te seçilir; bu kanca Awake'ten sonra çalıştığı için seçim burada değiştirilebilir
        SafeZone zone = null;
        float best = float.MaxValue;
        foreach (SafeZone candidate in Object.FindObjectsByType<SafeZone>(FindObjectsSortMode.None))
        {
            float distance = Mathf.Abs(candidate.transform.position.x - zoneX);
            if (distance < best)
            {
                best = distance;
                zone = candidate;
            }
        }
        var death = Object.FindFirstObjectByType<PlayerDeath>();
        if (zone == null || death == null) return;

        SafeZone.ForceCurrent(zone);
        death.RespawnNow();
    }
#endif
}
