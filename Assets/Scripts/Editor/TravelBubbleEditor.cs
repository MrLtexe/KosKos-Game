using UnityEditor;
using UnityEngine;

// Yolculuk baloncuğu için Scene düzenleyicisi: baloncuk seçiliyken her ara nokta ve fırlatma noktası için
// Scene görünümünde etiketli taşıma tutamacı çıkar (çocuk nesneleri tek tek seçmeye gerek yok).
// Inspector'a ara nokta ekleme / son ara noktayı silme butonları ekler. Tüm işlemler Ctrl+Z ile geri alınabilir.
[CustomEditor(typeof(TravelBubble))]
public class TravelBubbleEditor : Editor
{
    // Yeni ara nokta, son noktanın bu kadar ilerisine eklenir (birim)
    private const float NewWaypointOffset = 3f;
    private const float LabelOffset = 0.6f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Scene görünümünde ara noktaları ve fırlatma noktasını tutamaçlarla sürükleyin.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Waypoint")) AddWaypoint();
        if (GUILayout.Button("Remove Last Waypoint")) RemoveLastWaypoint();
        EditorGUILayout.EndHorizontal();
    }

    private void OnSceneGUI()
    {
        serializedObject.Update();
        SerializedProperty waypoints = serializedObject.FindProperty("waypoints");

        for (int i = 0; i < waypoints.arraySize; i++)
        {
            var waypoint = waypoints.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
            if (waypoint != null) MoveHandle(waypoint, $"Waypoint {i + 1}");
        }

        var eject = serializedObject.FindProperty("ejectDirection").objectReferenceValue as Transform;
        if (eject != null) MoveHandle(eject, "Eject");
    }

    // Tutamaçla taşıma; oyun düzleminde kalsın diye z = 0
    private static void MoveHandle(Transform point, string label)
    {
        Handles.Label(point.position + Vector3.up * LabelOffset, label);

        EditorGUI.BeginChangeCheck();
        Vector3 position = Handles.PositionHandle(point.position, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(point, "Move " + label);
            point.position = new Vector3(position.x, position.y, 0f);
        }
    }

    private void AddWaypoint()
    {
        var bubble = (TravelBubble)target;
        serializedObject.Update();
        SerializedProperty waypoints = serializedObject.FindProperty("waypoints");

        // Yeni nokta son noktanın (yoksa baloncuğun) biraz ilerisine
        Vector3 from = bubble.transform.position;
        if (waypoints.arraySize > 0)
        {
            var last = waypoints.GetArrayElementAtIndex(waypoints.arraySize - 1).objectReferenceValue as Transform;
            if (last != null) from = last.position;
        }

        var waypoint = new GameObject($"Waypoint_{waypoints.arraySize + 1}").transform;
        Undo.RegisterCreatedObjectUndo(waypoint.gameObject, "Add Waypoint");
        Undo.SetTransformParent(waypoint, bubble.transform, "Add Waypoint");
        waypoint.position = from + Vector3.right * NewWaypointOffset;

        waypoints.arraySize++;
        waypoints.GetArrayElementAtIndex(waypoints.arraySize - 1).objectReferenceValue = waypoint;
        serializedObject.ApplyModifiedProperties();
    }

    private void RemoveLastWaypoint()
    {
        serializedObject.Update();
        SerializedProperty waypoints = serializedObject.FindProperty("waypoints");
        if (waypoints.arraySize == 0) return;

        var last = waypoints.GetArrayElementAtIndex(waypoints.arraySize - 1).objectReferenceValue as Transform;
        waypoints.arraySize--;
        serializedObject.ApplyModifiedProperties();
        if (last != null) Undo.DestroyObjectImmediate(last.gameObject);
    }
}
