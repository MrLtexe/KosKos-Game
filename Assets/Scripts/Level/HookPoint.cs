using System.Collections.Generic;
using UnityEngine;

// Kanca noktası (C): oyuncu HookAbility ile buraya tutunur. Sallanma mı çekilme mi olduğunu
// oyuncunun noktaya göre yüksekliği belirler; nokta sadece yeri tanımlar. Collider'ı yoktur.
// GEÇİCİ: renklendirme (kullanılabilir = sarı, değil = gri) gerçek görsel gelince değişecek.
public class HookPoint : MonoBehaviour
{
    [Tooltip("Oyuncunun hedeflediği (kullanılabilir) noktanın rengi.")]
    [SerializeField] private Color activeColor = new Color(1f, 0.9f, 0.2f);

    [Tooltip("Kullanılamayan noktanın rengi.")]
    [SerializeField] private Color idleColor = new Color(0.5f, 0.5f, 0.5f);

    private static readonly List<HookPoint> points = new List<HookPoint>();

    // Sahnedeki tüm açık kanca noktaları; HookAbility hedefi buradan seçer
    public static IReadOnlyList<HookPoint> All => points;

    public Vector3 Position => transform.position;

    private Material material;

    // Domain reload kapatılırsa liste Play oturumları arasında kalır; bu yüzden elle temizlenir
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        points.Clear();
    }

    private void Awake()
    {
        Renderer r = GetComponent<Renderer>();
        // .material kopya oluşturur; diğer noktalar etkilenmez
        if (r != null) material = r.material;
        SetHighlighted(false);
    }

    private void OnEnable() => points.Add(this);
    private void OnDisable() => points.Remove(this);

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (material != null) material.color = highlighted ? activeColor : idleColor;
    }
}
