using System;
using System.Collections.Generic;

// Kayıt dosyalarının içeriği. JsonUtility alanları okuduğu için hepsi public alandır.

// Bir bölümün kalıcı kaydı
[Serializable]
public class LevelRecord
{
    public string id;
    public bool completed;
    // -1 = henüz kayıt yok
    public float bestTime = -1f;
    public int bestDeaths = -1;
    public Medal bestMedal;
    // Güvenceye alınıp kaydedilmiş toplanabilirlerin kimlikleri (ör. "L1_Kumas_3")
    public List<string> securedCollectibles = new List<string>();
}

// save.json: oyun ilerlemesi
[Serializable]
public class SaveData
{
    public int kumasBalance;
    public List<string> ownedCostumes = new List<string>();
    public string equippedCostume = "";
    public List<LevelRecord> levels = new List<LevelRecord>();
}

// settings.json: ayarlar (ilerleme sıfırlansa da korunur)
[Serializable]
public class SettingsData
{
    public float masterVolume = 1f;
    public bool fullscreen = true;
    // -1 = projenin varsayılan kalite seviyesi
    public int qualityLevel = -1;
    // Input System'in SaveBindingOverridesAsJson çıktısı
    public string bindingOverrides = "";
}
