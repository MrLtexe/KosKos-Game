using TMPro;
using UnityEditor;
using UnityEngine;

// Yazı tipini tüm arayüze tek seferde uygular: KosKos > Apply UI Font.
// Seçilen TextMeshPro yazı tipi; SettingsPanel, MainMenuUI ve LevelUI prefab'larındaki tüm yazılara verilir.
// Kodla kurulan hile paneli ana menü başlığının yazı tipini kullandığı için o da uyar.
// Yeni yazı tipi: .ttf dosyasına sağ tık > Create > TextMeshPro > Font Asset; Türkçe harfler için "Atlas Population Mode = Dynamic" olmalı.
public class UiFontTool : EditorWindow
{
    // İç içe prefab önce: SettingsPanel güncellenince diğerlerindeki kopyaları da güncel olur
    private static readonly string[] PrefabPaths =
    {
        UiPrefabBuilder.SettingsPath,
        UiPrefabBuilder.MainMenuPath,
        UiPrefabBuilder.LevelUiPath
    };

    private TMP_FontAsset font;

    [MenuItem("KosKos/Apply UI Font")]
    private static void Open()
    {
        GetWindow<UiFontTool>(true, "Apply UI Font").minSize = new Vector2(360f, 120f);
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Seçilen yazı tipi tüm UI prefab'larındaki yazılara uygulanır.", MessageType.Info);
        font = (TMP_FontAsset)EditorGUILayout.ObjectField("Font Asset", font, typeof(TMP_FontAsset), false);

        using (new EditorGUI.DisabledScope(font == null))
        {
            if (GUILayout.Button("Apply")) Apply(font);
        }
    }

    private static void Apply(TMP_FontAsset font)
    {
        if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic)
        {
            Debug.LogWarning($"[KosKos] '{font.name}' dinamik değil: listesinde olmayan harfler (ör. ğ, ş, ı) görünmeyebilir.");
        }

        int changed = 0;
        foreach (string path in PrefabPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font == font) continue;
                text.font = font;
                changed++;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[KosKos] Yazı tipi '{font.name}' uygulandı ({changed} yazı).");
    }
}
