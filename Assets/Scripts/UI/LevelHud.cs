using UnityEngine;
using UnityEngine.UI;

// GEÇİCİ bölüm içi gösterge (sol üst): süre, ölüm sayısı, Kumaş ve Belge sayıları.
public class LevelHud : MonoBehaviour
{
    private Text label;
    private RectTransform root;
    // Son gösterilen değerler: yazı sadece değişince yeniden oluşturulur (her karede dize üretilmesin)
    private int shownTenths = -1;
    private int shownDeaths = -1;
    private int shownKumas = -1;
    private int shownBelge = -1;

    public void Init(Canvas canvas)
    {
        root = UiKit.Panel(canvas.transform, new Color(0f, 0f, 0f, 0.45f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(20f, -20f);
        root.sizeDelta = new Vector2(330f, 170f);

        label = UiKit.Label(root, "", 30, TextAnchor.UpperLeft);
        var rect = (RectTransform)label.transform;
        UiKit.Stretch(rect);
        rect.offsetMin = new Vector2(16f, 10f);
        rect.offsetMax = new Vector2(-16f, -10f);
    }

    private void Update()
    {
        LevelRun run = LevelRun.Current;
        if (run == null || label == null) return;

        if (run.IsFinished)
        {
            root.gameObject.SetActive(false);
            return;
        }

        int tenths = (int)(run.Elapsed * 10f);
        int kumas = run.KumasCount;
        int belge = run.BelgeCount;
        if (tenths == shownTenths && run.Deaths == shownDeaths && kumas == shownKumas && belge == shownBelge) return;

        shownTenths = tenths;
        shownDeaths = run.Deaths;
        shownKumas = kumas;
        shownBelge = belge;
        label.text = $"{FormatTime(run.Elapsed)}\nDeaths: {run.Deaths}\nFabric: {kumas}/{run.KumasTotal}\nDocument: {belge}/{run.BelgeTotal}";
    }

    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) return "--:--";
        // Onda birlerden hesaplanır; 59.97 gibi değerler "00:60.0" görünmesin
        int tenths = (int)(seconds * 10f);
        return $"{tenths / 600:00}:{tenths % 600 / 10:00}.{tenths % 10}";
    }
}
