using UnityEngine;

// GEÇİCİ: kanca ipi (oyuncudan tutunulan noktaya düz çizgi) ve sallanırken açı göstergesi.
// Gerçek görsel gelince bu bileşen silinecek.
[RequireComponent(typeof(HookAbility))]
public class HookRopePlaceholder : MonoBehaviour
{
    [Tooltip("İp olarak çizilen çizgi.")]
    [SerializeField] private LineRenderer rope;

    [Tooltip("İpin kalınlığı (birim).")]
    [SerializeField] private float ropeWidth = 0.05f;

    [Tooltip("İpin rengi.")]
    [SerializeField] private Color ropeColor = new Color(0.9f, 0.8f, 0.5f);

    [Tooltip("Sallanırken açı göstergesini (ör. 45° / 90°) karakterin üstünde göster.")]
    [SerializeField] private bool showSwingAngle = true;

    [Tooltip("Açı patlama sınırına bu kadar (derece) yaklaşınca gösterge kırmızıya döner.")]
    [SerializeField] private float angleWarningMargin = 15f;

    [Tooltip("Açı göstergesinin arka plan rengi.")]
    [SerializeField] private Color angleBackgroundColor = new Color(0.08f, 0.08f, 0.1f);

    [Tooltip("Açı göstergesinin arka plan boyutu (genişlik, yükseklik; birim).")]
    [SerializeField] private Vector2 angleBackgroundSize = new Vector2(1.5f, 0.4f);

    private HookAbility hook;
    private Material material;
    private Material backgroundMaterial;
    // Açı yazısı oyunda oluşturulur (prefab değişmesin diye)
    private TextMesh angleText;
    private int shownAngle = int.MinValue;

    private void Awake()
    {
        hook = GetComponent<HookAbility>();
        rope.positionCount = 2;
        rope.useWorldSpace = true;
        rope.startWidth = ropeWidth;
        rope.endWidth = ropeWidth;
        rope.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rope.receiveShadows = false;
        // Işıktan etkilenmeyen düz renk malzeme
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.color = ropeColor;
        rope.sharedMaterial = material;
        rope.enabled = false;

        if (showSwingAngle) CreateAngleText();
    }

    // Karakterin üstünde, kameraya doğru hafif önde basit 3B yazı (sapan ipucuyla aynı yer ve stil)
    private void CreateAngleText()
    {
        var go = new GameObject("SwingAngle");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 1.8f, -0.6f);

        angleText = go.AddComponent<TextMesh>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        angleText.font = font;
        angleText.anchor = TextAnchor.MiddleCenter;
        angleText.alignment = TextAlignment.Center;
        angleText.fontSize = 48;
        angleText.characterSize = 0.05f;
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;

        // Arka plan: yazının hemen arkasında düz renk dörtgen (yazıyla birlikte açılıp kapanır)
        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "Background";
        Destroy(background.GetComponent<Collider>());
        background.transform.SetParent(go.transform, false);
        background.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        background.transform.localScale = new Vector3(angleBackgroundSize.x, angleBackgroundSize.y, 1f);
        var backgroundRenderer = background.GetComponent<Renderer>();
        backgroundRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        backgroundRenderer.receiveShadows = false;
        backgroundMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        backgroundMaterial.color = angleBackgroundColor;
        backgroundRenderer.sharedMaterial = backgroundMaterial;

        go.SetActive(false);
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (backgroundMaterial != null) Destroy(backgroundMaterial);
    }

    private void LateUpdate()
    {
        bool show = hook.IsHooked;
        if (rope.enabled != show) rope.enabled = show;
        RefreshAngleText();
        if (!show) return;

        rope.SetPosition(0, transform.position);
        rope.SetPosition(1, hook.AnchorPosition);
    }

    private void RefreshAngleText()
    {
        if (angleText == null) return;

        bool show = hook.IsSwinging;
        if (angleText.gameObject.activeSelf != show) angleText.gameObject.SetActive(show);
        if (!show) return;

        // Yazı sadece tam derece değişince yenilenir
        int degrees = Mathf.RoundToInt(hook.SwingAngleDegrees);
        if (degrees == shownAngle) return;
        shownAngle = degrees;

        int limit = Mathf.RoundToInt(hook.MaxSwingAngle);
        angleText.text = $"{degrees}° / {limit}°";
        angleText.color = degrees >= limit - angleWarningMargin ? Color.red : Color.white;
    }
}
