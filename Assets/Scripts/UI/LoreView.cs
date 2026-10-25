using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Belgeler paneli (A.3): her bölümün Belgesi toplandıysa başlığı ve metni, yoksa "???".
// Satır şablonunda "Title" ve "Body" adlı iki yazı olmalı; düzen ve görünüm MainMenuUI prefab'ında düzenlenir.
public class LoreView : MonoBehaviour
{
    [Tooltip("Belge satırlarının listesi (şablonda 'Title' ve 'Body' adlı iki TextMeshPro yazısı olmalı).")]
    [SerializeField] private UiRowList rows;

    [Tooltip("Geri butonu (ana menüye döner).")]
    [SerializeField] private Button backButton;

    public Button BackButton => backButton;

    public Selectable Open(LevelCatalog catalog)
    {
        rows.Clear();
        if (catalog == null) return backButton;

        for (int i = 0; i < catalog.Entries.Count; i++)
        {
            LevelCatalog.Entry entry = catalog.Entries[i];
            // Prototipte her bölümde tek Belge var (kimlik: <bölüm>_Belge_1)
            bool found = GameProgress.IsCollected(entry.id, $"{entry.id}{GameProgress.BelgeTag}1");

            GameObject row = rows.Add();
            TMP_Text title = row.transform.Find("Title").GetComponent<TMP_Text>();
            TMP_Text body = row.transform.Find("Body").GetComponent<TMP_Text>();
            title.text = found ? entry.BelgeTitle : $"{i + 1}. ???";
            body.text = found ? entry.BelgeText : "";
            body.gameObject.SetActive(found);
        }

        return backButton;
    }
}
