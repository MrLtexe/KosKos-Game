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

    private InputActionMap playerMap;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction swordAction;
    private InputAction boostAction;

    private void Awake()
    {
        playerMap = actions.FindActionMap("Player", true);
        jumpAction = playerMap.FindAction("Jump", true);
        dashAction = playerMap.FindAction("Dash", true);
        swordAction = playerMap.FindAction("Sword", true);
        boostAction = playerMap.FindAction("BoostCharge", true);
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
