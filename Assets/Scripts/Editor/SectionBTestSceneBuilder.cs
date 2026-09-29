using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using U = TestSceneBuilderUtils;

// Tek tıkla Section B test sahnesini sıfırdan üretir: KosKos > Build Section B Test Scene.
// Sahne her seferinde yeniden oluşturulur; elle düzenlemeyin.
// Player prefab'ı (Assets/Prefabs/Player.prefab) sadece yoksa oluşturulur; varsa olduğu gibi kullanılır.
public static class SectionBTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Test_SectionB.unity";

    private static readonly Color BreakableColor = new Color(1f, 0.55f, 0.1f);
    private static readonly Color ThinWallColor = new Color(0.2f, 0.9f, 1f);

    [MenuItem("KosKos/Build Section B Test Scene")]
    private static void Build()
    {
        if (!U.BeginScene(out Scene scene, out InputActionAsset actions, out U.Layers layers)) return;

        Transform level = new GameObject("Level").transform;
        BuildLevel(level, layers.Ground, layers.Phaseable);

        GameObject player = U.SpawnPlayer(layers, actions, new Vector3(0f, 1.05f, 0f));
        U.AddFollowCamera(player);

        U.SaveScene(scene, ScenePath);
    }

    private static void BuildLevel(Transform parent, int groundLayer, int phaseableLayer)
    {
        // Zemin üst yüzeyi y=0. Her zemin parçası TEK uzun küp (eklem yerlerinde yanlış çarpmayı önler).
        // --- Faz 1 bölümü ---
        // Bölüm 1: düz koşu + alçak duvar (tek zıplama ile aşılır)
        U.Ground(parent, groundLayer, "Ground_A", -5f, 40f);
        U.Block(parent, groundLayer, "LowWall", 20f, 0f, 1f, 1.5f);

        // Boşluk 40-44 (tek zıplama ile geçilir)
        U.Ground(parent, groundLayer, "Ground_B", 44f, 80f);
        // Yüksek duvar: sadece çift zıplama ile aşılır
        U.Block(parent, groundLayer, "TallWall", 62f, 0f, 1f, 4.5f);

        // Geniş boşluk 80-87 (zıplama + havada atılma gerekir)
        U.Ground(parent, groundLayer, "Ground_C", 87f, 250f);
        // Yükseltilmiş platform: üstüne inmek güvenli, yanına çarpmak ölüm
        U.Block(parent, groundLayer, "RaisedPlatform", 100f, 0f, 10f, 2f);
        // Alçak tavan: altında zıplayınca kafa çarpar ama ölmez
        U.Block(parent, groundLayer, "LowCeiling", 115f, 3.2f, 10f, 1f);

        // --- Faz 2 bölümü ---
        // Kırılabilirler Ground layer'ında: sıyrılganlık ile içlerinden geçilemez, sadece kılıç veya boost ile aşılır
        // Yerdeki kırılabilir: kılıçla kır ya da çarpıp öl
        BreakableBlock(parent, groundLayer, "Breakable_Ground", 145f, 0f, 1f, 2f);
        // Havadaki kırılabilir: havada kılıçla vurunca atılma yenilenir (yerden kılıçla erişilemeyecek yükseklikte)
        BreakableBlock(parent, groundLayer, "Breakable_Floating", 160f, 3f, 1f, 1f);
        // 175-195 arası düz alan: Boost şarjı denemek için
        // Yüksek kırılabilir duvar: zıplanamaz; boost'la çarp veya kılıçla kır
        BreakableBlock(parent, groundLayer, "Breakable_TallWall", 195f, 0f, 1f, 6f);
        // İnce duvar: sadece sıyrılganlık ile atılarak geçilir
        GameObject thinWall = U.Block(parent, phaseableLayer, "ThinWall_Phase", 225f, 0f, 0.3f, 6f);
        U.Colorize(thinWall, ThinWallColor);

        U.SafeZoneAt(parent, "SafeZone_Start", 0f, true);
        U.SafeZoneAt(parent, "SafeZone_Mid", 48f, false);
        U.SafeZoneAt(parent, "SafeZone_Phase2", 135f, false);

        U.KillZone(parent, 120f, 400f);
    }

    internal static void BreakableBlock(Transform parent, int layer, string name, float xLeft, float yBottom, float width, float height)
    {
        GameObject block = U.Block(parent, layer, name, xLeft, yBottom, width, height);
        block.AddComponent<Breakable>();
        U.Colorize(block, BreakableColor);
    }
}
