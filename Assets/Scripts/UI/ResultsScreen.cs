using UnityEngine;
using UnityEngine.UI;

// GEÇİCİ bölüm sonu ekranı: süre, ölüm, toplanabilirler, madalya, rekorlar ve devam butonları.
public class ResultsScreen : MonoBehaviour
{
    private Canvas canvas;
    private LevelRun run;

    public void Init(Canvas targetCanvas)
    {
        canvas = targetCanvas;
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
        RectTransform panel = UiKit.Panel(canvas.transform, UiKit.PanelColor, Vector2.zero, Vector2.one);
        RectTransform list = UiKit.VerticalList(panel, 10f);

        UiKit.Line(list, Loc.Get("results.title"), 56);
        UiKit.Line(list, Loc.Format("results.time", LevelHud.FormatTime(s.time)) + NewRecord(s.result.newBestTime), 32);
        UiKit.Line(list, Loc.Format("hud.deaths", s.deaths) + NewRecord(s.result.newBestDeaths), 32);
        UiKit.Line(list, Loc.Format("results.collectibles", s.kumas, s.kumasTotal, s.belge, s.belgeTotal), 32);
        UiKit.Line(list, Loc.Format("results.medal", MedalRule.DisplayName(s.medal)) + NewRecord(s.result.newMedal), 40);

        if (!s.saved) UiKit.Line(list, Loc.Get("results.notSaved"), 24);
        else if (s.result.kumasGained > 0) UiKit.Line(list, Loc.Format("results.fabricGained", s.result.kumasGained), 30);

        Button first = null;
        if (s.nextLevelIndex >= 0)
        {
            LevelCatalog catalog = s.catalog;
            int next = s.nextLevelIndex;
            first = UiKit.Button(list, Loc.Get("results.next"), () => SceneFlow.LoadLevel(catalog, next));
        }
        Button replay = UiKit.Button(list, Loc.Get("results.replay"), SceneFlow.ReloadCurrent);
        UiKit.Button(list, Loc.Get("pause.mainMenu"), SceneFlow.LoadMenu);

        UiKit.Select(first != null ? first : replay);
    }

    private static string NewRecord(bool isNew) => isNew ? Loc.Get("results.newRecord") : "";
}
