using UnityEngine;

// GEÇİCİ: jet-çanta yakıt çubuğu. Karakterin üstünde, depo dolu değilken görünür. Gerçek arayüz gelince silinecek.
[RequireComponent(typeof(JetBagAbility))]
public class FuelBarPlaceholder : MonoBehaviour
{
    [Tooltip("Çubuğun kök nesnesi; sadece depo dolu değilken açık.")]
    [SerializeField] private GameObject barRoot;

    [Tooltip("Dolu kısmı gösteren görsel; yakıt azaldıkça sağdan kısalır.")]
    [SerializeField] private Transform fill;

    [Tooltip("Çubuğun tam genişliği (birim).")]
    [SerializeField] private float barWidth = 1.2f;

    // Dolu kısım arka planın önünde kalsın
    private const float FillDepthOffset = -0.01f;

    private JetBagAbility jetBag;

    private void Awake()
    {
        jetBag = GetComponent<JetBagAbility>();
        barRoot.SetActive(false);
    }

    private void LateUpdate()
    {
        float fuel = jetBag.Fuel01;
        bool show = fuel < 1f;
        if (barRoot.activeSelf != show) barRoot.SetActive(show);
        if (!show) return;

        // Dolu kısım sol kenara yaslı kalır
        fill.localScale = new Vector3(barWidth * fuel, fill.localScale.y, 1f);
        fill.localPosition = new Vector3(-barWidth * (1f - fuel) * 0.5f, 0f, FillDepthOffset);
    }
}
