using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using U = TestSceneBuilderUtils;
using B = SectionBTestSceneBuilder;
using C = SectionCGadgetsTestSceneBuilder;
using DE = SectionDEnemiesTestSceneBuilder;
using DT = SectionDTraversalTestSceneBuilder;

// Bölüm kiti: bölüm editörünün paletindeki her parça için bir prefab üretir (Assets/LevelKit/<Kategori>/<Parça>.prefab).
// KosKos > Level Kit > Build Kit Prefabs. Parçalar test dünyasıyla aynı yardımcılarla kurulur, yani aynı davranır.
// Var olan prefab'lara DOKUNMAZ (sadece eksikleri üretir): üretildikten sonra prefab'lar ekibindir, görseli sanatçılar değiştirir.
// Her parçanın kökü boş bir nesnedir ve kökün konumu parçanın "zemin çizgisi"dir (zemin üst yüzeyi kökte y=0).
public static class LevelKitBuilder
{
    public const string KitRoot = "Assets/LevelKit";

    public const string GroundCategory = "Ground & Walls";
    public const string EnemiesCategory = "Enemies";
    public const string TraversalCategory = "Traversal";
    public const string FlowCategory = "Flow & Pickups";

    // Bölüm editörünün yeni bölümde kullandığı parça adları
    public const string GroundPiece = "Ground";
    public const string SafeZonePiece = "SafeZone";
    public const string GoalPiece = "Goal";
    public const string KillZonePiece = "KillZone";

    private struct Piece
    {
        public string category, name;
        public Action<Transform, U.Layers> build;
    }

    private static Piece[] Pieces => new[]
    {
        P(GroundCategory, GroundPiece, (r, l) => U.Ground(r, l.Ground, "Ground", -5f, 5f)),
        P(GroundCategory, "Wall", (r, l) => U.Block(r, l.Ground, "Wall", -0.5f, 0f, 1f, 4f)),
        P(GroundCategory, "ThinWall", (r, l) =>
            U.Colorize(U.Block(r, l.Phaseable, "ThinWall", -0.25f, 0f, 0.5f, 4f), PrototypeLevelsBuilder.ThinWallColor)),
        P(GroundCategory, "Breakable", (r, l) => B.BreakableBlock(r, l.Ground, "Breakable", -0.5f, 0f, 1f, 2f)),
        P(GroundCategory, "MetalCeiling", (r, l) =>
        {
            GameObject ceiling = U.Block(r, l.Ground, "MetalCeiling", -5f, 5f, 10f, 1f);
            U.Colorize(ceiling, C.SteelColor);
            ceiling.AddComponent<MetalCeiling>();
        }),
        P(GroundCategory, "MetalWall", (r, l) => C.MetalWallBetween(r, "MetalWall", -5f, 5f, 0f, 7f)),

        P(EnemiesCategory, "Turret", (r, l) => DE.Turret(r, l, "Turret", 0f, DE.TurretBullet(l))),
        P(EnemiesCategory, "Drone", (r, l) => DE.Drone(r, l, "Drone", 0f, 5f, DE.DroneBullet(l), 10f)),

        P(TraversalCategory, "LaunchPad", (r, l) => DT.LaunchPadAt(r, "LaunchPad", -0.75f)),
        // Düz başlar: uzatmak için kökün X ölçeği, eğim için kökün Z dönüşü değiştirilir
        P(TraversalCategory, "Zipline", (r, l) => DT.ZiplineBetween(r, "Zipline", new Vector3(-5f, 4f, 0f), new Vector3(5f, 4f, 0f))),
        P(TraversalCategory, "SlingZone", (r, l) => DT.SlingZoneAt(r, "SlingZone", new Vector3(0f, 4f, 0f))),
        P(TraversalCategory, "HookPoint", (r, l) => C.HookPointAt(r, "HookPoint", new Vector3(0f, 5f, 0f))),
        P(TraversalCategory, "Battery", (r, l) => C.BatteryAt(r, "Battery", new Vector3(0f, 2f, 0f))),
        P(TraversalCategory, "TravelBubble", (r, l) => DT.BubbleAt(r, "TravelBubble", new Vector3(0f, 1.5f, 0f),
            new[] { new Vector3(6f, 5f, 0f), new Vector3(12f, 3f, 0f) }, new Vector3(1f, 0.5f, 0f), 14f)),

        P(FlowCategory, SafeZonePiece, (r, l) => U.SafeZoneAt(r, "SafeZone", 0f, false)),
        P(FlowCategory, GoalPiece, (r, l) => LevelPieces.Goal(r, 0f)),
        P(FlowCategory, KillZonePiece, (r, l) => U.KillZone(r, 0f, 40f)),
        // Kimlik boş: bölüm editörü parçayı yerleştirirken atar
        P(FlowCategory, "Kumas", (r, l) => LevelPieces.Kumas(r, "", new Vector3(0f, 1.2f, 0f))),
        P(FlowCategory, "Belge", (r, l) => LevelPieces.Belge(r, "", new Vector3(0f, 1.5f, 0f)))
    };

    private static Piece P(string category, string name, Action<Transform, U.Layers> build)
    {
        return new Piece { category = category, name = name, build = build };
    }

    public static string PrefabPath(string category, string piece) => $"{KitRoot}/{category}/{piece}.prefab";

    [MenuItem("KosKos/Level Kit/Build Kit Prefabs")]
    public static void BuildKit()
    {
        U.Layers layers = U.EnsureLayers();
        U.EnsureFolder("Assets", "LevelKit");

        Scene preview = EditorSceneManager.NewPreviewScene();
        int created = 0;
        try
        {
            foreach (Piece piece in Pieces)
            {
                string path = PrefabPath(piece.category, piece.name);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) continue;

                U.EnsureFolder(KitRoot, piece.category);
                // Kök geçici sahnede; yardımcıların açık sahnede yarattığı nesneler köke bağlanınca oraya taşınır
                var root = new GameObject(piece.name);
                SceneManager.MoveGameObjectToScene(root, preview);
                piece.build(root.transform, layers);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                UnityEngine.Object.DestroyImmediate(root);
                created++;
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[KosKos] Bölüm kiti: {created} yeni parça prefab'ı üretildi ({KitRoot}). Var olanlara dokunulmadı.");
    }
}
