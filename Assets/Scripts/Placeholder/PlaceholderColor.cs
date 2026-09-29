using UnityEngine;

// GEÇİCİ: gri placeholder küpleri Play modunda renklendirir (kırılabilir = turuncu, ince duvar = camgöbeği).
// Gerçek modeller/malzemeler gelince bu bileşen silinecek.
[RequireComponent(typeof(Renderer))]
public class PlaceholderColor : MonoBehaviour
{
    [Tooltip("Play modunda bu nesneye verilecek renk.")]
    [SerializeField] private Color color = Color.white;

    private Material instance;

    private void Awake()
    {
        // .material paylaşılan malzemenin kopyasını oluşturur; diğer nesneler etkilenmez
        instance = GetComponent<Renderer>().material;
        instance.color = color;
    }

    private void OnDestroy()
    {
        if (instance != null) Destroy(instance);
    }
}
