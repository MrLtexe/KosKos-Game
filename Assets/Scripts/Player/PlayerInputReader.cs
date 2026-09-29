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

    // Sürekli okunan değerler (event değil): zipline dengesi (-1..1) ve sapan nişanı (fare ekran konumu)
    public float BalanceInput => balanceAction.ReadValue<float>();
    public Vector2 AimScreenPosition => aimAction.ReadValue<Vector2>();

    private InputActionMap playerMap;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction swordAction;
    private InputAction boostAction;
    private InputAction balanceAction;
    private InputAction aimAction;

    private void Awake()
    {
        playerMap = actions.FindActionMap("Player", true);
        jumpAction = playerMap.FindAction("Jump", true);
        dashAction = playerMap.FindAction("Dash", true);
        swordAction = playerMap.FindAction("Sword", true);
        boostAction = playerMap.FindAction("BoostCharge", true);
        balanceAction = playerMap.FindAction("Balance", true);
        aimAction = playerMap.FindAction("Aim", true);
    }

    private void OnEnable()
    {
        jumpAction.performed += OnJump;
        dashAction.performed += OnDash;
        swordAction.performed += OnSword;
        boostAction.performed += OnBoostPressed;
        // Button tipinde tuş bırakılınca 'canceled' tetiklenir
        boostAction.canceled += OnBoostReleased;
        playerMap.Enable();
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJump;
        dashAction.performed -= OnDash;
        swordAction.performed -= OnSword;
        boostAction.performed -= OnBoostPressed;
        boostAction.canceled -= OnBoostReleased;
        playerMap.Disable();
    }

    private void OnJump(InputAction.CallbackContext _) => JumpPressed?.Invoke();
    private void OnDash(InputAction.CallbackContext _) => DashPressed?.Invoke();
    private void OnSword(InputAction.CallbackContext _) => SwordPressed?.Invoke();
    private void OnBoostPressed(InputAction.CallbackContext _) => BoostPressed?.Invoke();
    private void OnBoostReleased(InputAction.CallbackContext _) => BoostReleased?.Invoke();
}
