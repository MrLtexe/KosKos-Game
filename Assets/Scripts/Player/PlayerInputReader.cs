using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Input System aksiyonlarını C# event'lerine çevirir.
// Oyun kodunun geri kalanı tuşları bilmez; kontrolcü/mobil desteği sadece bu katmanı etkiler.
public class PlayerInputReader : MonoBehaviour
{
    [Tooltip("Oyuncu kontrollerini içeren Input Actions dosyası (KosKosControls). İçinde 'Player' haritası olmalı.")]
    [SerializeField] private InputActionAsset actions;

    public event Action JumpPressed;
    public event Action DashPressed;
    public event Action SwordPressed;
    public event Action BoostPressed;
    public event Action BoostReleased;
    public event Action HookReleased;
    public event Action MagBootsPressed;

    // Sürekli okunan değerler (event değil): zipline dengesi (-1..1), sapan nişanı (fare ekran konumu), jet-çanta ve kanca tuşları basılı mı
    public float BalanceInput => balanceAction.ReadValue<float>();
    public Vector2 AimScreenPosition => aimAction.ReadValue<Vector2>();
    public bool JetBagHeld => jetBagAction.IsPressed();
    public bool HookHeld => hookAction.IsPressed();

    // Ekrandaki tuş ipuçları için güncel tuş adları (oyuncu Ayarlar'dan değiştirebilir)
    public string JumpKeyName => jumpAction.GetBindingDisplayString(0);
    public string BalanceLeftKeyName => balanceAction.GetBindingDisplayString(KeyRebinder.BindingIndex(balanceAction, "negative"));
    public string BalanceRightKeyName => balanceAction.GetBindingDisplayString(KeyRebinder.BindingIndex(balanceAction, "positive"));

    private InputActionMap playerMap;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction swordAction;
    private InputAction boostAction;
    private InputAction balanceAction;
    private InputAction aimAction;
    private InputAction hookAction;
    private InputAction jetBagAction;
    private InputAction magBootsAction;

    private void Awake()
    {
        // Oyuncunun kaydettiği tuş atamaları (Ayarlar > Controls)
        GameSettings.ApplyBindings(actions);
        playerMap = actions.FindActionMap("Player", true);
        jumpAction = playerMap.FindAction("Jump", true);
        dashAction = playerMap.FindAction("Dash", true);
        swordAction = playerMap.FindAction("Sword", true);
        boostAction = playerMap.FindAction("BoostCharge", true);
        balanceAction = playerMap.FindAction("Balance", true);
        aimAction = playerMap.FindAction("Aim", true);
        hookAction = playerMap.FindAction("Hook", true);
        jetBagAction = playerMap.FindAction("JetBag", true);
        magBootsAction = playerMap.FindAction("MagBoots", true);
    }

    private void OnEnable()
    {
        jumpAction.performed += OnJump;
        dashAction.performed += OnDash;
        swordAction.performed += OnSword;
        boostAction.performed += OnBoostPressed;
        // Button tipinde tuş bırakılınca 'canceled' tetiklenir
        boostAction.canceled += OnBoostReleased;
        hookAction.canceled += OnHookReleased;
        magBootsAction.performed += OnMagBoots;
        playerMap.Enable();
    }

    private void OnDisable()
    {
        // Kapanırken basılı tutulan tuşlar "bırakıldı" sayılır; yoksa ör. duraklatmada bırakılan boost tuşu
        // hiç bırakılmamış kalır ve şarj patlar, kanca tutunmaya devam eder
        if (boostAction.IsPressed()) BoostReleased?.Invoke();
        if (hookAction.IsPressed()) HookReleased?.Invoke();

        jumpAction.performed -= OnJump;
        dashAction.performed -= OnDash;
        swordAction.performed -= OnSword;
        boostAction.performed -= OnBoostPressed;
        boostAction.canceled -= OnBoostReleased;
        hookAction.canceled -= OnHookReleased;
        magBootsAction.performed -= OnMagBoots;
        playerMap.Disable();
    }

    private void OnJump(InputAction.CallbackContext _) => JumpPressed?.Invoke();
    private void OnDash(InputAction.CallbackContext _) => DashPressed?.Invoke();
    private void OnSword(InputAction.CallbackContext _) => SwordPressed?.Invoke();
    private void OnBoostPressed(InputAction.CallbackContext _) => BoostPressed?.Invoke();
    private void OnBoostReleased(InputAction.CallbackContext _) => BoostReleased?.Invoke();
    private void OnHookReleased(InputAction.CallbackContext _) => HookReleased?.Invoke();
    private void OnMagBoots(InputAction.CallbackContext _) => MagBootsPressed?.Invoke();
}
