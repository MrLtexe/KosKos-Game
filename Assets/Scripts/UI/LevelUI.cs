using UnityEngine;
using UnityEngine.InputSystem;

// Bölüm arayüzünün kökü (LevelUI prefab'ı): gösterge, duraklatma ve sonuç ekranı bu nesnededir.
// Düzen ve görünüm Assets/Prefabs/UI/LevelUI.prefab içinde düzenlenir; değişiklik tüm bölümlere yansır.
[RequireComponent(typeof(LevelHud), typeof(PauseMenu), typeof(ResultsScreen))]
public class LevelUI : MonoBehaviour
{
    [Tooltip("Oyuncu kontrollerini içeren Input Actions dosyası (KosKosControls). Duraklatma ve tuş ayarları için.")]
    [SerializeField] private InputActionAsset actions;

    private void Awake()
    {
        UiKit.EnsureEventSystem();
        GetComponent<PauseMenu>().Init(actions);
    }
}
