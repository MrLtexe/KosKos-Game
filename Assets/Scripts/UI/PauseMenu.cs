using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// GEÇİCİ duraklatma menüsü (Esc): zaman durur, oyuncu kontrolleri kapanır.
// Kapatınca zaman ölçeği duraklatmadan önceki değerine döner (sapan yavaşlaması sırasında duraklatılırsa yavaş devam eder).
public class PauseMenu : MonoBehaviour
{
    private Canvas canvas;
    private InputActionAsset actions;
    private InputAction pauseAction;
    private PlayerInputReader reader;
    private RectTransform panel;
    private float previousTimeScale = 1f;

    public bool IsOpen => panel != null;

    public void Init(Canvas targetCanvas, InputActionAsset actionsAsset)
    {
        canvas = targetCanvas;
        actions = actionsAsset;
        if (actions == null)
        {
            Debug.LogWarning("[KosKos] LevelUI'da Input Actions atanmamış: duraklatma çalışmayacak.");
            return;
        }
        pauseAction = actions.FindAction("System/Pause", true);
        pauseAction.performed += OnPausePressed;
        pauseAction.Enable();
    }

    private void Start()
    {
        reader = FindFirstObjectByType<PlayerInputReader>();
    }

    // Zaman ölçeği burada geri yüklenmez: sahne değişiminde SceneFlow zaten sıfırlar
    private void OnDestroy()
    {
        if (pauseAction != null) pauseAction.performed -= OnPausePressed;
    }

    private void OnPausePressed(InputAction.CallbackContext _)
    {
        LevelRun run = LevelRun.Current;
        if (run != null && run.IsFinished) return;
        // Tuş atanırken Esc sadece atamayı iptal eder, menüyü açıp kapatmaz
        if (KeyRebinder.BlocksPause) return;

        if (IsOpen) Close();
        else Open();
    }

    private void Open()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        if (LevelRun.Current != null) LevelRun.Current.Paused = true;
        if (reader != null) reader.enabled = false;

        panel = UiKit.Panel(canvas.transform, UiKit.PanelColor, Vector2.zero, Vector2.one);
        ShowMain();
    }

    private void ShowMain()
    {
        UiKit.Clear(panel);
        RectTransform list = UiKit.VerticalList(panel, 12f);
        UiKit.Line(list, "Paused", 56);
        Button resume = UiKit.Button(list, "Resume", Close);
        UiKit.Button(list, "Restart level", SceneFlow.ReloadCurrent);
        UiKit.Button(list, "Settings", ShowSettings);
        UiKit.Button(list, "Main menu", SceneFlow.LoadMenu);
        UiKit.Select(resume);
    }

    private void ShowSettings()
    {
        UiKit.Clear(panel);
        SettingsView.Build(panel, actions, ShowMain);
    }

    private void Close()
    {
        if (!IsOpen) return;
        Destroy(panel.gameObject);
        panel = null;

        Time.timeScale = previousTimeScale;
        if (LevelRun.Current != null) LevelRun.Current.Paused = false;
        if (reader != null) reader.enabled = true;
    }
}
