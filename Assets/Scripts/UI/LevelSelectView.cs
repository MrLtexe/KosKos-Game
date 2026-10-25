using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Bölüm seçim paneli (A.1): açık bölümler rekorlarıyla, kilitli bölümler gri.
// Satırlar şablondan kopyalanır (şablon = yazılı bir buton); düzen ve görünüm MainMenuUI prefab'ında düzenlenir.
public class LevelSelectView : MonoBehaviour
{
    [Tooltip("Bölüm satırlarının listesi (şablonu yazılı bir buton).")]
    [SerializeField] private UiRowList rows;

    [Tooltip("Geri butonu (ana menüye döner).")]
    [SerializeField] private Button backButton;

    public Button BackButton => backButton;

    // Listeyi doldurur; klavye seçiminin başlayacağı butonu döndürür
    public Selectable Open(LevelCatalog catalog)
    {
        rows.Clear();
        Selectable first = null;
        if (catalog == null)
        {
            Debug.LogWarning("[KosKos] MainMenuUI'da bölüm kataloğu atanmamış.");
            return backButton;
        }

        for (int i = 0; i < catalog.Entries.Count; i++)
        {
            LevelCatalog.Entry entry = catalog.Entries[i];
            bool unlocked = GameProgress.IsUnlocked(catalog, i);
            int index = i;

            GameObject row = rows.Add();
            var button = row.GetComponent<Button>();
            row.GetComponentInChildren<TMP_Text>(true).text = unlocked
                ? UnlockedText(entry, i)
                : $"{Loc.Get("levels.locked")}  {i + 1}. {entry.DisplayName}";
            button.interactable = unlocked;
            button.onClick.AddListener(() => SceneFlow.LoadLevel(catalog, index));
            if (unlocked && first == null) first = button;
        }

        return first != null ? first : backButton;
    }

    private static string UnlockedText(LevelCatalog.Entry entry, int index)
    {
        LevelRecord record = GameProgress.GetRecord(entry.id);
        if (!record.completed) return $"{index + 1}. {entry.DisplayName}";

        int fabric = GameProgress.CountSecured(entry.id, GameProgress.KumasTag);
        int documents = GameProgress.CountSecured(entry.id, GameProgress.BelgeTag);
        string deaths = record.bestDeaths < 0 ? "-" : record.bestDeaths.ToString();
        string stats = Loc.Format("levels.record", LevelHud.FormatTime(record.bestTime), deaths,
            MedalRule.DisplayName(record.bestMedal), fabric, documents);
        return $"{index + 1}. {entry.DisplayName}   |   {stats}";
    }
}
