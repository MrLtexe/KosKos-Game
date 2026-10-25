using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tek tıkla ana menü sahnesini üretir: KosKos > Build Main Menu.
// Sahneye MainMenuUI prefab'ı konur (prefab yoksa önce üretilir); menünün düzeni prefab'da düzenlenir, sahne yeniden üretilse de korunur.
public static class MainMenuBuilder
{
    [MenuItem("KosKos/Build Main Menu")]
    private static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        // Prefab'lar yeni sahne açıldıktan sonra üretilir/yüklenir
        UiPrefabBuilder.EnsurePrefabs();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabBuilder.MainMenuPath);
        PrefabUtility.InstantiatePrefab(prefab, scene);

        EditorSceneManager.SaveScene(scene, PrototypeLevelsBuilder.MenuScenePath);
        Debug.Log($"[KosKos] Ana menü oluşturuldu: {PrototypeLevelsBuilder.MenuScenePath}");
        PrototypeLevelsBuilder.UpdateBuildSettings();
    }
}
