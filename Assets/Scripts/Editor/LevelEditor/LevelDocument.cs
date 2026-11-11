using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using U = TestSceneBuilderUtils;

// Bölüm editörünün formu: açık sahnenin bölüm ayarlarını okur, doğrular ve kaydeder.
// Yeni bir veri biçimi yoktur; değerler zaten var olan yerlere yazılır:
// LevelSettings (kimlik, madalya süreleri, loadout), LevelGoal (bölüm sonu diyaloğu), {id}_Loadout.asset ve LevelCatalog girişi.
public class LevelDocument
{
    public string id, sceneName;
    public string nameEn, nameTr, belgeTitleEn, belgeTitleTr, belgeTextEn, belgeTextTr;
    public float silverTime, goldTime;
    public bool doubleJump, phase, parry, hook, jetBag, magBoots;
    public DialogueData endDialogue;

    // Henüz kaydedilmemiş yeni bölüm mü (kaydedilince sıradaki Level_NN olur)
    public bool IsNew { get; private set; }
    // Prototip bölümler builder'a aittir: "Build Prototype Levels" bu bölümlerdeki düzenlemelerin üzerine yazar
    public bool IsPrototype => id == "L1" || id == "L2" || id == "L3";

    public struct Message
    {
        public string text;
        public bool isError;
    }

    // Yeni bölüm için varsayılan madalya süreleri (sn)
    private const float DefaultSilver = 60f;
    private const float DefaultGold = 45f;
    private static readonly Regex LevelScenePattern = new Regex(@"^Level_(\d+)$");

    // Bölüm sahnesi değilse null. Bölüm = LevelSettings + LevelRun; test dünyası / test sahnelerinde de LevelSettings vardır
    // ama LevelRun yoktur (yoksa test dünyası "kaydedilmemiş yeni bölüm" sanılıp Level_NN diye kaydedilebilirdi)
    public static LevelDocument FromActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        LevelSettings settings = Find<LevelSettings>(scene).FirstOrDefault();
        if (settings == null || !Find<LevelRun>(scene).Any()) return null;

        var doc = new LevelDocument { IsNew = string.IsNullOrEmpty(settings.LevelId) };
        LevelCatalog catalog = PrototypeLevelsBuilder.GetOrCreateCatalog();

        if (doc.IsNew)
        {
            (doc.id, doc.sceneName) = NextFreeLevel();
            doc.silverTime = DefaultSilver;
            doc.goldTime = DefaultGold;
            // Başlangıç: bir önceki bölümün yetenekleri (tasarımcı sonra tikler / kaldırır)
            LevelCatalog.Entry last = catalog.Entries.Count > 0 ? catalog.Entries[catalog.Entries.Count - 1] : null;
            if (last != null) doc.ReadLoadout(AssetDatabase.LoadAssetAtPath<AbilityLoadout>(LoadoutPath(last.id)));
            doc.endDialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(LevelPieces.DefaultDialogueDataPath);
            return doc;
        }

        doc.id = settings.LevelId;
        doc.sceneName = scene.name;
        doc.silverTime = settings.SilverTime;
        doc.goldTime = settings.GoldTime;
        doc.ReadLoadout(settings.Loadout);

        LevelGoal goal = Find<LevelGoal>(scene).FirstOrDefault();
        if (goal != null) doc.endDialogue = new SerializedObject(goal).FindProperty("dialogueData").objectReferenceValue as DialogueData;

