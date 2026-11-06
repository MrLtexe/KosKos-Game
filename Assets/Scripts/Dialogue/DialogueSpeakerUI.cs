using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tek bir diyalog konuşmacısının (Player veya NPC) görsel panelini,
/// avatarını, konuşma balonunu ve DOTween geçiş animasyonlarını yöneten bileşen.
///
/// Avatarlar ebeveyn konteyner içinde kalır ancak CanvasGroup.ignoreParentGroups = true
/// sayesinde ebeveynin alpha'sından bağımsız hareket eder. Böylece:
/// - Konuşan karakter: Avatar parlak (alpha 1, beyaz), diyalog kutusu açık
/// - Konuşmayan karakter: Avatar soluk (dim alpha, gri ton), diyalog kutusu kapalı
/// - Diyalog sonu: Her iki avatar da animasyonlu şekilde tamamen kaybolur
/// </summary>
public class DialogueSpeakerUI : MonoBehaviour
{
    [Header("Avatar")]
    [Tooltip("Konuşmacının avatar resmi (konteyner içinde kalabilir)")]
    [SerializeField] private Image avatarImage;

    [Header("Diyalog Kutusu")]
    [Tooltip("Konuşmacının isim metni")]
    [SerializeField] private TextMeshProUGUI nameText;

    [Tooltip("Diyalog metninin yazılacağı TextMeshPro bileşeni")]
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Varsayılanlar")]
    [SerializeField] private Sprite defaultAvatar;
    [SerializeField] private string defaultName = "Konuşmacı";

    [Header("Animasyon Ayarları")]
    [Tooltip("Diyalog kutusunun alttan/yukarıdan kayma mesafesi (Y offset)")]
    [SerializeField] private float boxSlideOffsetY = -35f;

    [Tooltip("Avatarın kenardan geliş mesafesi (X offset)")]
    [SerializeField] private float avatarSlideOffsetX = 80f;

    [Tooltip("Avatarın kayma yönü: -1 sol (Player), +1 sağ (NPC)")]
    [SerializeField] private float avatarSlideDirection = -1f;

    [Tooltip("Açılış Ease eğrisi")]
    [SerializeField] private Ease openEase = Ease.OutBack;

    [Tooltip("Kapanış Ease eğrisi")]
    [SerializeField] private Ease closeEase = Ease.InCubic;

    [Header("Soluklaşma (Dim) Ayarları")]
    [Tooltip("Konuşmayan karakterin avatar alfa değeri")]
    [Range(0.1f, 1f)]
    [SerializeField] private float dimAlpha = 0.4f;

