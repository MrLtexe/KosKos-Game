using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using U = TestSceneBuilderUtils;

// Tek tıkla ana menü sahnesini üretir: KosKos > Build Main Menu.
// Menü ekranları oyun çalışırken kodla oluşturulur; sahnede sadece MainMenu nesnesi vardır.
public static class MainMenuBuilder
{
    [MenuItem("KosKos/Build Main Menu")]
    private static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        // Asset'ler yeni sahne açıldıktan sonra yüklenir (açılışta bellekten atılmasınlar)
        LevelCatalog catalog = PrototypeLevelsBuilder.GetOrCreateCatalog();
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(U.InputAssetPath);

        var menu = new GameObject("MainMenu").AddComponent<MainMenu>();
        U.SetField(menu, "catalog", catalog);
        U.SetField(menu, "actions", actions);

        EditorSceneManager.SaveScene(scene, PrototypeLevelsBuilder.MenuScenePath);
        Debug.Log($"[KosKos] Ana menü oluşturuldu: {PrototypeLevelsBuilder.MenuScenePath}");
        PrototypeLevelsBuilder.UpdateBuildSettings();
    }
}
