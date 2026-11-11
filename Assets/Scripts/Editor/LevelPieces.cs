using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using U = TestSceneBuilderUtils;

// Bölüm parçalarının ortak yapıcıları: Kumaş, Belge, bölüm sonu (hedef) ve bölüm sonu diyaloğu.
// Prototip bölüm builder'ı ve bölüm editörü (Level Kit) ikisi de bunları kullanır; ikisinde de çıktı aynıdır.
public static class LevelPieces
{
    // Bölüm sonu diyaloğu (Yunus'un diyalog sistemi)
    public const string DialoguePrefabPath = "Assets/Prefabs/UI/C_Dialogue.prefab";
    public const string DefaultDialogueDataPath = "Assets/Scripts/Dialogue/Data/DialogueData0.asset";

    private static readonly Color KumasColor = new Color(0.6f, 0.25f, 0.9f);
    private static readonly Color BelgeColor = Color.white;
    private static readonly Color GoalColor = new Color(0.2f, 0.85f, 0.3f);
    private static readonly Color FlagColor = new Color(1f, 0.9f, 0.2f);

    // Mor, dönen para (silindir kameraya bakar). id boş olabilir (kit prefab'ı; yerleştirilince atanır)
    public static GameObject Kumas(Transform parent, string id, Vector3 position, string name = "Kumas")
    {
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = name;
        coin.transform.SetParent(parent);
        coin.transform.position = position;
        coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        coin.transform.localScale = new Vector3(0.6f, 0.08f, 0.6f);
        coin.GetComponent<Collider>().isTrigger = true;
        SetupCollectible(coin, Collectible.Kind.Kumas, id, KumasColor, 120f);
        return coin;
    }

    // Beyaz, havada duran sayfa
    public static GameObject Belge(Transform parent, string id, Vector3 position)
    {
        GameObject page = GameObject.CreatePrimitive(PrimitiveType.Cube);
        page.name = "Belge";
        page.transform.SetParent(parent);
        page.transform.position = position;
        page.transform.localScale = new Vector3(0.6f, 0.8f, 0.05f);
        page.GetComponent<Collider>().isTrigger = true;
        SetupCollectible(page, Collectible.Kind.Belge, id, BelgeColor, 0f);
        return page;
    }

    public static void SetCollectibleId(Collectible collectible, string id)
    {
        var so = new SerializedObject(collectible);
        so.FindProperty("id").stringValue = id;
        so.ApplyModifiedProperties();
    }

    private static void SetupCollectible(GameObject go, Collectible.Kind kind, string id, Color color, float spin)
    {
        var collectible = go.AddComponent<Collectible>();
        var so = new SerializedObject(collectible);
        so.FindProperty("kind").enumValueIndex = (int)kind;
        so.FindProperty("id").stringValue = id;
        so.FindProperty("color").colorValue = color;
        so.FindProperty("spinSpeed").floatValue = spin;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Bölüm sonu: yüksek görünmez tetik + yeşil direk ve sarı bayrak. Kökü (LevelGoal) döndürür
    public static GameObject Goal(Transform parent, float x)
    {
        var goal = new GameObject("LevelGoal");
        goal.transform.SetParent(parent);
        goal.transform.position = new Vector3(x, 10f, 0f);
        var trigger = goal.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(2f, 20f, U.LevelDepth);
        goal.AddComponent<LevelGoal>();

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pole.name = "Pole";
        Object.DestroyImmediate(pole.GetComponent<Collider>());
        pole.transform.SetParent(goal.transform);
        pole.transform.position = new Vector3(x, 3f, 0f);
        pole.transform.localScale = new Vector3(0.3f, 6f, 0.3f);
        U.Colorize(pole, GoalColor);

        GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flag.name = "Flag";
        Object.DestroyImmediate(flag.GetComponent<Collider>());
        flag.transform.SetParent(goal.transform);
        flag.transform.position = new Vector3(x + 0.8f, 5.4f, 0f);
        flag.transform.localScale = new Vector3(1.4f, 0.9f, 0.1f);
        U.Colorize(flag, FlagColor);
        return goal;
    }

    // C_Dialogue prefab'ını sahneye koyar. keepTestTrigger kapalıysa T tuşlu test tetikleyicisi kaldırılır.
    // Prefab yoksa uyarı verip null döndürür
    public static GameObject AddDialogueObject(Scene scene, bool keepTestTrigger)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialoguePrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[KosKos] Diyalog prefab'ı bulunamadı: {DialoguePrefabPath}; sahne diyalogsuz kuruldu.");
            return null;
        }

        var dialogue = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        if (!keepTestTrigger)
        {
            foreach (DialogueTestTrigger trigger in dialogue.GetComponentsInChildren<DialogueTestTrigger>(true))
            {
                Object.DestroyImmediate(trigger);
            }
        }
        return dialogue;
    }
}
