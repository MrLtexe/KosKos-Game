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

        UiKit.Line(list, "Level complete!", 56);
        UiKit.Line(list, $"Time: {LevelHud.FormatTime(s.time)}{NewRecord(s.result.newBestTime)}", 32);
        UiKit.Line(list, $"Deaths: {s.deaths}{NewRecord(s.result.newBestDeaths)}", 32);
        UiKit.Line(list, $"Fabric: {s.kumas}/{s.kumasTotal}    Document: {s.belge}/{s.belgeTotal}", 32);
        UiKit.Line(list, $"Medal: {MedalRule.DisplayName(s.medal)}{NewRecord(s.result.newMedal)}", 40);

        if (!s.saved) UiKit.Line(list, "Not saved (level is not in the catalog)", 24);
        else if (s.result.kumasGained > 0) UiKit.Line(list, $"+{s.result.kumasGained} Fabric", 30);

        Button first = null;
        if (s.nextLevelIndex >= 0)
        {
            LevelCatalog catalog = s.catalog;
            int next = s.nextLevelIndex;
            first = UiKit.Button(list, "Next level", () => SceneFlow.LoadLevel(catalog, next));
        }
        Button replay = UiKit.Button(list, "Replay", SceneFlow.ReloadCurrent);
        UiKit.Button(list, "Main menu", SceneFlow.LoadMenu);

        UiKit.Select(first != null ? first : replay);
    }

    private static string NewRecord(bool isNew) => isNew ? "  (New record!)" : "";
}
