using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// GEÇİCİ mağaza: Kumaş ile kostüm satın alma ve giyme. Kostüm şimdilik sadece oyuncunun rengi.
public static class ShopView
{
    public static void Build(Transform parent, UnityAction onBack)
    {
        RectTransform list = UiKit.VerticalList(parent, 10f);
        UiKit.Line(list, "Shop", 56);
        UiKit.Line(list, $"Fabric: {GameProgress.Data.kumasBalance}", 34);

        Selectable first = null;
        string equipped = GameProgress.EquippedCostumeId;
        foreach (Costume costume in CostumeCatalog.All)
        {
            bool owned = GameProgress.OwnsCostume(costume.Id);
            bool isEquipped = costume.Id == equipped;
            string status = isEquipped ? "Equipped" : owned ? "Equip" : $"Buy ({costume.Price} Fabric)";

            Costume c = costume;
            Button button = UiKit.Button(list, $"{costume.Name}   -   {status}", () =>
            {
                if (GameProgress.OwnsCostume(c.Id)) GameProgress.Equip(c.Id);
                else GameProgress.TryBuyCostume(c);
                Refresh(parent, onBack);
            });
            button.interactable = !isEquipped && (owned || GameProgress.Data.kumasBalance >= costume.Price);
            if (button.interactable && first == null) first = button;
        }

        Button back = UiKit.Button(list, "Back", onBack);
        UiKit.Select(first != null ? first : back);
    }

    private static void Refresh(Transform parent, UnityAction onBack)
    {
        UiKit.Clear(parent);
        Build(parent, onBack);
    }
}
