using UnityEngine;

// GEÇİCİ: kanca ipi (oyuncudan tutunulan noktaya düz çizgi). Gerçek görsel gelince bu bileşen silinecek.
[RequireComponent(typeof(HookAbility))]
public class HookRopePlaceholder : MonoBehaviour
{
    [Tooltip("İp olarak çizilen çizgi.")]
    [SerializeField] private LineRenderer rope;

    [Tooltip("İpin kalınlığı (birim).")]
    [SerializeField] private float ropeWidth = 0.05f;

    [Tooltip("İpin rengi.")]
    [SerializeField] private Color ropeColor = new Color(0.9f, 0.8f, 0.5f);

    private HookAbility hook;
    private Material material;

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
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    private void LateUpdate()
    {
        bool show = hook.IsHooked;
        if (rope.enabled != show) rope.enabled = show;
        if (!show) return;

        rope.SetPosition(0, transform.position);
        rope.SetPosition(1, hook.AnchorPosition);
    }
}
