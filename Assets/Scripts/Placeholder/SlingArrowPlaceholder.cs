using UnityEngine;

// GEÇİCİ: sapan nişan oku (çizgi) ve "<zıplama tuşu>: launch" ipucu. Gerçek görsel gelince bu bileşen silinecek.
[RequireComponent(typeof(SlingAbility), typeof(PlayerInputReader))]
public class SlingArrowPlaceholder : MonoBehaviour
{
    [Tooltip("Ok olarak çizilen çizgi.")]
    [SerializeField] private LineRenderer arrow;

    [Tooltip("Fırlatma tuşu ipucu nesnesi (TextMesh); sadece sapan sırasında açık. Yazısı güncel zıplama tuşundan alınır.")]
    [SerializeField] private GameObject hint;

    [Tooltip("Okun uzunluğu (birim).")]
    [SerializeField] private float arrowLength = 1.5f;

    [Tooltip("Okun kalınlığı (birim).")]
    [SerializeField] private float arrowWidth = 0.12f;

    [Tooltip("Okun rengi.")]
    [SerializeField] private Color arrowColor = new Color(0.3f, 0.9f, 1f);

    private SlingAbility sling;
    private PlayerInputReader input;
    private TextMesh hintText;
    private Material material;
    private Vector3 direction = Vector3.right;

    private void Awake()
    {
        sling = GetComponent<SlingAbility>();
        input = GetComponent<PlayerInputReader>();
        hintText = hint.GetComponent<TextMesh>();
        arrow.positionCount = 2;
        arrow.useWorldSpace = true;
        arrow.startWidth = arrowWidth;
        arrow.endWidth = 0f;
        arrow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        arrow.receiveShadows = false;
        // Işıktan etkilenmeyen düz renk malzeme
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.color = arrowColor;
        arrow.sharedMaterial = material;
        SetVisible(false);
    }

    private void OnEnable()
    {
        sling.SlingActiveChanged += SetVisible;
        sling.AimChanged += OnAimChanged;
    }

    private void OnDisable()
    {
        sling.SlingActiveChanged -= SetVisible;
        sling.AimChanged -= OnAimChanged;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    private void LateUpdate()
    {
        if (!arrow.enabled) return;
        Vector3 origin = transform.position;
        arrow.SetPosition(0, origin);
        arrow.SetPosition(1, origin + direction * arrowLength);
    }

    private void OnAimChanged(Vector3 aim)
    {
        direction = aim;
    }

    private void SetVisible(bool visible)
    {
        // Tuş ataması değişmiş olabilir; ipucu her sapanda güncel zıplama tuşunu gösterir
        if (visible && hintText != null) hintText.text = $"{input.JumpKeyName}: launch";
        arrow.enabled = visible;
        hint.SetActive(visible);
    }
}
