using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Arayüz yardımcıları. Menüler artık düzenlenebilir prefab'lardır (Assets/Prefabs/UI); burada sadece:
// EventSystem oluşturma, klavye seçimi ve kodla kurulan tek ekran olan geliştirici hile paneli için basit parçalar kalır.
// Renkler prefab üreticisi (UiPrefabBuilder) ile ortaktır.
public static class UiKit
{
    public static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.8f);
    public static readonly Color BackgroundColor = new Color(0.08f, 0.1f, 0.12f, 1f);
    public static readonly Color ButtonColor = new Color(0.2f, 0.2f, 0.25f, 0.95f);
    public static readonly Color ButtonHighlight = new Color(0.4f, 0.4f, 0.55f, 1f);
    public static readonly Color ButtonDisabled = new Color(0.12f, 0.12f, 0.12f, 0.8f);
    public static readonly Color TextColor = Color.white;

    private const float ButtonHeight = 56f;
    private const float ListWidth = 900f;

    // Kodla kurulan yazıların yazı tipi; MainMenu prefab'daki bir yazının yazı tipini verir (font değişince hile paneli de uyar)
    public static TMP_FontAsset Font { get; set; }

    // Menülerin klavye/kontrolcü ile de kullanılabilmesi için sahnede tek bir EventSystem olmalı
    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        var module = go.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    // Klavye/kontrolcü ile gezinmenin başlayacağı buton
    public static void Select(Selectable selectable)
    {
        if (EventSystem.current == null || selectable == null) return;
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }

    public static void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(parent.GetChild(i).gameObject);
        }
    }

    // Butonun yazısı (TextMeshPro)
    public static TMP_Text ButtonLabel(Button button) => button.GetComponentInChildren<TMP_Text>(true);

    // --- Hile paneli için kodla kurulan parçalar ---

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

    // Listede kullanılan tek satırlık yazı (sabit yükseklik)
    public static TMP_Text Line(Transform parent, string text, int size)
    {
        TMP_Text label = Label(parent, text, size);
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = size + 14f;
        return label;
    }

    // Listede kullanılan tam genişlikli buton
    public static Button Button(Transform parent, string text, UnityAction onClick)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHighlight;
        colors.selectedColor = ButtonHighlight;
        colors.pressedColor = ButtonHighlight * 0.8f;
        colors.disabledColor = ButtonDisabled;
        button.colors = colors;
        if (onClick != null) button.onClick.AddListener(onClick);

        go.AddComponent<LayoutElement>().preferredHeight = ButtonHeight;

        TMP_Text label = Label(go.transform, text, 28);
        var rect = (RectTransform)label.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return button;
    }

    private static TMP_Text Label(Transform parent, string text, int size)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<TextMeshProUGUI>();
        if (Font != null) label.font = Font;
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = TextColor;
        label.raycastTarget = false;
        return label;
    }
}