    [Tooltip("Konuşmayan karakterin renk tonu (gri karartma)")]
    [SerializeField] private Color dimColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Tooltip("Konuşmayan karakterin ölçek çarpanı (hafif küçülme)")]
    [SerializeField] private float dimScaleMultiplier = 0.92f;

    // Konteyner (BG_DialoguePlayer / BG_DialogueNpc) cache
    private RectTransform containerRect;
    private CanvasGroup containerCanvasGroup;
    private Vector2 containerOriginalPos;
    private Vector3 containerOriginalScale = Vector3.one;

    // Avatar cache
    private RectTransform avatarRect;
    private CanvasGroup avatarCanvasGroup;
    private Vector2 avatarOriginalPos;
    private Vector3 avatarOriginalScale = Vector3.one;

    private bool isInitialized;
    private bool hasAvatarEntered;

    public TextMeshProUGUI DialogueText => dialogueText;
    public Image AvatarImage => avatarImage;
    public string DefaultName => defaultName;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    public void InitializeIfNeeded()
    {
        if (isInitialized) return;
        isInitialized = true;

        // Konteyner = bu bileşenin olduğu obje (BG_DialoguePlayer / BG_DialogueNpc)
        containerRect = GetComponent<RectTransform>();
        if (containerRect != null)
        {
            containerOriginalPos = containerRect.anchoredPosition;
            containerOriginalScale = containerRect.localScale;
        }
        containerCanvasGroup = GetOrAddCanvasGroup(gameObject);

        // Avatar
        if (avatarImage != null)
        {
            avatarRect = avatarImage.GetComponent<RectTransform>();
            if (avatarRect != null)
            {
                avatarOriginalPos = avatarRect.anchoredPosition;
                avatarOriginalScale = avatarRect.localScale;
            }
            avatarCanvasGroup = GetOrAddCanvasGroup(avatarImage.gameObject);

            // Avatarın ebeveyn konteyner alpha'sından bağımsız olmasını sağla.
            // Bu sayede konteyner alpha=0 olduğunda bile avatar kendi alpha'sıyla görünür kalır.
            avatarCanvasGroup.ignoreParentGroups = true;
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        if (obj == null) return null;
        var cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        return cg;
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>Konuşmacının adını ve avatarını ayarlar.</summary>
    public void Setup(string speakerName, Sprite customAvatar)
    {
        InitializeIfNeeded();

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(speakerName) ? defaultName : speakerName;

        if (avatarImage != null)
        {
            Sprite sprite = customAvatar != null ? customAvatar : defaultAvatar;
            if (sprite != null) avatarImage.sprite = sprite;
        }
    }

    /// <summary>Metin kutusunu boşaltır.</summary>
    public void ClearText()
    {
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            dialogueText.maxVisibleCharacters = 0;
        }
    }

    // ─────────────────────────────────────────────
    //  Aktif Konuşmacı Geçişi
    // ─────────────────────────────────────────────

    /// <summary>
    /// Bu konuşmacı aktif konuşmacı olduğunda çağrılır.
    /// Avatar öne çıkıp parlar, diyalog kutusu ekrana gelir.
    /// </summary>
    public Sequence TransitionToActiveSpeaker(float duration)
    {
        InitializeIfNeeded();
        ClearText();

        var seq = DOTween.Sequence().SetUpdate(true);

        // Konteyneri aktif yap (SetActive kullanıyoruz çünkü
        // avatar ignoreParentGroups ile bağımsız)
        gameObject.SetActive(true);

        // ── AVATAR ──
        if (avatarImage != null && avatarRect != null)
        {
            avatarImage.gameObject.SetActive(true);

            if (!hasAvatarEntered)
            {
                // İlk giriş: kenardan kayarak gel
                hasAvatarEntered = true;
                if (avatarCanvasGroup != null) avatarCanvasGroup.alpha = 0f;
                avatarImage.color = Color.white;
                avatarRect.anchoredPosition = avatarOriginalPos
                    + new Vector2(avatarSlideDirection * avatarSlideOffsetX, 0f);
                avatarRect.localScale = avatarOriginalScale * 0.82f;

                if (avatarCanvasGroup != null)
                    seq.Join(avatarCanvasGroup.DOFade(1f, duration).SetUpdate(true));
                seq.Join(avatarRect.DOAnchorPos(avatarOriginalPos, duration)
                    .SetEase(openEase).SetUpdate(true));
                seq.Join(avatarRect.DOScale(avatarOriginalScale, duration)
                    .SetEase(openEase).SetUpdate(true));
            }
            else
            {
                // Zaten sahnedeydi (soluktu): parlat, boyut ve renk düzelt
                if (avatarCanvasGroup != null)
                    seq.Join(avatarCanvasGroup.DOFade(1f, duration).SetUpdate(true));
                seq.Join(avatarImage.DOColor(Color.white, duration).SetUpdate(true));
                seq.Join(avatarRect.DOScale(avatarOriginalScale, duration)
                    .SetEase(openEase).SetUpdate(true));
            }
        }

        // ── DİYALOG KUTUSU (konteyner — arka plan + metin) ──
        if (containerCanvasGroup != null)
        {
            containerCanvasGroup.alpha = 0f;
            containerCanvasGroup.interactable = false;
            containerCanvasGroup.blocksRaycasts = false;
            seq.Join(containerCanvasGroup.DOFade(1f, duration).SetUpdate(true));
        }

        if (containerRect != null)
        {
            containerRect.anchoredPosition = containerOriginalPos
                + new Vector2(0f, boxSlideOffsetY);
            containerRect.localScale = containerOriginalScale * 0.88f;

            seq.Join(containerRect.DOAnchorPos(containerOriginalPos, duration)
                .SetEase(openEase).SetUpdate(true));
            seq.Join(containerRect.DOScale(containerOriginalScale, duration)
                .SetEase(openEase).SetUpdate(true));
        }

        seq.OnComplete(() =>
        {
            if (containerCanvasGroup != null)
            {
                containerCanvasGroup.interactable = true;
                containerCanvasGroup.blocksRaycasts = true;
            }
        });

        return seq;
    }

    // ─────────────────────────────────────────────
    //  Pasif (Soluk) Konuşmacı Geçişi
    // ─────────────────────────────────────────────

    /// <summary>
    /// Bu konuşmacının sırası geçtiğinde çağrılır.
    /// Avatar KAYBOLMAZ: soluklaşır, küçülür ve arka plana geçer.
    /// Diyalog kutusu (arka plan + metin) kapanır.
    /// </summary>
    public Sequence TransitionToInactiveSpeaker(float duration)
    {
        InitializeIfNeeded();

        var seq = DOTween.Sequence().SetUpdate(true);

        // ── AVATAR: Soluklaştır ──
        if (avatarImage != null && avatarRect != null && hasAvatarEntered)
        {
            if (avatarCanvasGroup != null)
                seq.Join(avatarCanvasGroup.DOFade(dimAlpha, duration).SetUpdate(true));
            seq.Join(avatarImage.DOColor(dimColor, duration).SetUpdate(true));
            seq.Join(avatarRect.DOScale(avatarOriginalScale * dimScaleMultiplier, duration)
                .SetEase(closeEase).SetUpdate(true));
        }

        // ── DİYALOG KUTUSU: Kapat ──
        // containerCanvasGroup alpha -> 0 yapar. Avatar ignoreParentGroups
        // sayesinde bundan etkilenmez.
        if (containerCanvasGroup != null)
        {
            containerCanvasGroup.interactable = false;
            containerCanvasGroup.blocksRaycasts = false;
            seq.Join(containerCanvasGroup.DOFade(0f, duration).SetUpdate(true));
        }

        if (containerRect != null)
        {
            var targetPos = containerOriginalPos + new Vector2(0f, boxSlideOffsetY * 0.5f);
            seq.Join(containerRect.DOAnchorPos(targetPos, duration)
                .SetEase(closeEase).SetUpdate(true));
            seq.Join(containerRect.DOScale(containerOriginalScale * 0.88f, duration)
                .SetEase(closeEase).SetUpdate(true));
        }

        seq.OnComplete(() =>
        {
            ClearText();
            // Konteyner pozisyonlarını sıfırla ama SetActive(false) YAPMA
            // Avatar hâlâ soluk olarak görünmeli
            if (containerRect != null)
            {
                containerRect.anchoredPosition = containerOriginalPos;
                containerRect.localScale = containerOriginalScale;
            }
        });

        return seq;
    }

    // ─────────────────────────────────────────────
    //  Mikro Punch (Aynı Konuşmacı)
    // ─────────────────────────────────────────────

    /// <summary>Aynı konuşmacı devam ederken avatara hafif konuşma efekti verir.</summary>
    public void PunchAvatar(float punchScale = 0.08f, float punchDuration = 0.2f)
    {
        if (avatarRect != null)
        {
            avatarRect.DOKill(true);
            avatarRect.localScale = avatarOriginalScale;
            avatarRect.DOPunchScale(Vector3.one * punchScale, punchDuration, 2, 0.5f)
                .SetUpdate(true);
        }
    }

    // ─────────────────────────────────────────────
    //  Tam Kapanış (Diyalog Sonu)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Tüm diyalog bittiğinde: hem diyalog kutusu hem de avatar
    /// animasyonlu şekilde sahneden tamamen çıkar.
    /// </summary>
    public Sequence PlayFullExitAnimation(float duration)
    {
        InitializeIfNeeded();

        var seq = DOTween.Sequence().SetUpdate(true);

        // ── AVATAR TAM KAPANIŞ ──
        if (avatarImage != null && avatarRect != null && hasAvatarEntered)
        {
            if (avatarCanvasGroup != null)
                seq.Join(avatarCanvasGroup.DOFade(0f, duration).SetUpdate(true));

            var targetPos = avatarOriginalPos
                + new Vector2(avatarSlideDirection * avatarSlideOffsetX * 0.7f, 0f);
            seq.Join(avatarRect.DOAnchorPos(targetPos, duration)
                .SetEase(closeEase).SetUpdate(true));
            seq.Join(avatarRect.DOScale(avatarOriginalScale * 0.82f, duration)
                .SetEase(closeEase).SetUpdate(true));
        }

        // ── DİYALOG KUTUSU TAM KAPANIŞ ──
        if (containerCanvasGroup != null)
        {
            containerCanvasGroup.interactable = false;
            containerCanvasGroup.blocksRaycasts = false;
            seq.Join(containerCanvasGroup.DOFade(0f, duration).SetUpdate(true));
        }

        if (containerRect != null)
        {
            var targetPos = containerOriginalPos + new Vector2(0f, boxSlideOffsetY * 0.5f);
            seq.Join(containerRect.DOAnchorPos(targetPos, duration)
                .SetEase(closeEase).SetUpdate(true));
            seq.Join(containerRect.DOScale(containerOriginalScale * 0.88f, duration)
                .SetEase(closeEase).SetUpdate(true));
        }

        seq.OnComplete(() => HideImmediately());

        return seq;
    }

    // ─────────────────────────────────────────────
    //  Anında Gizle / Sıfırla
    // ─────────────────────────────────────────────

    /// <summary>Paneli ve avatarı anında kapatır, tüm değerleri sıfırlar.</summary>
    public void HideImmediately()
    {
        InitializeIfNeeded();
        ClearText();
        hasAvatarEntered = false;

        // Konteyner sıfırla
        if (containerRect != null)
        {
            containerRect.DOKill();
            containerRect.anchoredPosition = containerOriginalPos;
            containerRect.localScale = containerOriginalScale;
        }
        if (containerCanvasGroup != null)
        {
            containerCanvasGroup.alpha = 0f;
            containerCanvasGroup.interactable = false;
            containerCanvasGroup.blocksRaycasts = false;
        }

        // Avatar sıfırla
        if (avatarImage != null)
        {
            avatarImage.color = Color.white;
            if (avatarRect != null)
            {
                avatarRect.DOKill();
                avatarRect.anchoredPosition = avatarOriginalPos;
                avatarRect.localScale = avatarOriginalScale;
            }
            if (avatarCanvasGroup != null)
            {
                avatarCanvasGroup.alpha = 0f;
            }
            avatarImage.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }
}
