using UnityEngine;

// Mağazadaki kostüm (GEÇİCİ: şimdilik sadece oyuncu kapsülünün rengi)
public readonly struct Costume
{
    public readonly string Id;
    public readonly string Name;
    public readonly int Price;
    public readonly Color Color;

    public Costume(string id, string name, int price, Color color)
    {
        Id = id;
        Name = name;
        Price = price;
        Color = color;
    }
}

// GEÇİCİ kostüm listesi. Gerçek kostümler (sanat) gelince bir asset'e taşınacak.
public static class CostumeCatalog
{
    public static readonly Costume Default = new Costume("default", "Default", 0, Color.white);

    public static readonly Costume[] All =
    {
        Default,
        new Costume("c_red", "Red Activist", 5, new Color(0.9f, 0.25f, 0.25f)),
        new Costume("c_green", "Sprout", 10, new Color(0.35f, 0.85f, 0.35f)),
        new Costume("c_blue", "Midnight Blue", 15, new Color(0.3f, 0.45f, 0.95f)),
        new Costume("c_gold", "Golden Runner", 25, new Color(1f, 0.8f, 0.2f))
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
