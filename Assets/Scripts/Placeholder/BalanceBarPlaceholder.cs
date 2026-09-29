using UnityEngine;

// GEÇİCİ: zipline denge çubuğu. Karakterin üstünde koyu bir çubuk, beyaz imleç (uçlara yakınken kırmızı),
// uçlarda basılınca yanan "A" / "D" harfleri. Gerçek arayüz gelince bu bileşen silinecek.
[RequireComponent(typeof(ZiplineRider), typeof(PlayerInputReader))]
public class BalanceBarPlaceholder : MonoBehaviour
{
    [Tooltip("Çubuğun kök nesnesi; sadece zipline'dayken açık.")]
    [SerializeField] private GameObject barRoot;

    [Tooltip("Denge imleci görseli.")]
    [SerializeField] private Renderer cursor;

    [Tooltip("Sol uçtaki 'A' yazısı.")]
    [SerializeField] private TextMesh leftKey;

    [Tooltip("Sağ uçtaki 'D' yazısı.")]
    [SerializeField] private TextMesh rightKey;

    [Tooltip("Çubuğun yarı genişliği (birim). İmleç -1..1 değerini bu mesafeye çevirir.")]
    [SerializeField] private float barHalfWidth = 0.75f;

    [Tooltip("İmlecin |konum| bu değeri geçince tehlike rengine döner (0-1).")]
    [SerializeField] private float dangerThreshold = 0.7f;

    [Tooltip("İmlecin normal rengi.")]
    [SerializeField] private Color cursorColor = Color.white;

    [Tooltip("İmlecin uçlara yakınken rengi.")]
    [SerializeField] private Color dangerColor = Color.red;

    [Tooltip("Tuş harfinin basılı değilkenki rengi.")]
    [SerializeField] private Color keyDimColor = new Color(0.5f, 0.5f, 0.5f);

    [Tooltip("Tuş harfinin basılıyken rengi.")]
    [SerializeField] private Color keyLitColor = new Color(1f, 0.9f, 0.2f);

    // Tuş basılı sayılma eşiği
    private const float KeyPressThreshold = 0.1f;
    // İmleç çubuğun önünde kalsın
    private const float CursorDepthOffset = -0.01f;

    private ZiplineRider rider;
    private PlayerInputReader input;
    private Material cursorMaterial;

    private void Awake()
    {
        rider = GetComponent<ZiplineRider>();
        input = GetComponent<PlayerInputReader>();
        cursorMaterial = cursor.material;
        barRoot.SetActive(false);
    }

    private void OnEnable()
    {
        rider.RidingChanged += OnRidingChanged;
        rider.BalanceChanged += OnBalanceChanged;
    }

    private void OnDisable()
    {
        rider.RidingChanged -= OnRidingChanged;
        rider.BalanceChanged -= OnBalanceChanged;
    }

    private void OnDestroy()
    {
        if (cursorMaterial != null) Destroy(cursorMaterial);
    }

    private void Update()
    {
        if (!barRoot.activeSelf) return;

        float balance = input.BalanceInput;
        leftKey.color = balance < -KeyPressThreshold ? keyLitColor : keyDimColor;
        rightKey.color = balance > KeyPressThreshold ? keyLitColor : keyDimColor;
    }

    private void OnRidingChanged(bool riding)
    {
        barRoot.SetActive(riding);
    }

    private void OnBalanceChanged(float offset)
    {
        cursor.transform.localPosition = new Vector3(Mathf.Clamp(offset, -1f, 1f) * barHalfWidth, 0f, CursorDepthOffset);
        cursorMaterial.color = Mathf.Abs(offset) > dangerThreshold ? dangerColor : cursorColor;
    }
}
