using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// GEÇİCİ bölüm seçim ekranı (A.1): açık bölümler rekorlarıyla, kilitli bölümler gri.
public static class LevelSelectView
{
    public static void Build(Transform parent, LevelCatalog catalog, UnityAction onBack)
    {
        RectTransform list = UiKit.VerticalList(parent, 10f);
        UiKit.Line(list, "Select Level", 56);

        Selectable first = null;
        if (catalog == null)
        {
            UiKit.Line(list, "No level catalog assigned.", 28);
        }
        else
        {
            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                LevelCatalog.Entry entry = catalog.Entries[i];
                bool unlocked = GameProgress.IsUnlocked(catalog, i);
                int index = i;

                Button button = UiKit.Button(list, unlocked ? UnlockedText(entry, i) : $"[Locked]  {i + 1}. {entry.displayName}",
                    () => SceneFlow.LoadLevel(catalog, index));
                button.interactable = unlocked;
                if (unlocked && first == null) first = button;
            }
        }

        Button back = UiKit.Button(list, "Back", onBack);
        UiKit.Select(first != null ? first : back);
    }

    private static string UnlockedText(LevelCatalog.Entry entry, int index)
    {
        LevelRecord record = GameProgress.GetRecord(entry.id);
        if (!record.completed) return $"{index + 1}. {entry.displayName}";

        int fabric = GameProgress.CountSecured(entry.id, GameProgress.KumasTag);
        int documents = GameProgress.CountSecured(entry.id, GameProgress.BelgeTag);
        return $"{index + 1}. {entry.displayName}   |   {LevelHud.FormatTime(record.bestTime)}   Deaths {record.bestDeaths}   " +
               $"{MedalRule.DisplayName(record.bestMedal)}   Fabric {fabric}   Document {documents}";
    }
}
