using UnityEngine;

// Mağazadaki kostüm (GEÇİCİ: şimdilik sadece oyuncu kapsülünün rengi). Adı çeviri tablosundan gelir ("costume." + kimlik).
public readonly struct Costume
{
    public readonly string Id;
    public readonly int Price;
    public readonly Color Color;

    public Costume(string id, int price, Color color)
    {
        Id = id;
        Price = price;
        Color = color;
    }

    // Seçili dildeki adı
    public string Name => Loc.Get("costume." + Id);
}

// GEÇİCİ kostüm listesi. Gerçek kostümler (sanat) gelince bir asset'e taşınacak.
public static class CostumeCatalog
{
    public static readonly Costume Default = new Costume("default", 0, Color.white);

    public static readonly Costume[] All =
    {
        Default,
        new Costume("c_red", 5, new Color(0.9f, 0.25f, 0.25f)),
        new Costume("c_green", 10, new Color(0.35f, 0.85f, 0.35f)),
        new Costume("c_blue", 15, new Color(0.3f, 0.45f, 0.95f)),
        new Costume("c_gold", 25, new Color(1f, 0.8f, 0.2f))
    };

    // Kimliği verilen kostüm; bulunamazsa varsayılan
    public static Costume Find(string id)
    {
        foreach (Costume costume in All)
        {
            if (costume.Id == id) return costume;
        }
        return Default;
    }
}
