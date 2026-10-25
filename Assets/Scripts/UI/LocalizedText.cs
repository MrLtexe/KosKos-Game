using TMPro;
using UnityEngine;

// Sabit arayüz yazısı: anahtarı verilen metni çeviri tablosundan (Loc) seçili dilde gösterir, dil değişince kendini yeniler.
// Kullanım: bir TextMeshPro yazısına ekleyip Key alanına anahtarı yazın (ör. menu.play). Anahtarlar Settings/Loc.cs içinde.
// Kodla değiştirilen yazılara (süre, sayılar) eklemeyin; onları ekran kodu doldurur.
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Çeviri anahtarı (Settings/Loc.cs tablosundaki, ör. menu.play). Yazı seçili dilde bu anahtarın metni olur.")]
    [SerializeField] private string key;

    private TMP_Text text;

    public string Key
    {
        get => key;
        set
        {
            key = value;
            Refresh();
        }
    }

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Loc.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        Loc.LanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (text != null && !string.IsNullOrEmpty(key)) text.text = Loc.Get(key);
    }
}
