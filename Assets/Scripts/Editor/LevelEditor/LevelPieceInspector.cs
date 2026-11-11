using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Bölüm editörü: seçili kit parçasının sade ayarları (uzunluk, eğim, genişlik, taret/dron değerleri...).
// Boyutlar parçanın DIŞ (kök) nesnesinin ölçeğiyle değişir — elle ölçeklemekle aynı, parçanın içi bozulmaz.
// Tüm değişiklikler Ctrl+Z ile geri alınır; kit prefab'larına dokunulmaz (sadece bu sahnedeki parça değişir).
public static class LevelPieceInspector
{
    // Kit parçasının ölçek 1'deki boyutu (LevelKitBuilder'daki ölçülerle aynı). height 0 = yükseklik ayarı yok
    private struct BaseSize
    {
        public float width, height;
        public BaseSize(float w, float h) { width = w; height = h; }
    }

    private static readonly Dictionary<string, BaseSize> Sizes = new Dictionary<string, BaseSize>
    {
        { "Ground", new BaseSize(10f, 1f) },
        { "Wall", new BaseSize(1f, 4f) },
        { "ThinWall", new BaseSize(0.5f, 4f) },
        { "Breakable", new BaseSize(1f, 2f) },
        // Tavan zeminden 5 yukarıda: yükseklik ölçeği onu da yukarı iterdi; tavanın yerini taşıyarak ayarlayın
        { "MetalCeiling", new BaseSize(10f, 0f) },
        { "MetalWall", new BaseSize(10f, 7f) },
        { "KillZone", new BaseSize(40f, 0f) }
    };

    private const float ZiplineBaseLength = 10f;

    public static void Draw()
    {
        EditorGUILayout.LabelField("Selected piece", EditorStyles.boldLabel);

        GameObject root = SelectedPieceRoot(out string piece);
        if (root == null)
        {
            EditorGUILayout.HelpBox("Select a piece in the scene to edit it.", MessageType.None);
            return;
        }

        EditorGUILayout.LabelField(piece, root.name);
        EditorGUI.indentLevel++;
        if (piece == "Zipline") DrawZipline(root.transform);
        else if (Sizes.TryGetValue(piece, out BaseSize size)) DrawSize(root.transform, size);
        else if (piece == "Turret") DrawTurret(root);
        else if (piece == "Drone") DrawDrone(root);
        else if (piece == "SafeZone") DrawSafeZone(root);
        else if (piece == "Kumas" || piece == "Belge") DrawCollectible(root);
        else EditorGUILayout.LabelField("Position only — move it with the Move tool (W).");
        EditorGUI.indentLevel--;
    }

    // Seçili nesnenin (ya da üstündeki) kit parçası kökü ve parçanın adı (kit prefab'ının dosya adı). Kit parçası değilse null
    private static GameObject SelectedPieceRoot(out string piece)
    {
        piece = null;
        if (Selection.gameObjects.Length != 1) return null;

        GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(Selection.activeGameObject);
        if (root == null) return null;
        string path = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(root));
        if (string.IsNullOrEmpty(path) || !path.StartsWith(LevelKitBuilder.KitRoot + "/")) return null;

