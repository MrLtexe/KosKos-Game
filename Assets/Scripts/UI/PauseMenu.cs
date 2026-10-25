using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Duraklatma menüsü (Esc; LevelUI prefab'ının kökünde durur, panel kapalıyken de Esc'i dinler).
// Zaman durur, oyuncu kontrolleri kapanır. Kapatınca zaman ölçeği duraklatmadan önceki değerine döner
// (sapan yavaşlaması sırasında duraklatılırsa yavaş devam eder). Düzen ve görünüm prefab'da düzenlenir.
public class PauseMenu : MonoBehaviour
{
    [Tooltip("Duraklatma paneli (açılınca görünür).")]
    [SerializeField] private GameObject panel;

    [Tooltip("Duraklatma butonlarının grubu (ayarlar açılınca gizlenir).")]
    [SerializeField] private GameObject mainGroup;

    [Tooltip("Ayarlar paneli (SettingsPanel prefab'ı).")]
    [SerializeField] private SettingsView settings;

    [Tooltip("Devam butonu.")]
    [SerializeField] private Button resumeButton;

    [Tooltip("Bölümü yeniden başlat butonu.")]
    [SerializeField] private Button restartButton;

    [Tooltip("Ayarlar butonu.")]
    [SerializeField] private Button settingsButton;

    [Tooltip("Ana menü butonu.")]
    [SerializeField] private Button mainMenuButton;

    private InputActionAsset actions;
    private InputAction pauseAction;
    private PlayerInputReader reader;
    private float previousTimeScale = 1f;

    public bool IsOpen => panel.activeSelf;

    private void Awake()
    {
        resumeButton.onClick.AddListener(Close);
        restartButton.onClick.AddListener(SceneFlow.ReloadCurrent);
        settingsButton.onClick.AddListener(ShowSettings);
        mainMenuButton.onClick.AddListener(SceneFlow.LoadMenu);
        panel.SetActive(false);
    }

    // LevelUI çağırır
    public void Init(InputActionAsset actionsAsset)
    {
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

        panel.SetActive(true);
        ShowMain();
    }

    private void ShowMain()
    {
        mainGroup.SetActive(true);
        settings.gameObject.SetActive(false);
        UiKit.Select(resumeButton);
    }

    private void ShowSettings()
    {
        mainGroup.SetActive(false);
        settings.gameObject.SetActive(true);
        UiKit.Select(settings.Open(actions, ShowMain));
    }

    private void Close()
    {
        if (!IsOpen) return;
        panel.SetActive(false);

        Time.timeScale = previousTimeScale;
        if (LevelRun.Current != null) LevelRun.Current.Paused = false;
        if (reader != null) reader.enabled = true;
    }
}
