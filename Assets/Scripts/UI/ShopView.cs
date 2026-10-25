using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Mağaza paneli: Kumaş ile kostüm satın alma ve giyme (kostüm şimdilik sadece oyuncunun rengi).
// Satırlar şablondan kopyalanır (şablon = yazılı bir buton); düzen ve görünüm MainMenuUI prefab'ında düzenlenir.
public class ShopView : MonoBehaviour
{
    [Tooltip("Kumaş bakiyesini gösteren yazı.")]
    [SerializeField] private TMP_Text balanceText;

    [Tooltip("Kostüm satırlarının listesi (şablonu yazılı bir buton).")]
    [SerializeField] private UiRowList rows;

    [Tooltip("Geri butonu (ana menüye döner).")]
    [SerializeField] private Button backButton;

    public Button BackButton => backButton;

    // Paneli doldurur; klavye seçiminin başlayacağı butonu döndürür
    public Selectable Open()
    {
        balanceText.text = Loc.Format("shop.balance", GameProgress.Data.kumasBalance);
        rows.Clear();

        Selectable first = null;
        string equipped = GameProgress.EquippedCostumeId;
        foreach (Costume costume in CostumeCatalog.All)
        {
            bool owned = GameProgress.OwnsCostume(costume.Id);
            bool isEquipped = costume.Id == equipped;
            string status = isEquipped ? Loc.Get("shop.equipped")
                : owned ? Loc.Get("shop.equip")
                : Loc.Format("shop.buy", costume.Price);

            GameObject row = rows.Add();
            var button = row.GetComponent<Button>();
            row.GetComponentInChildren<TMP_Text>(true).text = $"{costume.Name}   -   {status}";
            button.interactable = !isEquipped && (owned || GameProgress.Data.kumasBalance >= costume.Price);

            Costume c = costume;
            button.onClick.AddListener(() =>
            {
                if (GameProgress.OwnsCostume(c.Id)) GameProgress.Equip(c.Id);
                else GameProgress.TryBuyCostume(c);
                UiKit.Select(Open());
            });
            if (button.interactable && first == null) first = button;
        }

        return first != null ? first : backButton;
    }
}
