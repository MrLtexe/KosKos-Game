using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Ara sahneler ve sahne geçişleri için daktilo efektli, sesli ve çift taraflı (Player / NPC) diyalog yöneticisi.
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
    private Coroutine typewriterCoroutine;
    private TextMeshProUGUI activeTextComponent;
    private int currentTotalCharacters = 0;
    private float cachedTimeScale = 1f;
    private Action onCompleteCallback;

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

        // Başlangıçta arayüzü kapat
        HideDialogueUI();
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
    }

    private void OnSkipActionPerformed(InputAction.CallbackContext context)
    {
        if (!isDialogueActive) return;
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

        currentLines = lines;
        currentLineIndex = 0;
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
        if (!isDialogueActive) return;

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
        }

        bool isPlayer = line.speaker == SpeakerType.Player;

        // Player / NPC görünürlüğünü karşılıklı ayarla
        SetSpeakerVisibility(isPlayer);

        if (isPlayer)
        {
            activeTextComponent = playerDialogueText;

            // İsim ayarı
            if (playerNameText != null)
            {
                playerNameText.text = string.IsNullOrEmpty(line.speakerName) ? defaultPlayerName : line.speakerName;
            }

            // Avatar ayarı
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

            // İsim ayarı
            if (npcNameText != null)
            {
                npcNameText.text = string.IsNullOrEmpty(line.speakerName) ? defaultNpcName : line.speakerName;
            }

            // Avatar ayarı
            if (npcAvatar != null)
            {
                Sprite avatarToUse = line.customAvatar != null ? line.customAvatar : defaultNpcAvatar;
                if (avatarToUse != null)
                {
                    npcAvatar.sprite = avatarToUse;
                }
            }
        }

        if (activeTextComponent != null)
        {
            typewriterCoroutine = StartCoroutine(TypewriterRoutine(activeTextComponent, line.dialogueText, line.voiceClip));
        }
        else
        {
            isTyping = false;
        }
    }

    private void SetSpeakerVisibility(bool isPlayer)
    {
        // Player tarafı
        if (playerAvatar != null) playerAvatar.gameObject.SetActive(isPlayer);
        if (playerContainer != null) playerContainer.SetActive(isPlayer);

        // NPC tarafı
        if (npcAvatar != null) npcAvatar.gameObject.SetActive(!isPlayer);
        if (npcContainer != null) npcContainer.SetActive(!isPlayer);
    }

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
    /// Diyalog sürecini sonlandırır ve UI'ı eski haline getirir.
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

        HideDialogueUI();

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
    }

    private void HideDialogueUI()
    {
        if (playerAvatar != null) playerAvatar.gameObject.SetActive(false);
        if (playerContainer != null) playerContainer.SetActive(false);
        if (npcAvatar != null) npcAvatar.gameObject.SetActive(false);
        if (npcContainer != null) npcContainer.SetActive(false);

        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(false);
        }
    }
}
