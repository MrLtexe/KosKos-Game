using System.Collections.Generic;
using UnityEngine;

public enum Language
{
    English,
    Turkish
}

// Oyun içi metinler (çeviri tablosu). Arayüzdeki her metin buradan anahtarla alınır: Loc.Get("menu.play").
// Yeni metin eklemek için tabloya bir satır ekleyin: { "anahtar", new[] { "İngilizce", "Türkçe" } }.
// Yeni dil eklemek için Language enum'una ekleyip her satıra bir çeviri daha yazın (sıra enum ile aynı).
public static class Loc
{
    public static Language Current => GameSettings.Language;

    // Biçimli metinler: {0}, {1} ... yerine Format ile değer konur
    private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
    {
        // Ana menü
        { "menu.play", new[] { "Play", "Oyna" } },
        { "menu.shop", new[] { "Shop", "Mağaza" } },
        { "menu.documents", new[] { "Documents", "Belgeler" } },
        { "menu.settings", new[] { "Settings", "Ayarlar" } },
        { "menu.credits", new[] { "Credits", "Yapımcılar" } },
        { "menu.quit", new[] { "Quit", "Çıkış" } },
        { "common.back", new[] { "Back", "Geri" } },
        { "common.on", new[] { "On", "Açık" } },
        { "common.off", new[] { "Off", "Kapalı" } },

        // Bölüm seçimi
        { "levels.title", new[] { "Select Level", "Bölüm Seç" } },
        { "levels.noCatalog", new[] { "No level catalog assigned.", "Bölüm kataloğu atanmamış." } },
        { "levels.locked", new[] { "[Locked]", "[Kilitli]" } },
        { "levels.record", new[] { "{0}   Deaths {1}   {2}   Fabric {3}   Document {4}", "{0}   Ölüm {1}   {2}   Kumaş {3}   Belge {4}" } },

        // Mağaza
        { "shop.title", new[] { "Shop", "Mağaza" } },
        { "shop.balance", new[] { "Fabric: {0}", "Kumaş: {0}" } },
        { "shop.equipped", new[] { "Equipped", "Giyili" } },
        { "shop.equip", new[] { "Equip", "Giy" } },
        { "shop.buy", new[] { "Buy ({0} Fabric)", "Satın al ({0} Kumaş)" } },

        // Belgeler
        { "lore.title", new[] { "Documents", "Belgeler" } },

        // Ayarlar
        { "settings.title", new[] { "Settings", "Ayarlar" } },
        { "settings.language", new[] { "Language: English", "Dil: Türkçe" } },
        { "settings.volume", new[] { "Volume", "Ses" } },
        { "settings.fullscreen", new[] { "Fullscreen: {0}", "Tam ekran: {0}" } },
        { "settings.graphics", new[] { "Graphics: {0}", "Grafik: {0}" } },
        { "settings.controls", new[] { "Controls", "Kontroller" } },
        { "settings.noActions", new[] { "No Input Actions assigned.", "Input Actions atanmamış." } },
        { "settings.pressKey", new[] { "{0}: press a key...  (Esc: cancel)", "{0}: bir tuşa bas...  (Esc: iptal)" } },
        { "settings.keyInUse", new[] { "This key is already in use", "Bu tuş zaten kullanılıyor" } },
        { "settings.resetControls", new[] { "Reset controls to default", "Kontrolleri varsayılana döndür" } },
        { "settings.resetProgress", new[] { "Reset progress", "İlerlemeyi sıfırla" } },
        { "settings.resetConfirm", new[] { "Are you sure? Click again to reset progress", "Emin misin? Sıfırlamak için tekrar tıkla" } },
        { "settings.resetDone", new[] { "Progress reset", "İlerleme sıfırlandı" } },

        // Tuş atama eylemleri
        { "action.jump", new[] { "Jump", "Zıpla" } },
        { "action.dash", new[] { "Dash", "Atılma" } },
        { "action.sword", new[] { "Sword", "Kılıç" } },
        { "action.boost", new[] { "Boost Charge", "Boost Şarjı" } },
        { "action.hook", new[] { "Hook", "Kanca" } },
        { "action.jetBag", new[] { "Jet-Bag", "Jet-Çanta" } },
        { "action.magBoots", new[] { "Magnetic Boots", "Manyetik Botlar" } },
        { "action.balanceLeft", new[] { "Balance Left", "Denge Sol" } },
        { "action.balanceRight", new[] { "Balance Right", "Denge Sağ" } },

        // Duraklatma
        { "pause.title", new[] { "Paused", "Duraklatıldı" } },
        { "pause.resume", new[] { "Resume", "Devam" } },
        { "pause.restart", new[] { "Restart level", "Bölümü yeniden başlat" } },
        { "pause.mainMenu", new[] { "Main menu", "Ana menü" } },

        // Bölüm içi gösterge ve sonuç ekranı
        { "hud.deaths", new[] { "Deaths: {0}", "Ölüm: {0}" } },
        { "hud.fabric", new[] { "Fabric: {0}/{1}", "Kumaş: {0}/{1}" } },
        { "hud.document", new[] { "Document: {0}/{1}", "Belge: {0}/{1}" } },
        { "results.title", new[] { "Level complete!", "Bölüm tamamlandı!" } },
        { "results.time", new[] { "Time: {0}", "Süre: {0}" } },
        { "results.collectibles", new[] { "Fabric: {0}/{1}    Document: {2}/{3}", "Kumaş: {0}/{1}    Belge: {2}/{3}" } },
        { "results.medal", new[] { "Medal: {0}", "Madalya: {0}" } },
        { "results.newRecord", new[] { "  (New record!)", "  (Yeni rekor!)" } },
        { "results.notSaved", new[] { "Not saved (level is not in the catalog)", "Kaydedilmedi (bölüm katalogda yok)" } },
        { "results.fabricGained", new[] { "+{0} Fabric", "+{0} Kumaş" } },
        { "results.next", new[] { "Next level", "Sonraki bölüm" } },
        { "results.replay", new[] { "Replay", "Tekrar oyna" } },

        // Madalyalar
        { "medal.gold", new[] { "Gold", "Altın" } },
        { "medal.silver", new[] { "Silver", "Gümüş" } },
        { "medal.bronze", new[] { "Bronze", "Bronz" } },

        // Oyuncu üstündeki ipuçları
        { "hint.launch", new[] { "{0}: launch", "{0}: fırlat" } },

        // Kostümler (anahtar: "costume." + kostüm kimliği)
        { "costume.default", new[] { "Default", "Varsayılan" } },
        { "costume.c_red", new[] { "Red Activist", "Kızıl Aktivist" } },
        { "costume.c_green", new[] { "Sprout", "Filiz" } },
        { "costume.c_blue", new[] { "Midnight Blue", "Gece Mavisi" } },
        { "costume.c_gold", new[] { "Golden Runner", "Altın Koşucu" } },

        // Yapımcılar
        { "credits.title", new[] { "Credits", "Yapımcılar" } },
        { "role.developer", new[] { "Developer", "Geliştirici" } },
        { "role.gameDesign", new[] { "Game Design", "Oyun Tasarımı" } },
        { "role.artist3d", new[] { "3D Artist", "3D Sanatçı" } },
        { "role.artist2d", new[] { "2D Artist", "2D Sanatçı" } },
        { "role.narrativeDesigner", new[] { "Narrative Designer", "Anlatı Tasarımcısı" } },
        { "role.director", new[] { "Director", "Yönetmen" } },
        { "role.gameDesigner", new[] { "Game Designer", "Oyun Tasarımcısı" } }
    };

    public static string Get(string key)
    {
        if (Table.TryGetValue(key, out string[] texts))
        {
            int index = (int)Current;
            // Çeviri eksikse İngilizce gösterilir
            if (index < texts.Length && !string.IsNullOrEmpty(texts[index])) return texts[index];
            return texts[0];
        }

        Debug.LogWarning($"[KosKos] Çeviri anahtarı bulunamadı: {key}");
        return key;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }
}
