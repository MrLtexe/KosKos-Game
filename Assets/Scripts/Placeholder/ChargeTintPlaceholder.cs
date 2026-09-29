using UnityEngine;

// GEÇİCİ: Boost şarj adımını karakterin rengiyle gösterir (bar yok, B.4).
// Animator ile "idle değişimi" gelince bu bileşen silinecek.
[RequireComponent(typeof(BoostChargeAbility))]
public class ChargeTintPlaceholder : MonoBehaviour
{
    [Tooltip("Rengi değişecek görsel (şimdilik kapsülün MeshRenderer'ı).")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("Adım 0 (normal), 1, 2, 3 için renkler.")]
    [SerializeField] private Color[] stepColors = { Color.white, Color.yellow, new Color(1f, 0.5f, 0f), Color.red };

    private BoostChargeAbility boost;
    private Material material;
    // Şarj yokken (adım 0) kullanılan renk; giyili kostüm bunu değiştirir
    private Color baseColor;

    private void Awake()
    {
        boost = GetComponent<BoostChargeAbility>();
        // Kopya malzeme; paylaşılan malzemeyi kullanan diğer nesneler etkilenmez
        material = targetRenderer.material;
        baseColor = stepColors[0];
    }

    // GEÇİCİ kostüm: karakterin normal rengi (bölüm başında LevelRun ayarlar)
    public void SetBaseColor(Color color)
    {
        baseColor = color;
        if (boost.CurrentStep == 0) material.color = color;
    }

    private void OnEnable()
    {
        boost.ChargeStepChanged += OnStepChanged;
    }

    private void OnDisable()
    {
        boost.ChargeStepChanged -= OnStepChanged;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    private void OnStepChanged(int step)
    {
        material.color = step == 0 ? baseColor : stepColors[Mathf.Clamp(step, 0, stepColors.Length - 1)];
    }
}
