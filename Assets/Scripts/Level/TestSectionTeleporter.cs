using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// TEST: test dünyasında her sistemi tek başına tekrar tekrar denemek için alan seçme.
// Oyuncu bekleme alanında (hub) hareketsiz ve kontrolsüz başlar. Hub'da her alan için sahnede küçük bir küp buton durur:
// fareyle üstüne gelince sararır, tıklayınca o alanın başına ışınlar. 1-9 ve 0 (10.) tuşları da aynı işi yapar,
// R bulunulan alanı baştan alır, H hub'a döner. Alanlar sahnedeki güvenli alanlardır (aşağıdan yukarı, sonra soldan sağa sıralı).
// Ölünce de seçilen alanın başında doğulur. Sağ üstte alan tuşları ve oyun tuşları (güncel atamalarıyla) gösterilir.
// Sadece test dünyasına konur (KosKos > Build Test World ekler); prototip bölümlerde yoktur.
public class TestSectionTeleporter : MonoBehaviour
{
    // Bekleme alanının güvenli alan adı: alan listesine konmaz (test dünyası builder'ı da bu adı kullanır)
    public const string HubZoneName = "SafeZone_Hub";

    [Tooltip("Sağ üstte alan ve oyun tuşlarının listesini göster.")]
    [SerializeField] private bool showKeyList = true;

    [Tooltip("Bulunulan alanın listede gösterildiği renk.")]
    [SerializeField] private Color currentSectionColor = new Color(1f, 0.85f, 0.3f);

    [Header("Hub Küp Butonları")]
    [Tooltip("Küp butonların normal rengi.")]
    [SerializeField] private Color cubeColor = new Color(0.25f, 0.6f, 0.9f);

    [Tooltip("Fare üstüne gelince küpün rengi.")]
    [SerializeField] private Color cubeHoverColor = new Color(1f, 0.85f, 0.3f);

    [Tooltip("Küplerin boyutu (birim).")]
    [SerializeField] private float cubeSize = 0.9f;

    [Tooltip("Küpler arası yatay mesafe (birim).")]
    [SerializeField] private float cubeSpacingX = 2.4f;

    [Tooltip("Küp sıraları arası dikey mesafe (birim).")]
    [SerializeField] private float cubeSpacingY = 2.8f;

    // Bir sıradaki küp sayısı (10 alan = 2 sıra)
    private const int CubesPerRow = 5;

    // 1-9 ve 0 tuşları
    private const int MaxSections = 10;
    private const string SafeZonePrefix = "SafeZone_";

    private readonly List<SafeZone> sections = new List<SafeZone>();
    private readonly List<InputAction> keyActions = new List<InputAction>();
    private PlayerDeath player;
    private PlayerMotor playerMotor;
    private PlayerInputReader playerInput;
    private SafeZone hub;
    private bool inHub;
    private TMP_Text keyListText;
    // Hub'daki küp butonlar: collider -> alan sırası; renk değiştirmek için malzemeleri
    private readonly Dictionary<Collider, int> cubeIndex = new Dictionary<Collider, int>();
    private readonly List<Material> cubeMaterials = new List<Material>();
    private int hoveredCube = -1;
    private InputAction pointerPosition;
    private SafeZone shownSection;
    private bool listDirty = true;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerDeath>();
        if (player != null)
        {
            playerMotor = player.GetComponent<PlayerMotor>();
            playerInput = player.GetComponent<PlayerInputReader>();
        }

        foreach (SafeZone zone in FindObjectsByType<SafeZone>(FindObjectsSortMode.None))
        {
            if (zone.name == HubZoneName) hub = zone;
            else sections.Add(zone);
        }
        // Alanlar üst üste durur: önce yüksekliğe, aynı yükseklikte soldan sağa sıralanır
        sections.Sort((a, b) =>
        {
            int byHeight = a.transform.position.y.CompareTo(b.transform.position.y);
            return byHeight != 0 ? byHeight : a.transform.position.x.CompareTo(b.transform.position.x);
        });

