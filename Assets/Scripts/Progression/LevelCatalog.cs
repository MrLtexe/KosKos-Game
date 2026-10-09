using System;
using System.Collections.Generic;
using UnityEngine;

// Oyundaki bölümlerin sıralı listesi. Menü bölüm listesini buradan okur; sıra = açılma sırası (A.1).
// Assets > Create > KosKos > Level Catalog. Prototipte "KosKos > Build Prototype Levels" bu asset'i doldurur.
[CreateAssetMenu(fileName = "LevelCatalog", menuName = "KosKos/Level Catalog")]
public class LevelCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Bölümün kalıcı kimliği (ör. L1). Kayıt dosyası ve toplanabilir kimlikleri bunu kullanır; sonradan değiştirmeyin.")]
        public string id;

        [Tooltip("Menüde görünen bölüm adı.")]
        public string displayName;

        [Tooltip("Yüklenecek sahnenin adı (Build Settings'te olmalı).")]
        public string sceneName;

        [Tooltip("Bu bölümün Belgesi toplandığında Belgeler ekranında görünen başlık.")]
        public string belgeTitle;

        [Tooltip("Bu bölümün Belgesi toplandığında Belgeler ekranında görünen metin.")]
        [TextArea(2, 6)]
        public string belgeText;

        [Header("Türkçe")]
        [Tooltip("Menüde görünen bölüm adı (Türkçe). Boşsa İngilizce ad gösterilir.")]
        public string displayNameTr;

        [Tooltip("Belge başlığı (Türkçe). Boşsa İngilizce başlık gösterilir.")]
        public string belgeTitleTr;

        [Tooltip("Belge metni (Türkçe). Boşsa İngilizce metin gösterilir.")]
        [TextArea(2, 6)]
        public string belgeTextTr;

        // Seçili dildeki metinler (Türkçe boşsa İngilizce)
        public string DisplayName => Pick(displayName, displayNameTr);
        public string BelgeTitle => Pick(belgeTitle, belgeTitleTr);
        public string BelgeText => Pick(belgeText, belgeTextTr);

        private static string Pick(string english, string turkish)
        {
            return Loc.Current == Language.Turkish && !string.IsNullOrEmpty(turkish) ? turkish : english;
        }
    }

    [Tooltip("Bölümler, oynanma sırasıyla.")]
    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;

    // Kimliği verilen bölümün sırası; yoksa -1
    public int IndexOf(string id)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].id == id) return i;
        }
        return -1;
    }
}
