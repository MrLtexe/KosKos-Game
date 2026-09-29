using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// GEÇİCİ arayüz yardımcıları: tüm menüler oyun çalışırken kodla oluşturulur (Canvas, panel, yazı, buton, kaydırıcı).
// Gerçek arayüz tasarımı gelince bu yardımcılar ve onları kullanan ekranlar yeniden yapılacak.
public static class UiKit
{
    public static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.8f);
    public static readonly Color ButtonColor = new Color(0.2f, 0.2f, 0.25f, 0.95f);
    public static readonly Color ButtonHighlight = new Color(0.4f, 0.4f, 0.55f, 1f);
    public static readonly Color TextColor = Color.white;

    private const float ButtonHeight = 56f;
    private const float ListWidth = 900f;

    private static Font font;

    private static Font Font
    {
        get
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }

    public static Canvas CreateCanvas(string name, int sortOrder)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // Menülerin klavye/kontrolcü ile de kullanılabilmesi için sahnede tek bir EventSystem olmalı
    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        var module = go.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    // Verilen çapalar arasında dolu, renkli bir panel
    public static RectTransform Panel(Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    // Ortada, içindekileri alt alta dizen liste (yüksekliği içeriğe göre ayarlanır)
    public static RectTransform VerticalList(Transform parent, float spacing)
    {
        var go = new GameObject("List", typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(ListWidth, 0f);

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = go.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rect;
    }

    public static Text Label(Transform parent, string text, int size, TextAnchor anchor)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<Text>();
        label.font = Font;
        label.text = text;
        label.fontSize = size;
        label.alignment = anchor;
        label.color = TextColor;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        return label;
    }

    // Listede kullanılan tam genişlikli buton
    public static Button Button(Transform parent, string text, UnityAction onClick, float height = ButtonHeight)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = Color.white;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHighlight;
        colors.selectedColor = ButtonHighlight;
        colors.pressedColor = ButtonHighlight * 0.8f;
        colors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.8f);
        button.colors = colors;
        if (onClick != null) button.onClick.AddListener(onClick);

        go.AddComponent<LayoutElement>().preferredHeight = height;

        Text label = Label(go.transform, text, height < ButtonHeight ? 24 : 28, TextAnchor.MiddleCenter);
        Stretch((RectTransform)label.transform);
        return button;
    }

    public static Text ButtonLabel(Button button) => button.GetComponentInChildren<Text>();

    public static Slider Slider(Transform parent, float value, UnityAction<float> onChanged)
    {
        GameObject go = DefaultControls.CreateSlider(new DefaultControls.Resources());
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = 36f;
        var slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;
        if (onChanged != null) slider.onValueChanged.AddListener(onChanged);
        return slider;
    }

    // Listede kullanılan tek satırlık yazı (sabit yükseklik)
    public static Text Line(Transform parent, string text, int size)
    {
        Text label = Label(parent, text, size, TextAnchor.MiddleCenter);
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = size + 14f;
        return label;
    }

    // Listede kullanılan çok satırlı yazı (yüksekliği metne göre)
    public static Text Paragraph(Transform parent, string text, int size)
    {
        return Label(parent, text, size, TextAnchor.UpperCenter);
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(parent.GetChild(i).gameObject);
        }
    }

    // Klavye/kontrolcü ile gezinmenin başlayacağı buton
    public static void Select(Selectable selectable)
    {
        if (EventSystem.current == null || selectable == null) return;
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }
}
