using UnityEngine;
using UnityEngine.InputSystem;

// Bölüm arayüzünün kökü: Canvas'ı oluşturur ve gösterge, duraklatma ve sonuç ekranlarını ekler.
// Bölüm sahnelerinde "LevelUI" nesnesinde durur (prototip bölüm builder'ı ekler).
public class LevelUI : MonoBehaviour
{
    [Tooltip("Oyuncu kontrollerini içeren Input Actions dosyası (KosKosControls). Duraklatma ve tuş ayarları için.")]
    [SerializeField] private InputActionAsset actions;

    private void Awake()
    {
        UiKit.EnsureEventSystem();
        Canvas canvas = UiKit.CreateCanvas("LevelCanvas", 10);
        canvas.transform.SetParent(transform, false);

        gameObject.AddComponent<LevelHud>().Init(canvas);
        gameObject.AddComponent<ResultsScreen>().Init(canvas);
        gameObject.AddComponent<PauseMenu>().Init(canvas, actions);
    }
}
