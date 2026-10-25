using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Bölüm sonu ekranı (LevelUI prefab'ının kökünde durur): süre, ölüm, toplanabilirler, madalya, rekorlar ve devam butonları.
// Düzen ve görünüm prefab'da düzenlenir; bu kod sadece yazıları doldurur ve butonları bağlar.
public class ResultsScreen : MonoBehaviour
{
    [Tooltip("Sonuç paneli (bölüm bitince görünür).")]
    [SerializeField] private GameObject panel;

    [Tooltip("Süre yazısı.")]
    [SerializeField] private TMP_Text timeText;

    [Tooltip("Ölüm yazısı.")]
    [SerializeField] private TMP_Text deathsText;

    [Tooltip("Kumaş/Belge sayıları yazısı.")]
    [SerializeField] private TMP_Text collectiblesText;

    [Tooltip("Madalya yazısı.")]
    [SerializeField] private TMP_Text medalText;

    [Tooltip("Ek bilgi yazısı (+Kumaş veya 'kaydedilmedi'); gerekmezse gizlenir.")]
    [SerializeField] private TMP_Text extraText;

    [Tooltip("Sonraki bölüm butonu (son bölümde gizlenir).")]
    [SerializeField] private Button nextButton;

    [Tooltip("Tekrar oyna butonu.")]
    [SerializeField] private Button replayButton;

    [Tooltip("Ana menü butonu.")]
    [SerializeField] private Button mainMenuButton;

    private LevelRun run;

    private void Awake()
    {
        replayButton.onClick.AddListener(SceneFlow.ReloadCurrent);
        mainMenuButton.onClick.AddListener(SceneFlow.LoadMenu);
        panel.SetActive(false);
    }

    private void Start()
    {
        run = LevelRun.Current;
        if (run != null) run.Finished += Show;
    }

    private void OnDestroy()
    {
        if (run != null) run.Finished -= Show;
    }

    private void Show(RunSummary s)
    {
        panel.SetActive(true);

        timeText.text = Loc.Format("results.time", LevelHud.FormatTime(s.time)) + NewRecord(s.result.newBestTime);
        deathsText.text = Loc.Format("hud.deaths", s.deaths) + NewRecord(s.result.newBestDeaths);
        collectiblesText.text = Loc.Format("results.collectibles", s.kumas, s.kumasTotal, s.belge, s.belgeTotal);
        medalText.text = Loc.Format("results.medal", MedalRule.DisplayName(s.medal)) + NewRecord(s.result.newMedal);

        string extra = !s.saved ? Loc.Get("results.notSaved")
            : s.result.kumasGained > 0 ? Loc.Format("results.fabricGained", s.result.kumasGained)
            : "";
        extraText.text = extra;
        extraText.gameObject.SetActive(extra.Length > 0);

        bool hasNext = s.nextLevelIndex >= 0;
        nextButton.gameObject.SetActive(hasNext);
        nextButton.onClick.RemoveAllListeners();
        if (hasNext)
        {
            LevelCatalog catalog = s.catalog;
            int next = s.nextLevelIndex;
            nextButton.onClick.AddListener(() => SceneFlow.LoadLevel(catalog, next));
        }

        UiKit.Select(hasNext ? nextButton : replayButton);
    }

    private static string NewRecord(bool isNew) => isNew ? Loc.Get("results.newRecord") : "";
}