        // Oyun tuşları gibi Input System action'ları üzerinden (klavye: 1-9, 0, R ve H)
        int count = Mathf.Min(sections.Count, MaxSections);
        for (int i = 0; i < count; i++)
        {
            int index = i;
            AddKey($"Section{i + 1}", $"<Keyboard>/{KeyLabel(i)}", () => TeleportTo(index));
        }
        AddKey("RestartSection", "<Keyboard>/r", Restart);
        if (hub != null)
        {
            AddKey("BackToHub", "<Keyboard>/h", EnterHub);
            // Hub küplerine fareyle tıklama: imleç konumu + sol tık
            pointerPosition = new InputAction("HubPointer", InputActionType.Value, "<Pointer>/position");
            pointerPosition.Enable();
            keyActions.Add(pointerPosition);
            AddKey("HubClick", "<Mouse>/leftButton", OnHubClick);
            BuildHubCubes(count);
        }

        if (showKeyList) BuildKeyList(CreateCanvas());
        Loc.LanguageChanged += MarkListDirty;

        // Oyuncu seçim yapana kadar hub'da bekler
        EnterHub();
    }

    private void OnDestroy()
    {
        foreach (InputAction action in keyActions)
        {
            action.Disable();
            action.Dispose();
        }
        Loc.LanguageChanged -= MarkListDirty;
        foreach (Material material in cubeMaterials) Destroy(material);
    }

    private void AddKey(string name, string binding, System.Action onPressed)
    {
        var action = new InputAction(name, InputActionType.Button, binding);
        action.performed += _ => onPressed();
        action.Enable();
        keyActions.Add(action);
    }

    private bool CanAct => player != null && !player.IsDead && Time.timeScale != 0f;

    private void TeleportTo(int index)
    {
        if (!CanAct) return;

        // Hub'dan çıkılır: oyuncu yeniden hareket eder ve kontrol edilebilir
        SetHubHold(false);
        // Geriye dönülebilir: seçilen alan aktif güvenli alan olur, ölünce de orada doğulur
        SafeZone.ForceCurrent(sections[index]);
        player.RespawnNow();
    }

    private void Restart()
    {
        if (!CanAct || inHub) return;
        player.RespawnNow();
    }

    // Bekleme alanı: oyuncu orada durur, koşmaz ve oyun tuşları çalışmaz (sadece alan seçme tuşları)
    private void EnterHub()
    {
        if (hub == null || !CanAct) return;

        SafeZone.ForceCurrent(hub);
        player.RespawnNow();
        SetHubHold(true);
    }

    private void SetHubHold(bool hold)
    {
        inHub = hold;
        listDirty = true;
        // Donuk motor: yerçekimi ve koşu yok. Kapalı okuyucu: zıplama/atılma vb. donukken oyuncuyu kaydırmasın
        if (playerMotor != null) playerMotor.SetFrozen(hold);
        if (playerInput != null) playerInput.enabled = !hold;
        if (!hold) SetHovered(-1);
    }

    // Hub'ın önünde iki sıra küp; her küpün üstünde tuşu ve alan adı yazar. Sadece hub'dan görünür
    private void BuildHubCubes(int count)
    {
        var root = new GameObject("HubCubes").transform;
        root.SetParent(transform, false);
        Vector3 origin = hub.transform.position;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        for (int i = 0; i < count; i++)
        {
            int column = i % CubesPerRow;
            int row = i / CubesPerRow;
            // Kameranın baktığı yerin (oyuncunun ~4 birim sağı) etrafında ortalı; ilk sıra üstte.
            // Güvenli alanın merkezi y=3'te: sıralar oyuncunun başının üstünde kalır
            float x = origin.x + 4f + (column - (CubesPerRow - 1) * 0.5f) * cubeSpacingX;
            var position = new Vector3(x, origin.y + 3f - row * cubeSpacingY, 0f);

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"HubCube_{KeyLabel(i)}_{SectionName(sections[i])}";
            cube.transform.SetParent(root);
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one * cubeSize;
            var renderer = cube.GetComponent<Renderer>();
            Material material = renderer.material;
            material.color = cubeColor;
            cubeMaterials.Add(material);
            cubeIndex[cube.GetComponent<Collider>()] = i;

            // Küpün üstünde, kameraya doğru hafif önde: "1 Movement"
            var label = new GameObject("Label");
            label.transform.SetParent(root);
            label.transform.position = position + new Vector3(0f, cubeSize * 0.5f + 0.45f, -0.5f);
            var text = label.AddComponent<TextMesh>();
            text.font = font;
            text.text = $"{KeyLabel(i)}  {SectionName(sections[i])}";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 48;
            text.characterSize = 0.04f;
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
    }

    // İmlecin altındaki küp (yoksa -1)
    private int CubeUnderPointer()
    {
        Camera cam = Camera.main;
        if (cam == null || pointerPosition == null) return -1;

        Ray ray = cam.ScreenPointToRay(pointerPosition.ReadValue<Vector2>());
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore)) return -1;
        return cubeIndex.TryGetValue(hit.collider, out int index) ? index : -1;
    }

    private void SetHovered(int index)
    {
        if (index == hoveredCube) return;
        if (hoveredCube >= 0) cubeMaterials[hoveredCube].color = cubeColor;
        hoveredCube = index;
        if (hoveredCube >= 0) cubeMaterials[hoveredCube].color = cubeHoverColor;
    }

    private void OnHubClick()
    {
        if (!inHub) return;
        int index = CubeUnderPointer();
        if (index >= 0) TeleportTo(index);
    }

    private void MarkListDirty()
    {
        listDirty = true;
    }

    private void Update()
    {
        // Hub'da fare hangi küpün üstündeyse o sararır
        if (inHub && cubeMaterials.Count > 0) SetHovered(CubeUnderPointer());

        if (keyListText == null) return;
        // Oyuncu ileride yeni bir güvenli alana girince ya da dil değişince liste yenilenir
        if (SafeZone.Current == shownSection && !listDirty) return;
        RefreshKeyList();
    }

    // Kodla kurulan test arayüzünün tuvali (hile paneli gibi)
    private Transform CreateCanvas()
    {
        var canvasObject = new GameObject("TestUI");
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvasObject.transform;
    }

    // Sağ üstte yarı saydam kutu: alan tuşları ve oyun tuşları
    private void BuildKeyList(Transform canvas)
    {
        var box = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
        var boxRect = (RectTransform)box.transform;
        boxRect.SetParent(canvas, false);
        boxRect.anchorMin = boxRect.anchorMax = boxRect.pivot = new Vector2(1f, 1f);
        boxRect.anchoredPosition = new Vector2(-20f, -20f);
        box.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
        var layout = box.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 10, 10);
        var fitter = box.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(box.transform, false);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        keyListText = text;
        RefreshKeyList();
    }

    private void RefreshKeyList()
    {
        shownSection = SafeZone.Current;
        listDirty = false;

        var builder = new StringBuilder();
        string highlight = ColorUtility.ToHtmlStringRGB(currentSectionColor);

        builder.AppendLine("<b>TEST</b>");
        if (inHub) builder.AppendLine($"<color=#{highlight}>Choose an area</color>");
        int count = Mathf.Min(sections.Count, MaxSections);
        for (int i = 0; i < count; i++)
        {
            string line = $"{KeyLabel(i)}   {SectionName(sections[i])}";
            builder.AppendLine(!inHub && sections[i] == shownSection ? $"<color=#{highlight}>> {line}</color>" : $"  {line}");
        }
        builder.AppendLine("R   Restart area");
        if (hub != null) builder.AppendLine("H   Back to hub");

        // Oyun tuşları: güncel atamalarla (ayarlardan değiştirilmişse o görünür)
        if (playerInput != null && playerInput.Actions != null)
        {
            builder.AppendLine();
            builder.AppendLine("<b>Controls</b>");
            foreach (KeyRebinder.Entry entry in KeyRebinder.Rebindables)
            {
                builder.AppendLine($"{KeyRebinder.DisplayName(playerInput.Actions, entry)}   {entry.Label}");
            }
        }

        keyListText.text = builder.ToString().TrimEnd();
    }

    // Sıra -> tuş: 0..8 -> "1".."9", 9 -> "0"
    private static string KeyLabel(int index) => index < 9 ? (index + 1).ToString() : "0";

    // "SafeZone_Pull" -> "Pull"; başlangıç alanı "Start"
    private static string SectionName(SafeZone zone)
    {
        string name = zone.name;
        return name.StartsWith(SafeZonePrefix) ? name.Substring(SafeZonePrefix.Length) : name;
    }
}
