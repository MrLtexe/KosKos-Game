using UnityEngine;
using U = TestSceneBuilderUtils;

// Temel mekaniklerin (Section B) ortak sahne parçaları: test dünyası ve prototip bölümler kullanır.
// (Eski ayrı "Section B Test Scene" sahnesi kaldırıldı; testler KosKos > Build Test World ile yapılır.)
public static class SectionBTestSceneBuilder
{
    private static readonly Color BreakableColor = new Color(1f, 0.55f, 0.1f);

    // Kırılabilirler Ground layer'ında: sıyrılganlık ile içlerinden geçilemez, sadece kılıç veya boost ile aşılır
    internal static void BreakableBlock(Transform parent, int layer, string name, float xLeft, float yBottom, float width, float height)
    {
        GameObject block = U.Block(parent, layer, name, xLeft, yBottom, width, height);
        block.AddComponent<Breakable>();
        U.Colorize(block, BreakableColor);
    }
}
