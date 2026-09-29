using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Test hileleri (ana menüdeki hamburger butonu): Kumaş ekle, bölümleri/kostümleri/Belgeleri aç, kaydı sıfırla.
// Sadece Editor'da ve development build'lerde görünür (MainMenu kontrol eder).
public static class CheatsView
{
    public static void Build(Transform parent, LevelCatalog catalog, UnityAction onBack)
    {
        RectTransform list = UiKit.VerticalList(parent, 10f);
        UiKit.Line(list, "Cheats", 56);
        Text status = UiKit.Line(list, $"Fabric: {GameProgress.Data.kumasBalance}", 30);

        Button first = UiKit.Button(list, "+10 Fabric", () => AddFabric(10, status));
        UiKit.Button(list, "+100 Fabric", () => AddFabric(100, status));

        UiKit.Button(list, "Unlock all levels", () =>
        {
            if (catalog == null) return;
            // Her bölümü bitirilmiş say: bir sonraki bölümler açılır
            foreach (LevelCatalog.Entry entry in catalog.Entries) GameProgress.GetRecord(entry.id).completed = true;
            GameProgress.Save();
            status.text = "All levels unlocked";
        });

        UiKit.Button(list, "Unlock all costumes", () =>
        {
            foreach (Costume costume in CostumeCatalog.All)
            {
                if (!GameProgress.OwnsCostume(costume.Id)) GameProgress.Data.ownedCostumes.Add(costume.Id);
            }
            GameProgress.Save();
            status.text = "All costumes unlocked";
        });

        UiKit.Button(list, "Collect all documents", () =>
        {
            if (catalog == null) return;
            foreach (LevelCatalog.Entry entry in catalog.Entries)
            {
                string id = $"{entry.id}{GameProgress.BelgeTag}1";
                LevelRecord record = GameProgress.GetRecord(entry.id);
                if (!record.securedCollectibles.Contains(id)) record.securedCollectibles.Add(id);
            }
            GameProgress.Save();
            status.text = "All documents collected";
        });

        UiKit.Button(list, "Reset save", () =>
        {
            GameProgress.ResetAll();
            status.text = "Save reset";
        });

        UiKit.Button(list, "Back", onBack);
        UiKit.Select(first);
    }

    private static void AddFabric(int amount, Text status)
    {
        GameProgress.Data.kumasBalance += amount;
        GameProgress.Save();
        status.text = $"Fabric: {GameProgress.Data.kumasBalance}";
    }
}
