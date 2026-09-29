// Bölüm sonu madalyası. Sıralama önemli: büyük değer = daha iyi madalya.
public enum Medal
{
    None,
    Bronze,
    Silver,
    Gold
}

// Madalya kuralı (tasarımcı onaylı): tek yerde durur, kural değişirse sadece burası değişir.
// Bronz = bölümü bitir. Gümüş = süre <= gümüş süresi. Altın = süre <= altın süresi VE bölümdeki tüm toplanabilirler (aynı oynanışta).
// Ölümler madalyayı etkilemez. Süre sınırı 0 ise o madalya kapalıdır.
public static class MedalRule
{
    public static Medal Compute(float time, bool allCollected, float silverTime, float goldTime)
    {
        if (goldTime > 0f && time <= goldTime && allCollected) return Medal.Gold;
        if (silverTime > 0f && time <= silverTime) return Medal.Silver;
        return Medal.Bronze;
    }

    // Arayüzde gösterilen ad
    public static string DisplayName(Medal medal)
    {
        switch (medal)
        {
            case Medal.Gold: return "Gold";
            case Medal.Silver: return "Silver";
            case Medal.Bronze: return "Bronze";
            default: return "—";
        }
    }
}
