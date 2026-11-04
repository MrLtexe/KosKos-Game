using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Ara sahneler ve sahne geçişleri için daktilo efektli, DOTween geçiş animasyonlu,
/// sesli ve çift taraflı (Player / NPC) diyalog yöneticisi.
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("Root & Visibility")]
    [Tooltip("Diyalog arayüzünün kök objesi (Canvas veya ana panel). Diyalog yokken gizlenir.")]
    [SerializeField] private GameObject dialogueRoot;

    [Header("Player UI Elemanları")]
    [Tooltip("Sol taraftaki oyuncu avatar resmi")]
    [SerializeField] private Image playerAvatar;
    [Tooltip("Oyuncunun diyalog kutusu arka planı (BG_DialoguePlayer)")]
    [SerializeField] private GameObject playerContainer;
    [Tooltip("Oyuncu diyalog metni (T_DialoguePlayer)")]
    [SerializeField] private TextMeshProUGUI playerDialogueText;
    [Tooltip("Oyuncu isim metni (T_PlayerName)")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [Tooltip("Varsayılan oyuncu avatarı")]
    [SerializeField] private Sprite defaultPlayerAvatar;
    [Tooltip("Varsayılan oyuncu ismi")]
    [SerializeField] private string defaultPlayerName = "KosKos";

    [Header("NPC UI Elemanları")]
    [Tooltip("Sağ taraftaki NPC avatar resmi")]
    [SerializeField] private Image npcAvatar;
    [Tooltip("NPC diyalog kutusu arka planı (BG_DialogueNpc)")]
    [SerializeField] private GameObject npcContainer;
    [Tooltip("NPC diyalog metni (T_DialogueNpc)")]
    [SerializeField] private TextMeshProUGUI npcDialogueText;
    [Tooltip("NPC isim metni (T_NpcName)")]
    [SerializeField] private TextMeshProUGUI npcNameText;
    [Tooltip("Varsayılan NPC avatarı")]
    [SerializeField] private Sprite defaultNpcAvatar;
    [Tooltip("Varsayılan NPC ismi")]
    [SerializeField] private string defaultNpcName = "NPC";

    [Header("Daktilo (Typewriter) Ayarları")]
    [Tooltip("Her harfin yazılması arasındaki süre (saniye)")]
    [SerializeField] private float typingSpeed = 0.03f;
    [Tooltip("Daktilo ses efektini çalacak AudioSource")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Her harf yazıldığında çalınacak ses")]
    [SerializeField] private AudioClip typewriterSound;
    [Tooltip("Ses tonuna hafif rastgelelik eklensin mi?")]
    [SerializeField] private bool randomizePitch = true;
    [Tooltip("Rastgele ton aralığı (min, max)")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);
    [Tooltip("Kaç harfte bir ses çalınsın (1: her harf)")]
    [SerializeField] private int soundFrequency = 1;

    [Header("Geçiş (Transition) Efektleri")]
    [Tooltip("Diyalog açılış animasyonu süresi (saniye)")]
    [SerializeField] private float openDuration = 0.3f;
    [Tooltip("Diyalog kapanış animasyonu süresi (saniye)")]
    [SerializeField] private float closeDuration = 0.22f;
    [Tooltip("Konuşmacı değiştiğinde geçiş süresi (saniye)")]
    [SerializeField] private float switchDuration = 0.25f;
    [Tooltip("Diyalog kutusunun alttan geliş mesafesi (Y offset)")]
    [SerializeField] private float containerSlideOffsetY = -35f;
    [Tooltip("Avatarın kenardan geliş mesafesi (X offset)")]
    [SerializeField] private float avatarSlideOffsetX = 80f;
    [Tooltip("Açılışta kullanılacak Ease eğrisi")]
    [SerializeField] private Ease openEase = Ease.OutBack;
    [Tooltip("Kapanışta kullanılacak Ease eğrisi")]
    [SerializeField] private Ease closeEase = Ease.InCubic;
    [Tooltip("Aynı konuşmacı konuşmaya devam ederken avatara hafif konuşma efekti (punch) verilsin mi?")]
    [SerializeField] private bool punchAvatarOnSameSpeaker = true;

    [Header("Zaman Ayarı")]
    [Tooltip("Diyalog sırasında Time.timeScale = 0 yapılarak oyun dondurulsun mu?")]
    [SerializeField] private bool pauseTimeScale = true;

    [Header("Input Sistemi")]
    [Tooltip("Input Actions asset (Dialogue haritasını içeren dosya)")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string dialogueMapName = "Dialogue";
    [SerializeField] private string skipActionName = "Skip";
    [SerializeField] private string playerMapName = "Player";
    [Tooltip("Opsiyonel: Doğrudan InputActionReference atamak isterseniz")]
    [SerializeField] private InputActionReference skipActionReference;

    [Header("Olaylar (Events)")]
    public UnityEvent OnDialogueStarted;
    public UnityEvent OnDialogueEnded;

    // Durum değişkenleri
    private List<DialogueLine> currentLines;
    private int currentLineIndex = 0;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private bool isTransitioning = false;
    private bool isClosing = false;
    private SpeakerType? lastSpeaker = null;
    private Coroutine typewriterCoroutine;
    private TextMeshProUGUI activeTextComponent;
    private int currentTotalCharacters = 0;
    private float cachedTimeScale = 1f;
    private Action onCompleteCallback;

    // DOTween geçiş referansları
    private Sequence activeTransitionSequence;

    // Cache'lenen Transform ve CanvasGroup referansları
    private RectTransform playerContainerRect;
    private CanvasGroup playerContainerCanvasGroup;
    private Vector2 playerContainerOriginalPos;
    private Vector3 playerContainerOriginalScale = Vector3.one;

    private RectTransform playerAvatarRect;
    private CanvasGroup playerAvatarCanvasGroup;
    private Vector2 playerAvatarOriginalPos;
    private Vector3 playerAvatarOriginalScale = Vector3.one;

    private RectTransform npcContainerRect;
    private CanvasGroup npcContainerCanvasGroup;
    private Vector2 npcContainerOriginalPos;
    private Vector3 npcContainerOriginalScale = Vector3.one;

    private RectTransform npcAvatarRect;
    private CanvasGroup npcAvatarCanvasGroup;
    private Vector2 npcAvatarOriginalPos;
    private Vector3 npcAvatarOriginalScale = Vector3.one;

    // Input aksiyon referansları
    private InputAction skipAction;
    private InputActionMap dialogueMap;
    private InputActionMap playerMap;

    public bool IsDialogueActive => isDialogueActive;
    public bool IsTyping => isTyping;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SetupInput();
        CacheOriginalTransforms();

        // Başlangıçta arayüzü kapat
        HideDialogueUIImmediately();
    }

    private void CacheOriginalTransforms()
    {
        if (playerContainer != null)
        {
            playerContainerRect = playerContainer.GetComponent<RectTransform>();
            if (playerContainerRect != null)
            {
                playerContainerOriginalPos = playerContainerRect.anchoredPosition;
                playerContainerOriginalScale = playerContainerRect.localScale;
            }
            playerContainerCanvasGroup = GetOrAddCanvasGroup(playerContainer);
        }

        if (playerAvatar != null)
        {
            playerAvatarRect = playerAvatar.GetComponent<RectTransform>();
            if (playerAvatarRect != null)
            {
                playerAvatarOriginalPos = playerAvatarRect.anchoredPosition;
                playerAvatarOriginalScale = playerAvatarRect.localScale;
            }
            playerAvatarCanvasGroup = GetOrAddCanvasGroup(playerAvatar.gameObject);
        }

        if (npcContainer != null)
        {
            npcContainerRect = npcContainer.GetComponent<RectTransform>();
            if (npcContainerRect != null)
            {
                npcContainerOriginalPos = npcContainerRect.anchoredPosition;
                npcContainerOriginalScale = npcContainerRect.localScale;
            }
            npcContainerCanvasGroup = GetOrAddCanvasGroup(npcContainer);
        }

        if (npcAvatar != null)
        {
            npcAvatarRect = npcAvatar.GetComponent<RectTransform>();
            if (npcAvatarRect != null)
            {
                npcAvatarOriginalPos = npcAvatarRect.anchoredPosition;
                npcAvatarOriginalScale = npcAvatarRect.localScale;
            }
            npcAvatarCanvasGroup = GetOrAddCanvasGroup(npcAvatar.gameObject);
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

    private void SetupInput()
    {
        if (skipActionReference != null && skipActionReference.action != null)
        {
            skipAction = skipActionReference.action;
        }
        else if (inputActions != null)
        {
            dialogueMap = inputActions.FindActionMap(dialogueMapName, false);
            if (dialogueMap != null)
            {
                skipAction = dialogueMap.FindAction(skipActionName, false);
            }
            playerMap = inputActions.FindActionMap(playerMapName, false);
        }
    }

    private void OnEnable()
    {
        if (skipAction != null)
        {
            skipAction.performed += OnSkipActionPerformed;
        }
    }

    private void OnDisable()
    {
        if (skipAction != null)
        {
            skipAction.performed -= OnSkipActionPerformed;
        }
        KillActiveTransition();
    }

    private void OnDestroy()
    {
        KillActiveTransition();
    }

    private void OnSkipActionPerformed(InputAction.CallbackContext context)
    {
        Skip();
    }

    /// <summary>
    /// ScriptableObject üzerinden diyalogu başlatır.
    /// </summary>
    public void StartDialogue(DialogueData data, Action onComplete = null)
    {
        if (data == null || data.Lines == null || data.Lines.Count == 0)
        {
            Debug.LogWarning("[DialogueSystem] Başlatılmak istenen DialogueData boş veya tanımsız!", this);
            onComplete?.Invoke();
            return;
        }

        StartDialogue(data.Lines, onComplete);
    }

    /// <summary>
    /// Liste üzerinden doğrudan diyalogu başlatır.
    /// </summary>
    public void StartDialogue(List<DialogueLine> lines, Action onComplete = null)
    {
        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning("[DialogueSystem] Başlatılacak diyalog satırı bulunamadı!", this);
            onComplete?.Invoke();
            return;
        }

        KillActiveTransition();

        currentLines = lines;
        currentLineIndex = 0;
        lastSpeaker = null;
        isClosing = false;
        isTransitioning = false;
        onCompleteCallback = onComplete;
        isDialogueActive = true;

        if (pauseTimeScale)
        {
            cachedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        // Input haritalarını ayarla
        if (playerMap != null && playerMap.enabled)
        {
            playerMap.Disable();
        }

        if (dialogueMap != null && !dialogueMap.enabled)
        {
            dialogueMap.Enable();
        }

        if (skipAction != null && !skipAction.enabled)
        {
            skipAction.Enable();
        }

        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(true);
        }

        OnDialogueStarted?.Invoke();
        DisplayLine(currentLineIndex);
    }

    /// <summary>
    /// Oyuncunun Skip tuşuna bastığında tetiklenen metod.
    /// </summary>
    public void Skip()
    {
        // Kapanış animasyonu oynuyorsa hemen tamamla
        if (isClosing)
        {
            if (activeTransitionSequence != null && activeTransitionSequence.IsActive())
            {
                activeTransitionSequence.Complete();
            }
            return;
        }

        if (!isDialogueActive) return;

        // Geçiş animasyonu (açılış veya konuşmacı değişimi) oynuyorsa anında tamamla
        if (isTransitioning)
        {
            if (activeTransitionSequence != null && activeTransitionSequence.IsActive())
            {
                activeTransitionSequence.Complete();
            }
            return;
        }

        if (isTyping)
        {
            // Yazı henüz daktilo ile yazılıyorsa anında tamamla
            CompleteTypingImmediately();
        }
        else
        {
            // Yazı zaten bittiyse sıradakine geç
            currentLineIndex++;
            if (currentLineIndex < currentLines.Count)
            {
                DisplayLine(currentLineIndex);
            }
            else
            {
                EndDialogue();
            }
        }
    }

    private void DisplayLine(int index)
    {
        if (index < 0 || index >= currentLines.Count) return;

        DialogueLine line = currentLines[index];

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        bool isPlayer = line.speaker == SpeakerType.Player;

        // Metin ve avatar içeriklerini hazırla
        if (isPlayer)
        {
            activeTextComponent = playerDialogueText;

            if (playerNameText != null)
            {
                playerNameText.text = string.IsNullOrEmpty(line.speakerName) ? defaultPlayerName : line.speakerName;
            }

            if (playerAvatar != null)
            {
                Sprite avatarToUse = line.customAvatar != null ? line.customAvatar : defaultPlayerAvatar;
                if (avatarToUse != null)
                {
                    playerAvatar.sprite = avatarToUse;
                }
            }
        }
        else
        {
            activeTextComponent = npcDialogueText;

            if (npcNameText != null)
            {
                npcNameText.text = string.IsNullOrEmpty(line.speakerName) ? defaultNpcName : line.speakerName;
            }

            if (npcAvatar != null)
            {
                Sprite avatarToUse = line.customAvatar != null ? line.customAvatar : defaultNpcAvatar;
                if (avatarToUse != null)
                {
                    npcAvatar.sprite = avatarToUse;
                }
            }
        }

        // Geçiş durumunu yönet (Açılış, Konuşmacı Değişimi veya Aynı Konuşmacı)
        if (!lastSpeaker.HasValue)
        {
            // İlk satır: Açılış animasyonu
            lastSpeaker = line.speaker;
            isTransitioning = true;
            AnimateSpeakerEnter(isPlayer, openDuration, () =>
            {
                isTransitioning = false;
                StartTypewriter(line);
            });
        }
        else if (lastSpeaker.Value == line.speaker)
        {
            // Aynı konuşmacı devam ediyor: Kapatıp açmak yerine hafif avatar konuşma punch'ı
            isTransitioning = false;
            if (punchAvatarOnSameSpeaker)
            {
                PunchActiveAvatar(isPlayer);
            }
            StartTypewriter(line);
        }
        else
        {
            // Konuşmacı değişti: Çıkan konuşmacı kapanırken yeni konuşmacı açılır
            bool outgoingWasPlayer = (lastSpeaker.Value == SpeakerType.Player);
            lastSpeaker = line.speaker;
            isTransitioning = true;
            AnimateSpeakerSwitch(outgoingWasPlayer, isPlayer, switchDuration, () =>
            {
                isTransitioning = false;
                StartTypewriter(line);
            });
        }
    }

    private void StartTypewriter(DialogueLine line)
    {
        if (activeTextComponent != null)
        {
            typewriterCoroutine = StartCoroutine(TypewriterRoutine(activeTextComponent, line.dialogueText, line.voiceClip));
        }
        else
        {
            isTyping = false;
        }
    }

    #region DOTween Geçiş Animasyonları

    private void AnimateSpeakerEnter(bool isPlayer, float duration, Action onComplete)
    {
        KillActiveTransition();

        // Karşı konuşmacıyı kapalı tut
        bool otherIsPlayer = !isPlayer;
        GameObject otherContainer = otherIsPlayer ? playerContainer : npcContainer;
        GameObject otherAvatar = otherIsPlayer ? (playerAvatar != null ? playerAvatar.gameObject : null) : (npcAvatar != null ? npcAvatar.gameObject : null);
        if (otherContainer != null) otherContainer.SetActive(false);
        if (otherAvatar != null) otherAvatar.SetActive(false);

        activeTransitionSequence = CreateEnterSequence(isPlayer, duration);
        activeTransitionSequence.OnComplete(() =>
        {
            activeTransitionSequence = null;
            onComplete?.Invoke();
        });
    }

    private void AnimateSpeakerSwitch(bool outgoingIsPlayer, bool incomingIsPlayer, float duration, Action onComplete)
    {
        KillActiveTransition();

        activeTransitionSequence = DOTween.Sequence().SetUpdate(true);

        float exitDuration = duration * 0.65f;
        float enterDuration = duration;

        Sequence exitSeq = CreateExitSequence(outgoingIsPlayer, exitDuration);
        Sequence enterSeq = CreateEnterSequence(incomingIsPlayer, enterDuration);

        // Çıkış başlar, çıkış bitmeden yeni konuşmacı hafif overlap ile sahneye girer
        activeTransitionSequence.Append(exitSeq);
        activeTransitionSequence.Insert(exitDuration * 0.35f, enterSeq);

        activeTransitionSequence.OnComplete(() =>
        {
            activeTransitionSequence = null;
            onComplete?.Invoke();
        });
    }

    private void AnimateClose(bool isPlayer, float duration, Action onComplete)
    {
        KillActiveTransition();

        if (dialogueRoot == null || !dialogueRoot.activeInHierarchy)
        {
            onComplete?.Invoke();
            return;
        }

        activeTransitionSequence = CreateExitSequence(isPlayer, duration);
        activeTransitionSequence.OnComplete(() =>
        {
            activeTransitionSequence = null;
            onComplete?.Invoke();
        });
    }

    private Sequence CreateEnterSequence(bool isPlayer, float duration)
    {
        Sequence seq = DOTween.Sequence().SetUpdate(true);

        RectTransform containerRect = isPlayer ? playerContainerRect : npcContainerRect;
        CanvasGroup containerCg = isPlayer ? playerContainerCanvasGroup : npcContainerCanvasGroup;
        Vector2 containerOrigPos = isPlayer ? playerContainerOriginalPos : npcContainerOriginalPos;
        Vector3 containerOrigScale = isPlayer ? playerContainerOriginalScale : npcContainerOriginalScale;

        RectTransform avatarRect = isPlayer ? playerAvatarRect : npcAvatarRect;
        CanvasGroup avatarCg = isPlayer ? playerAvatarCanvasGroup : npcAvatarCanvasGroup;
        Vector2 avatarOrigPos = isPlayer ? playerAvatarOriginalPos : npcAvatarOriginalPos;
        Vector3 avatarOrigScale = isPlayer ? playerAvatarOriginalScale : npcAvatarOriginalScale;
        float avatarDir = isPlayer ? -1f : 1f;

        GameObject containerObj = isPlayer ? playerContainer : npcContainer;
        GameObject avatarObj = isPlayer ? (playerAvatar != null ? playerAvatar.gameObject : null) : (npcAvatar != null ? npcAvatar.gameObject : null);

        if (containerObj != null) containerObj.SetActive(true);
        if (avatarObj != null) avatarObj.SetActive(true);

        // Başlangıç değerleri
        if (containerCg != null)
        {
            containerCg.alpha = 0f;
            containerCg.interactable = false;
            containerCg.blocksRaycasts = false;
        }

        if (containerRect != null)
        {
            containerRect.anchoredPosition = containerOrigPos + new Vector2(0f, containerSlideOffsetY);
            containerRect.localScale = containerOrigScale * 0.88f;
        }

        if (avatarCg != null)
        {
            avatarCg.alpha = 0f;
        }

        if (avatarRect != null)
        {
            avatarRect.anchoredPosition = avatarOrigPos + new Vector2(avatarDir * avatarSlideOffsetX, 0f);
            avatarRect.localScale = avatarOrigScale * 0.82f;
        }

        // Tweens: Container
        if (containerCg != null)
        {
            seq.Join(containerCg.DOFade(1f, duration).SetUpdate(true));
        }
        if (containerRect != null)
        {
            seq.Join(containerRect.DOAnchorPos(containerOrigPos, duration).SetEase(openEase).SetUpdate(true));
            seq.Join(containerRect.DOScale(containerOrigScale, duration).SetEase(openEase).SetUpdate(true));
        }

        // Tweens: Avatar
        if (avatarCg != null)
        {
            seq.Join(avatarCg.DOFade(1f, duration).SetUpdate(true));
        }
        if (avatarRect != null)
        {
            seq.Join(avatarRect.DOAnchorPos(avatarOrigPos, duration).SetEase(openEase).SetUpdate(true));
            seq.Join(avatarRect.DOScale(avatarOrigScale, duration).SetEase(openEase).SetUpdate(true));
        }

        seq.OnComplete(() =>
        {
            if (containerCg != null)
            {
                containerCg.interactable = true;
                containerCg.blocksRaycasts = true;
            }
        });

        return seq;
    }

    private Sequence CreateExitSequence(bool isPlayer, float duration)
    {
        Sequence seq = DOTween.Sequence().SetUpdate(true);

        RectTransform containerRect = isPlayer ? playerContainerRect : npcContainerRect;
        CanvasGroup containerCg = isPlayer ? playerContainerCanvasGroup : npcContainerCanvasGroup;
        Vector2 containerOrigPos = isPlayer ? playerContainerOriginalPos : npcContainerOriginalPos;
        Vector3 containerOrigScale = isPlayer ? playerContainerOriginalScale : npcContainerOriginalScale;

        RectTransform avatarRect = isPlayer ? playerAvatarRect : npcAvatarRect;
        CanvasGroup avatarCg = isPlayer ? playerAvatarCanvasGroup : npcAvatarCanvasGroup;
        Vector2 avatarOrigPos = isPlayer ? playerAvatarOriginalPos : npcAvatarOriginalPos;
        Vector3 avatarOrigScale = isPlayer ? playerAvatarOriginalScale : npcAvatarOriginalScale;
        float avatarDir = isPlayer ? -1f : 1f;

        GameObject containerObj = isPlayer ? playerContainer : npcContainer;
        GameObject avatarObj = isPlayer ? (playerAvatar != null ? playerAvatar.gameObject : null) : (npcAvatar != null ? npcAvatar.gameObject : null);

        if (containerCg != null)
        {
            containerCg.interactable = false;
            containerCg.blocksRaycasts = false;
            seq.Join(containerCg.DOFade(0f, duration).SetUpdate(true));
        }

        if (containerRect != null)
        {
            Vector2 targetPos = containerOrigPos + new Vector2(0f, containerSlideOffsetY * 0.5f);
            seq.Join(containerRect.DOAnchorPos(targetPos, duration).SetEase(closeEase).SetUpdate(true));
            seq.Join(containerRect.DOScale(containerOrigScale * 0.88f, duration).SetEase(closeEase).SetUpdate(true));
        }

        if (avatarCg != null)
        {
            seq.Join(avatarCg.DOFade(0f, duration).SetUpdate(true));
        }

        if (avatarRect != null)
        {
            Vector2 targetPos = avatarOrigPos + new Vector2(avatarDir * (avatarSlideOffsetX * 0.7f), 0f);
            seq.Join(avatarRect.DOAnchorPos(targetPos, duration).SetEase(closeEase).SetUpdate(true));
            seq.Join(avatarRect.DOScale(avatarOrigScale * 0.82f, duration).SetEase(closeEase).SetUpdate(true));
        }

        seq.OnComplete(() =>
        {
            if (containerObj != null) containerObj.SetActive(false);
            if (avatarObj != null) avatarObj.SetActive(false);

            if (containerRect != null)
            {
                containerRect.anchoredPosition = containerOrigPos;
                containerRect.localScale = containerOrigScale;
            }
            if (avatarRect != null)
            {
                avatarRect.anchoredPosition = avatarOrigPos;
                avatarRect.localScale = avatarOrigScale;
            }
        });

        return seq;
    }

    private void PunchActiveAvatar(bool isPlayer)
    {
        RectTransform avatarRect = isPlayer ? playerAvatarRect : npcAvatarRect;
        Vector3 origScale = isPlayer ? playerAvatarOriginalScale : npcAvatarOriginalScale;

        if (avatarRect != null)
        {
            avatarRect.DOKill(true);
            avatarRect.localScale = origScale;
            avatarRect.DOPunchScale(Vector3.one * 0.08f, 0.2f, 2, 0.5f).SetUpdate(true);
        }
    }

    private void KillActiveTransition()
    {
        if (activeTransitionSequence != null)
        {
            if (activeTransitionSequence.IsActive())
            {
                activeTransitionSequence.Kill();
            }
            activeTransitionSequence = null;
        }
    }

    #endregion

    private IEnumerator TypewriterRoutine(TextMeshProUGUI targetText, string fullText, AudioClip customClip)
    {
        isTyping = true;
        targetText.text = fullText;
        targetText.ForceMeshUpdate();

        currentTotalCharacters = targetText.textInfo.characterCount;
        targetText.maxVisibleCharacters = 0;

        int charactersTyped = 0;

        for (int i = 0; i <= currentTotalCharacters; i++)
        {
            targetText.maxVisibleCharacters = i;

            // Ses çalma mantığı
            if (i > 0 && i <= currentTotalCharacters)
            {
                char typedChar = targetText.textInfo.characterInfo[i - 1].character;

                // Boşluklarda ses çalınmaz
                if (!char.IsWhiteSpace(typedChar))
                {
                    charactersTyped++;
                    if (charactersTyped % soundFrequency == 0)
                    {
                        PlayTypewriterSound(customClip);
                    }
                }
            }

            // Time.timeScale = 0 olduğunda bile çalışması için WaitForSecondsRealtime kullanılır
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
        typewriterCoroutine = null;
    }

    private void CompleteTypingImmediately()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        if (activeTextComponent != null)
        {
            activeTextComponent.maxVisibleCharacters = currentTotalCharacters;
        }

        isTyping = false;
    }

    private void PlayTypewriterSound(AudioClip customClip)
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = customClip != null ? customClip : typewriterSound;
        if (clipToPlay == null) return;

        if (randomizePitch)
        {
            audioSource.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        }
        else
        {
            audioSource.pitch = 1f;
        }

        audioSource.PlayOneShot(clipToPlay);
    }

    /// <summary>
    /// Diyalog sürecini sonlandırır ve kapanış animasyonunun ardından UI'ı kapatır.
    /// </summary>
    public void EndDialogue()
    {
        if (!isDialogueActive) return;

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        isTyping = false;
        isDialogueActive = false;
        isClosing = true;

        bool closePlayer = (!lastSpeaker.HasValue || lastSpeaker.Value == SpeakerType.Player);

        AnimateClose(closePlayer, closeDuration, () =>
        {
            isClosing = false;
            lastSpeaker = null;

            HideDialogueUIImmediately();

            // TimeScale'i eski haline getir
            if (pauseTimeScale)
            {
                Time.timeScale = cachedTimeScale;
            }

            // Input haritalarını geri yükle
            if (dialogueMap != null && dialogueMap.enabled)
            {
                dialogueMap.Disable();
            }

            if (playerMap != null && !playerMap.enabled)
            {
                playerMap.Enable();
            }

            Action callback = onCompleteCallback;
            onCompleteCallback = null;
            currentLines = null;

            OnDialogueEnded?.Invoke();
            callback?.Invoke();
        });
    }

    private void HideDialogueUIImmediately()
    {
        KillActiveTransition();

        if (playerAvatar != null) playerAvatar.gameObject.SetActive(false);
        if (playerContainer != null) playerContainer.SetActive(false);
        if (npcAvatar != null) npcAvatar.gameObject.SetActive(false);
        if (npcContainer != null) npcContainer.SetActive(false);

        if (playerContainerCanvasGroup != null) playerContainerCanvasGroup.alpha = 0f;
        if (playerAvatarCanvasGroup != null) playerAvatarCanvasGroup.alpha = 0f;
        if (npcContainerCanvasGroup != null) npcContainerCanvasGroup.alpha = 0f;
        if (npcAvatarCanvasGroup != null) npcAvatarCanvasGroup.alpha = 0f;

        if (playerContainerRect != null)
        {
            playerContainerRect.anchoredPosition = playerContainerOriginalPos;
            playerContainerRect.localScale = playerContainerOriginalScale;
        }
        if (playerAvatarRect != null)
        {
            playerAvatarRect.anchoredPosition = playerAvatarOriginalPos;
            playerAvatarRect.localScale = playerAvatarOriginalScale;
        }
        if (npcContainerRect != null)
        {
            npcContainerRect.anchoredPosition = npcContainerOriginalPos;
            npcContainerRect.localScale = npcContainerOriginalScale;
        }
        if (npcAvatarRect != null)
        {
            npcAvatarRect.anchoredPosition = npcAvatarOriginalPos;
            npcAvatarRect.localScale = npcAvatarOriginalScale;
        }

        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(false);
        }
    }
}