using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// GEÇİCİ bölüm seçim ekranı (A.1): açık bölümler rekorlarıyla, kilitli bölümler gri.
public static class LevelSelectView
{
    public static void Build(Transform parent, LevelCatalog catalog, UnityAction onBack)
    {
        RectTransform list = UiKit.VerticalList(parent, 10f);
        UiKit.Line(list, Loc.Get("levels.title"), 56);

        Selectable first = null;
        if (catalog == null)
        {
            UiKit.Line(list, Loc.Get("levels.noCatalog"), 28);
        }
        else
        {
            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                LevelCatalog.Entry entry = catalog.Entries[i];
                bool unlocked = GameProgress.IsUnlocked(catalog, i);
                int index = i;

                Button button = UiKit.Button(list, unlocked ? UnlockedText(entry, i) : $"{Loc.Get("levels.locked")}  {i + 1}. {entry.DisplayName}",
                    () => SceneFlow.LoadLevel(catalog, index));
                button.interactable = unlocked;
                if (unlocked && first == null) first = button;
            }
        }

        Button back = UiKit.Button(list, Loc.Get("common.back"), onBack);
        UiKit.Select(first != null ? first : back);
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
