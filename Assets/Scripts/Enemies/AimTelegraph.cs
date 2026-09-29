using UnityEngine;

// Atış öncesi uyarı: namludan nişan yönüne ince bir çizgi.
// Nişan kilitlenince renk değişir. Uyarı stilini değiştirmek için SADECE bu dosya düzenlenir.
[RequireComponent(typeof(LineRenderer))]
public class AimTelegraph : MonoBehaviour
{
    [Tooltip("Nişan çizgisinin uzunluğu (birim).")]
    [SerializeField] private float lineLength = 20f;

    [Tooltip("Nişan çizgisinin kalınlığı (birim).")]
    [SerializeField] private float lineWidth = 0.06f;

    [Tooltip("Nişan alınırken çizginin rengi.")]
    [SerializeField] private Color windUpColor = new Color(1f, 0.6f, 0.1f);

    [Tooltip("Nişan kilitlendiğinde (atışa az kala) çizginin rengi.")]
    [SerializeField] private Color lockedColor = Color.red;

    private LineRenderer line;
    private Material material;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        // Işıktan etkilenmeyen düz renk malzeme
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        line.sharedMaterial = material;
        line.enabled = false;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    public void Show(Vector3 origin, Vector3 direction, bool locked)
    {
        line.SetPosition(0, origin);
        line.SetPosition(1, origin + direction * lineLength);
        material.color = locked ? lockedColor : windUpColor;
        line.enabled = true;
    }

    public void Hide()
    {
        line.enabled = false;
    }
}
