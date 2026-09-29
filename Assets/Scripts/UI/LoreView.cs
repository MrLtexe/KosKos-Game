using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// GEÇİCİ Belgeler ekranı (A.3): her bölümün Belgesi toplandıysa başlığı ve metni, yoksa "???".
public static class LoreView
{
    public static void Build(Transform parent, LevelCatalog catalog, UnityAction onBack)
    {
        RectTransform list = UiKit.VerticalList(parent, 10f);
        UiKit.Line(list, "Documents", 56);

        if (catalog == null)
        {
            UiKit.Line(list, "No level catalog assigned.", 28);
        }
        else
        {
            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                LevelCatalog.Entry entry = catalog.Entries[i];
                // Prototipte her bölümde tek Belge var (kimlik: <bölüm>_Belge_1)
                bool found = GameProgress.IsCollected(entry.id, $"{entry.id}{GameProgress.BelgeTag}1");
                if (found)
                {
                    UiKit.Line(list, entry.belgeTitle, 34);
                    UiKit.Paragraph(list, entry.belgeText, 26);
                }
                else
                {
                    UiKit.Line(list, $"{i + 1}. ???", 34);
                }
            }
        }

        Button back = UiKit.Button(list, "Back", onBack);
        UiKit.Select(back);
    }
}
