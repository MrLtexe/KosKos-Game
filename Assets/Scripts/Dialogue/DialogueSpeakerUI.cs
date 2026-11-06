using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tek bir diyalog konuşmacısının (Player veya NPC) görsel panelini,
/// avatarını, metinlerini ve DOTween giriş/çıkış animasyonlarını yöneten bileşen.
/// </summary>
public class DialogueSpeakerUI : MonoBehaviour
{
    [Header("UI Elemanları")]
    [Tooltip("Konuşmacının avatar resmi")]
    [SerializeField] private Image avatarImage;

    [Tooltip("Konuşmacının isim metni")]
    [SerializeField] private TextMeshProUGUI nameText;

    [Tooltip("Diyalog metninin yazılacağı TextMeshPro bileşeni")]
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Tooltip("Varsayılan avatar")]
    [SerializeField] private Sprite defaultAvatar;

    [Tooltip("Varsayılan konuşmacı adı")]
    [SerializeField] private string defaultName = "Konuşmacı";

    [Header("Animasyon Ayarları")]
    [Tooltip("Diyalog kutusunun alttan/yukarıdan kayma mesafesi (Y offset)")]
    [SerializeField] private float containerSlideOffsetY = -35f;

    [Tooltip("Avatarın kenardan geliş mesafesi (X offset)")]
    [SerializeField] private float avatarSlideOffsetX = 80f;

    [Tooltip("Avatarın kayma yönü: -1 sol (Player), +1 sağ (NPC)")]
    [SerializeField] private float avatarSlideDirection = -1f;

    [Tooltip("Açılış Ease eğrisi")]
    [SerializeField] private Ease openEase = Ease.OutBack;

    [Tooltip("Kapanış Ease eğrisi")]
    [SerializeField] private Ease closeEase = Ease.InCubic;

    // Cache'lenen bileşenler
    private RectTransform containerRect;
    private CanvasGroup containerCanvasGroup;
    private Vector2 containerOriginalPos;
    private Vector3 containerOriginalScale = Vector3.one;

    private RectTransform avatarRect;
    private CanvasGroup avatarCanvasGroup;
    private Vector2 avatarOriginalPos;
    private Vector3 avatarOriginalScale = Vector3.one;

    private bool isInitialized = false;

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

        containerRect = GetComponent<RectTransform>();
        if (containerRect != null)
        {
            containerOriginalPos = containerRect.anchoredPosition;
            containerOriginalScale = containerRect.localScale;
        }
        containerCanvasGroup = GetOrAddCanvasGroup(gameObject);

        if (avatarImage != null)
        {
            avatarRect = avatarImage.GetComponent<RectTransform>();
            if (avatarRect != null)
            {
                avatarOriginalPos = avatarRect.anchoredPosition;
                avatarOriginalScale = avatarRect.localScale;
            }
            avatarCanvasGroup = GetOrAddCanvasGroup(avatarImage.gameObject);
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        if (obj == null) return null;
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = obj.AddComponent<CanvasGroup>();
        }
        return cg;
    }

    /// <summary>
    /// Konuşmacının adını ve avatarını ayarlar.
    /// </summary>
    public void Setup(string speakerName, Sprite customAvatar)
    {
        InitializeIfNeeded();

        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(speakerName) ? defaultName : speakerName;
        }

        if (avatarImage != null)
        {
            Sprite spriteToUse = customAvatar != null ? customAvatar : defaultAvatar;
            if (spriteToUse != null)
            {
                avatarImage.sprite = spriteToUse;
            }
        }
    }

    /// <summary>
    /// Metin kutusunu boşaltır (geçiş animasyonlarında eski metnin görünmesini engeller).
    /// </summary>
    public void ClearText()
    {
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            dialogueText.maxVisibleCharacters = 0;
        }
    }

    /// <summary>
    /// Giriş animasyonu için bir Sequence oluşturur.
    /// </summary>
    public Sequence CreateEnterSequence(float duration)
    {
        InitializeIfNeeded();
        ClearText();

        gameObject.SetActive(true);
        if (avatarImage != null) avatarImage.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        // Başlangıç değerleri
        if (containerCanvasGroup != null)
        {
            containerCanvasGroup.alpha = 0f;
            containerCanvasGroup.interactable = false;
            containerCanvasGroup.blocksRaycasts = false;
        }

        if (containerRect != null)
        {
            containerRect.anchoredPosition = containerOriginalPos + new Vector2(0f, containerSlideOffsetY);
            containerRect.localScale = containerOriginalScale * 0.88f;
        }

        if (avatarCanvasGroup != null)
        {
            avatarCanvasGroup.alpha = 0f;
        }

        if (avatarRect != null)
        {
            avatarRect.anchoredPosition = avatarOriginalPos + new Vector2(avatarSlideDirection * avatarSlideOffsetX, 0f);
            avatarRect.localScale = avatarOriginalScale * 0.82f;
        }

        // Tweens: Container
        if (containerCanvasGroup != null)
        {
            seq.Join(containerCanvasGroup.DOFade(1f, duration).SetUpdate(true));
        }
        if (containerRect != null)
        {
            seq.Join(containerRect.DOAnchorPos(containerOriginalPos, duration).SetEase(openEase).SetUpdate(true));
            seq.Join(containerRect.DOScale(containerOriginalScale, duration).SetEase(openEase).SetUpdate(true));
        }

        // Tweens: Avatar
        if (avatarCanvasGroup != null)
        {
            seq.Join(avatarCanvasGroup.DOFade(1f, duration).SetUpdate(true));
        }
        if (avatarRect != null)
        {
            seq.Join(avatarRect.DOAnchorPos(avatarOriginalPos, duration).SetEase(openEase).SetUpdate(true));
            seq.Join(avatarRect.DOScale(avatarOriginalScale, duration).SetEase(openEase).SetUpdate(true));
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

    /// <summary>
    /// Çıkış animasyonu için bir Sequence oluşturur.
    /// </summary>
    public Sequence CreateExitSequence(float duration)
    {
        InitializeIfNeeded();

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        if (containerCanvasGroup != null)
        {
            containerCanvasGroup.interactable = false;
            containerCanvasGroup.blocksRaycasts = false;
            seq.Join(containerCanvasGroup.DOFade(0f, duration).SetUpdate(true));
        }

        if (containerRect != null)
        {
            Vector2 targetPos = containerOriginalPos + new Vector2(0f, containerSlideOffsetY * 0.5f);
            seq.Join(containerRect.DOAnchorPos(targetPos, duration).SetEase(closeEase).SetUpdate(true));
            seq.Join(containerRect.DOScale(containerOriginalScale * 0.88f, duration).SetEase(closeEase).SetUpdate(true));
        }

        if (avatarCanvasGroup != null)
        {
            seq.Join(avatarCanvasGroup.DOFade(0f, duration).SetUpdate(true));
        }

        if (avatarRect != null)
        {
            Vector2 targetPos = avatarOriginalPos + new Vector2(avatarSlideDirection * (avatarSlideOffsetX * 0.7f), 0f);
            seq.Join(avatarRect.DOAnchorPos(targetPos, duration).SetEase(closeEase).SetUpdate(true));
            seq.Join(avatarRect.DOScale(avatarOriginalScale * 0.82f, duration).SetEase(closeEase).SetUpdate(true));
        }

        seq.OnComplete(() =>
        {
            ClearText();
            gameObject.SetActive(false);
            if (avatarImage != null) avatarImage.gameObject.SetActive(false);

            if (containerRect != null)
            {
                containerRect.anchoredPosition = containerOriginalPos;
                containerRect.localScale = containerOriginalScale;
            }
            if (avatarRect != null)
            {
                avatarRect.anchoredPosition = avatarOriginalPos;
                avatarRect.localScale = avatarOriginalScale;
            }
        });

        return seq;
    }

    /// <summary>
    /// Aynı konuşmacı devam ettiğinde avatara hafif konuşma punch'ı verir.
    /// </summary>
    public void PunchAvatar(float punchScale = 0.08f, float duration = 0.2f)
    {
        if (avatarRect != null)
        {
            avatarRect.DOKill(true);
            avatarRect.localScale = avatarOriginalScale;
            avatarRect.DOPunchScale(Vector3.one * punchScale, duration, 2, 0.5f).SetUpdate(true);
        }
    }

    /// <summary>
    /// Paneli anında kapatır ve değerleri sıfırlar.
    /// </summary>
    public void HideImmediately()
    {
        InitializeIfNeeded();
        ClearText();

        if (avatarImage != null) avatarImage.gameObject.SetActive(false);
        gameObject.SetActive(false);

        if (containerCanvasGroup != null) containerCanvasGroup.alpha = 0f;
        if (avatarCanvasGroup != null) avatarCanvasGroup.alpha = 0f;

        if (containerRect != null)
        {
            containerRect.DOKill();
            containerRect.anchoredPosition = containerOriginalPos;
            containerRect.localScale = containerOriginalScale;
        }
        if (avatarRect != null)
        {
            avatarRect.DOKill();
            avatarRect.anchoredPosition = avatarOriginalPos;
            avatarRect.localScale = avatarOriginalScale;
        }
    }
}