        piece = System.IO.Path.GetFileNameWithoutExtension(path);
        return root;
    }

    private static void DrawZipline(Transform t)
    {
        float length = ZiplineBaseLength * t.localScale.x;
        float slope = Mathf.DeltaAngle(0f, t.localEulerAngles.z);

        EditorGUI.BeginChangeCheck();
        length = EditorGUILayout.FloatField(new GUIContent("Length", "Hattın uzunluğu (birim)."), length);
        slope = EditorGUILayout.Slider(new GUIContent("Slope (°)", "Eğim. Pozitif = sağa doğru yükselir, negatif = alçalır."), slope, -45f, 45f);
        if (!EditorGUI.EndChangeCheck()) return;

        Undo.RecordObject(t, "Edit zipline");
        t.localScale = new Vector3(Mathf.Max(1f, length) / ZiplineBaseLength, 1f, 1f);
        t.localRotation = Quaternion.Euler(0f, 0f, slope);
    }

    private static void DrawSize(Transform t, BaseSize size)
    {
        float width = size.width * t.localScale.x;
        float height = size.height * t.localScale.y;

        EditorGUI.BeginChangeCheck();
        width = EditorGUILayout.FloatField(new GUIContent("Width", "Genişlik (birim)."), width);
        if (size.height > 0f) height = EditorGUILayout.FloatField(new GUIContent("Height", "Yükseklik (birim). Alt kenar yerinde kalır."), height);
        if (!EditorGUI.EndChangeCheck()) return;

        Undo.RecordObject(t, "Edit piece size");
        float sx = Mathf.Max(0.1f, width) / size.width;
        float sy = size.height > 0f ? Mathf.Max(0.1f, height) / size.height : t.localScale.y;
        t.localScale = new Vector3(sx, sy, t.localScale.z);
    }

    private static void DrawTurret(GameObject root)
    {
        var shooter = root.GetComponentInChildren<EnemyShooter>(true);
        if (shooter == null) return;
        var so = new SerializedObject(shooter);
        Field(so, "range", "Range", "Oyuncu bu mesafeye girince ateşe başlar (birim). Büyük = daha çok atış.");
        Field(so, "windUpTime", "Aim time (s)", "Her atıştan önceki nişan süresi (sn).");
        Field(so, "cooldownTime", "Gap between shots (s)", "Atıştan sonra bir sonraki nişana kadar bekleme (sn).");
        Field(so, "bulletSpeed", "Bullet speed", "Merminin hızı (birim/sn).");
        so.ApplyModifiedProperties();
    }

    private static void DrawDrone(GameObject root)
    {
        var shooter = root.GetComponentInChildren<EnemyShooter>(true);
        if (shooter == null) return;

        // Yükseklik: dron nesnesinin parça kökünün üstündeki konumu (kök = zemin çizgisi)
        Transform drone = shooter.transform;
        EditorGUI.BeginChangeCheck();
        float height = EditorGUILayout.FloatField(new GUIContent("Height above ground", "Dronun zeminden yüksekliği (birim). En az 4.5: yerdeki oyuncu altından geçebilsin."),
            drone.localPosition.y);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(drone, "Edit drone height");
            drone.localPosition = new Vector3(drone.localPosition.x, Mathf.Max(4.5f, height), drone.localPosition.z);
        }

        var so = new SerializedObject(shooter);
        Field(so, "range", "Range", "Oyuncu bu mesafeye girince ateşe başlar (birim).");
        so.ApplyModifiedProperties();

        var hover = root.GetComponentInChildren<DroneHover>(true);
        if (hover == null) return;
        var hoverSo = new SerializedObject(hover);
        SerializedProperty patrol = hoverSo.FindProperty("patrolOffset");
        EditorGUI.BeginChangeCheck();
        float distance = EditorGUILayout.FloatField(new GUIContent("Patrol distance", "Devriye mesafesi (birim, sağa doğru). 0 = yerinde durur."), patrol.vector3Value.x);
        if (EditorGUI.EndChangeCheck()) patrol.vector3Value = new Vector3(distance, 0f, 0f);
        hoverSo.ApplyModifiedProperties();
    }

    // Başlangıç alanı tek olmalı: biri işaretlenince diğerlerinin işareti kalkar
    private static void DrawSafeZone(GameObject root)
    {
        var zone = root.GetComponentInChildren<SafeZone>(true);
        if (zone == null) return;
        var so = new SerializedObject(zone);
        SerializedProperty isStart = so.FindProperty("isStartZone");

        EditorGUI.BeginChangeCheck();
        bool value = EditorGUILayout.Toggle(new GUIContent("Start zone", "Bölüm buradan başlar. Bölümde tek başlangıç alanı olur."), isStart.boolValue);
        if (!EditorGUI.EndChangeCheck()) return;

        if (value)
        {
            foreach (SafeZone other in Object.FindObjectsByType<SafeZone>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other == zone) continue;
                var otherSo = new SerializedObject(other);
                otherSo.FindProperty("isStartZone").boolValue = false;
                otherSo.ApplyModifiedProperties();
            }
        }
        isStart.boolValue = value;
        so.ApplyModifiedProperties();
    }

    private static void DrawCollectible(GameObject root)
    {
        var collectible = root.GetComponentInChildren<Collectible>(true);
        if (collectible == null) return;
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField(new GUIContent("ID", "Kayıt dosyasının kullandığı kalıcı kimlik; otomatik verilir, değiştirilmez."), collectible.Id);
        }
    }

    private static void Field(SerializedObject so, string property, string label, string tooltip)
    {
        SerializedProperty p = so.FindProperty(property);
        if (p != null) EditorGUILayout.PropertyField(p, new GUIContent(label, tooltip));
    }
}
