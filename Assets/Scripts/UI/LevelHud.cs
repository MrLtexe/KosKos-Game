using TMPro;
using UnityEngine;

// Bölüm içi gösterge (LevelUI prefab'ı, sol üst): süre, ölüm sayısı, Kumaş ve Belge sayıları.
// Düzen ve görünüm prefab'da düzenlenir; bu kod sadece yazıları doldurur.
public class LevelHud : MonoBehaviour
{
    [Tooltip("Göstergenin kök nesnesi (bölüm bitince gizlenir).")]
    [SerializeField] private GameObject root;

    [Tooltip("Süre yazısı.")]
    [SerializeField] private TMP_Text timeText;

    [Tooltip("Ölüm sayısı yazısı.")]
    [SerializeField] private TMP_Text deathsText;

    [Tooltip("Kumaş sayısı yazısı.")]
    [SerializeField] private TMP_Text fabricText;

    [Tooltip("Belge sayısı yazısı.")]
    [SerializeField] private TMP_Text documentText;

    // Son gösterilen değerler: yazılar sadece değişince yeniden oluşturulur (her karede dize üretilmesin)
    private int shownTenths = -1;
    private int shownDeaths = -1;
    private int shownKumas = -1;
    private int shownBelge = -1;

    private void OnEnable()
    {
        Loc.LanguageChanged += ForceRefresh;
    }

    private void OnDisable()
    {
        Loc.LanguageChanged -= ForceRefresh;
    }

    // Dil değişince bir sonraki karede tüm yazılar yeniden yazılır
    private void ForceRefresh()
    {
        shownTenths = shownDeaths = shownKumas = shownBelge = -1;
    }

    private void Update()
    {
        LevelRun run = LevelRun.Current;
        if (run == null) return;

        if (run.IsFinished)
        {
            if (root.activeSelf) root.SetActive(false);
            return;
        }

        int tenths = (int)(run.Elapsed * 10f);
        if (tenths != shownTenths)
        {
            shownTenths = tenths;
            timeText.text = FormatTime(run.Elapsed);
        }
        if (run.Deaths != shownDeaths)
        {
            shownDeaths = run.Deaths;
            deathsText.text = Loc.Format("hud.deaths", run.Deaths);
        }
        int kumas = run.KumasCount;
        if (kumas != shownKumas)
        {
            shownKumas = kumas;
            fabricText.text = Loc.Format("hud.fabric", kumas, run.KumasTotal);
        }
        int belge = run.BelgeCount;
        if (belge != shownBelge)
        {
            shownBelge = belge;
            documentText.text = Loc.Format("hud.document", belge, run.BelgeTotal);
        }
    }

    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) return "--:--";
        // Onda birlerden hesaplanır; 59.97 gibi değerler "00:60.0" görünmesin
        int tenths = (int)(seconds * 10f);
        return $"{tenths / 600:00}:{tenths % 600 / 10:00}.{tenths % 10}";
    }
}