        int index = catalog.IndexOf(doc.id);
        if (index >= 0)
        {
            LevelCatalog.Entry e = catalog.Entries[index];
            doc.nameEn = e.displayName;
            doc.nameTr = e.displayNameTr;
            doc.belgeTitleEn = e.belgeTitle;
            doc.belgeTitleTr = e.belgeTitleTr;
            doc.belgeTextEn = e.belgeText;
            doc.belgeTextTr = e.belgeTextTr;
        }
        return doc;
    }

    // Sıradaki bölüm: katalogdaki en büyük Level_NN numarası + 1 → ("L5", "Level_05")
    public static (string id, string sceneName) NextFreeLevel()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(PrototypeLevelsBuilder.CatalogPath);
        int highest = 0;
        if (catalog != null)
        {
            foreach (LevelCatalog.Entry e in catalog.Entries)
            {
                Match m = LevelScenePattern.Match(e.sceneName ?? "");
                if (m.Success && int.TryParse(m.Groups[1].Value, out int n) && n > highest) highest = n;
            }
        }
        int next = highest + 1;
        return ("L" + next, $"Level_{next:00}");
    }

    public List<Message> Validate(out bool hasErrors)
    {
        Scene scene = SceneManager.GetActiveScene();
        var messages = new List<Message>();

        int startZones = Find<SafeZone>(scene).Count(z => new SerializedObject(z).FindProperty("isStartZone").boolValue);
        if (startZones != 1) messages.Add(Error("Level needs exactly one start SafeZone"));
        if (Find<LevelGoal>(scene).Count() != 1) messages.Add(Error("Level needs exactly one Goal"));
        if (silverTime <= 0f || goldTime <= 0f || goldTime >= silverTime)
            messages.Add(Error("Medal times: gold must be faster than silver, both above 0"));
        if (string.IsNullOrWhiteSpace(nameEn)) messages.Add(Error("Level needs an English name"));
        if (IsNew && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            messages.Add(Error($"{ScenePath} already exists"));

        Collectible[] collectibles = LevelSceneFactory.Collectibles(scene);
        if (collectibles.Any(c => string.IsNullOrEmpty(c.Id)))
            messages.Add(Error("Pickup without an ID (press Fix IDs)"));
        if (collectibles.Where(c => !string.IsNullOrEmpty(c.Id)).GroupBy(c => c.Id).Any(g => g.Count() > 1))
            messages.Add(Error("Duplicate pickup IDs (press Fix IDs)"));

        int belges = collectibles.Count(c => c.Type == Collectible.Kind.Belge);
        if (belges == 0) messages.Add(Warning("No Belge in this level"));
        if (belges > 1) messages.Add(Warning("More than one Belge"));

        hasErrors = messages.Any(m => m.isError);
        return messages;
    }

    // Aynı kimliği taşıyan kopyalarda ilki (hiyerarşi sırasıyla) korunur, diğerleri yeni kimlik alır; kimliksizler de kimlik alır.
    // Fazla Belge'nin kimliği boşaltılır (bölümde tek Belge olur). Değişen sayıyı döndürür; geri alınabilir
    public int FixDuplicateIds()
    {
        Scene scene = SceneManager.GetActiveScene();
        var seen = new HashSet<string>();
        int changed = 0;
        foreach (Collectible c in LevelSceneFactory.Collectibles(scene))
        {
            bool needsId = string.IsNullOrEmpty(c.Id) || !seen.Add(c.Id);
            if (!needsId) continue;

            Undo.RecordObject(c, "Fix pickup IDs");
            if (c.Type == Collectible.Kind.Kumas)
            {
                string newId = LevelSceneFactory.NextKumasId(scene, id);
                LevelPieces.SetCollectibleId(c, newId);
                seen.Add(newId);
            }
            else if (!seen.Contains(LevelSceneFactory.BelgeId(id)))
            {
                LevelPieces.SetCollectibleId(c, LevelSceneFactory.BelgeId(id));
                seen.Add(c.Id);
            }
            else
            {
                Debug.LogWarning($"[KosKos] Fazla Belge: {c.name}. Bir bölümde tek Belge olur; bunu silin.", c);
                continue;
            }
            changed++;
        }
        return changed;
    }

    // Test için: formdaki süreleri ve diyaloğu sahneye yazar (kaydetmeden). Kayıtlı bölümde loadout asset'i de güncellenir;
    // kaydedilmemiş yeni bölüm test loadout'uyla (her şey açık) oynar
    public void ApplyToScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        LevelSettings settings = Find<LevelSettings>(scene).First();
        var so = new SerializedObject(settings);
        so.FindProperty("silverTime").floatValue = silverTime;
        so.FindProperty("goldTime").floatValue = goldTime;
        if (!IsNew) so.FindProperty("loadout").objectReferenceValue = WriteLoadout();
        so.ApplyModifiedProperties();

        LevelGoal goal = Find<LevelGoal>(scene).FirstOrDefault();
        if (goal != null)
        {
            var goalSo = new SerializedObject(goal);
            goalSo.FindProperty("dialogueData").objectReferenceValue = endDialogue;
            goalSo.ApplyModifiedProperties();
        }
    }

    // Doğrulamada hata varsa false. Yeni bölüm Level_NN olarak kaydedilir, kataloğun sonuna eklenir ve Build Settings'e girer
    public bool Save()
    {
        Validate(out bool hasErrors);
        if (hasErrors) return false;

        Scene scene = SceneManager.GetActiveScene();
        LevelCatalog catalog = PrototypeLevelsBuilder.GetOrCreateCatalog();

        LevelSettings settings = Find<LevelSettings>(scene).First();
        var so = new SerializedObject(settings);
        so.FindProperty("levelId").stringValue = id;
        so.FindProperty("catalog").objectReferenceValue = catalog;
        so.ApplyModifiedProperties();
        // Süreler, diyalog ve loadout (kimlik artık dolu: yeni bölümün loadout asset'i de oluşturulur)
        IsNew = false;
        ApplyToScene();

        if (!EditorSceneManager.SaveScene(scene, ScenePath)) return false;

        var catalogSo = new SerializedObject(catalog);
        SerializedProperty entries = catalogSo.FindProperty("entries");
        int index = catalog.IndexOf(id);
        if (index < 0)
        {
            index = entries.arraySize;
            entries.arraySize++;
        }
        PrototypeLevelsBuilder.WriteEntry(entries.GetArrayElementAtIndex(index), id, nameEn, sceneName,
            belgeTitleEn, belgeTextEn, nameTr, belgeTitleTr, belgeTextTr);
        catalogSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        PrototypeLevelsBuilder.UpdateBuildSettings();
        Debug.Log($"[KosKos] Bölüm kaydedildi: {sceneName} ({id}).");
        return true;
    }

    private string ScenePath => $"Assets/Scenes/{sceneName}.unity";

    private static string LoadoutPath(string levelId) => $"Assets/Loadouts/{levelId}_Loadout.asset";

    private void ReadLoadout(AbilityLoadout loadout)
    {
        if (loadout == null) return;
        doubleJump = loadout.DoubleJump;
        phase = loadout.Phase;
        parry = loadout.Parry;
        hook = loadout.Hook;
        jetBag = loadout.JetBag;
        magBoots = loadout.MagBoots;
    }

    private AbilityLoadout WriteLoadout()
    {
        string path = LoadoutPath(id);
        var loadout = AssetDatabase.LoadAssetAtPath<AbilityLoadout>(path);
        if (loadout == null)
        {
            U.EnsureFolder("Assets", "Loadouts");
            loadout = ScriptableObject.CreateInstance<AbilityLoadout>();
            AssetDatabase.CreateAsset(loadout, path);
        }

        var so = new SerializedObject(loadout);
        so.FindProperty("doubleJump").boolValue = doubleJump;
        so.FindProperty("phase").boolValue = phase;
        so.FindProperty("parry").boolValue = parry;
        so.FindProperty("hook").boolValue = hook;
        so.FindProperty("jetBag").boolValue = jetBag;
        so.FindProperty("magBoots").boolValue = magBoots;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(loadout);
        AssetDatabase.SaveAssets();
        return loadout;
    }

    private static IEnumerable<T> Find<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true));
    }

    private static Message Error(string text) => new Message { text = text, isError = true };
    private static Message Warning(string text) => new Message { text = text, isError = false };
}
