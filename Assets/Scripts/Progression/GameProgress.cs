using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Bir bölüm sonunda neyin yeni olduğu (sonuç ekranı için)
public struct RunResult
{
    public bool newBestTime;
    public bool newBestDeaths;
    public bool newMedal;
    public int kumasGained;
}

// Oyun ilerlemesi: save.json dosyasına SADECE bu sınıf dokunur.
// Dosya ilk kullanımda yüklenir; bölüm sonu ve mağaza alışverişinden sonra kaydedilir.
public static class GameProgress
{
    private const string FileName = "save.json";
    // Toplanabilir kimliklerinde Kumaş olanları ayırt eden parça (ör. "L1_Kumas_3")
    public const string KumasTag = "_Kumas_";
    public const string BelgeTag = "_Belge_";

    private static SaveData data;

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static SaveData Data
    {
        get
        {
            if (data == null) Load();
            return data;
        }
    }

    // Giyili kostüm; hiç seçilmediyse varsayılan
    public static string EquippedCostumeId => CostumeCatalog.Find(Data.equippedCostume).Id;
    public static Color EquippedColor => CostumeCatalog.Find(Data.equippedCostume).Color;

    // Domain reload kapatılırsa önbellek Play oturumları arasında kalır; dosyadan tekrar okunsun
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        data = null;
    }

    private static void Load()
    {
        data = null;
        if (File.Exists(FilePath))
        {
            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[KosKos] Kayıt dosyası okunamadı, yeni kayıt başlatılıyor: {e.Message}");
            }
        }
        if (data == null) data = new SaveData();
    }

    public static void Save()
    {
        File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
    }

    // Kayıt dosyasını siler; ayarlar (settings.json) korunur
    public static void ResetAll()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        data = new SaveData();
        Debug.Log("[KosKos] İlerleme sıfırlandı.");
    }

    // Bölüm kaydı; yoksa oluşturur (dosyaya kaydetmez)
    public static LevelRecord GetRecord(string levelId)
    {
        List<LevelRecord> levels = Data.levels;
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i].id == levelId) return levels[i];
        }
        var record = new LevelRecord { id = levelId };
        levels.Add(record);
        return record;
    }

    // A.1: ilk bölüm hep açık; diğerleri bir önceki bitirilince açılır
    public static bool IsUnlocked(LevelCatalog catalog, int index)
    {
        return index == 0 || GetRecord(catalog.Entries[index - 1].id).completed;
    }

    public static bool IsCollected(string levelId, string collectibleId)
    {
        return GetRecord(levelId).securedCollectibles.Contains(collectibleId);
    }

    // Bölüm sonu: rekorlar (süre ve ölüm en düşük, madalya en yüksek) ve yeni toplanabilirler kaydedilir
    public static RunResult SubmitRun(string levelId, float time, int deaths, IReadOnlyCollection<string> securedIds,
        Medal medal, int kumasPerId)
    {
        LevelRecord record = GetRecord(levelId);
        var result = new RunResult();

        record.completed = true;

        if (record.bestTime < 0f || time < record.bestTime)
        {
            record.bestTime = time;
            result.newBestTime = true;
        }

        // A.4: ölüm sayacı sadece daha az ölünürse güncellenir
        if (record.bestDeaths < 0 || deaths < record.bestDeaths)
        {
            record.bestDeaths = deaths;
            result.newBestDeaths = true;
        }

        if (medal > record.bestMedal)
        {
            record.bestMedal = medal;
            result.newMedal = true;
        }

        // Aynı toplanabilir iki kez sayılmaz; Kumaş sadece yeni kimlikler için eklenir
        foreach (string id in securedIds)
        {
            if (record.securedCollectibles.Contains(id)) continue;
            record.securedCollectibles.Add(id);
            if (id.Contains(KumasTag)) result.kumasGained += kumasPerId;
        }
        Data.kumasBalance += result.kumasGained;

        Save();
        return result;
    }

    // Kaydedilmiş kayıttaki verilen türden toplanabilir sayısı (menüde gösterim için)
    public static int CountSecured(string levelId, string tag)
    {
        int count = 0;
        foreach (string id in GetRecord(levelId).securedCollectibles)
        {
            if (id.Contains(tag)) count++;
        }
        return count;
    }

    public static bool OwnsCostume(string costumeId)
    {
        return costumeId == CostumeCatalog.Default.Id || Data.ownedCostumes.Contains(costumeId);
    }

    public static bool TryBuyCostume(Costume costume)
    {
        if (OwnsCostume(costume.Id) || Data.kumasBalance < costume.Price) return false;

        Data.kumasBalance -= costume.Price;
        Data.ownedCostumes.Add(costume.Id);
        // Satın alınan kostüm hemen giyilir
        Data.equippedCostume = costume.Id;
        Save();
        return true;
    }

    public static void Equip(string costumeId)
    {
        if (!OwnsCostume(costumeId)) return;
        Data.equippedCostume = costumeId;
        Save();
    }
}
