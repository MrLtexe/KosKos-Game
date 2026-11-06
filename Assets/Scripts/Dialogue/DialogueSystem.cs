using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Ara sahneler ve oyun içi diyaloglar için akış yöneticisi (Controller).
/// Konuşmacı panellerini (DialogueSpeakerUI), daktilo efektini (DialogueTypewriter)
/// ve oyuncu girdilerini koordine eder.
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("Root & Visibility")]
    [Tooltip("Diyalog arayüzünün kök objesi (Canvas veya ana panel).")]
    [SerializeField] private GameObject dialogueRoot;

    [Header("Konuşmacı Panelleri")]
    [Tooltip("Sol taraftaki oyuncu paneli")]
    [SerializeField] private DialogueSpeakerUI playerUI;

    [Tooltip("Sağ taraftaki NPC paneli")]
    [SerializeField] private DialogueSpeakerUI npcUI;

    [Header("Daktilo & Ses Modülü")]
    [SerializeField] private DialogueTypewriter typewriter;

    [Header("Geçiş (Transition) Ayarları")]
    [Tooltip("Diyalog açılış animasyonu süresi (saniye)")]
    [SerializeField] private float openDuration = 0.3f;

    [Tooltip("Diyalog kapanış animasyonu süresi (saniye)")]
    [SerializeField] private float closeDuration = 0.22f;

    [Tooltip("Konuşmacı değiştiğinde geçiş süresi (saniye)")]
    [SerializeField] private float switchDuration = 0.25f;

    [Tooltip("Aynı konuşmacı devam ederken avatara hafif konuşma efekti (punch) verilsin mi?")]
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
    private bool isTransitioning = false;
    private bool isClosing = false;
    private SpeakerType? lastSpeaker = null;
    private float cachedTimeScale = 1f;
    private Action onCompleteCallback;
    private Canvas rootCanvas;

    private Sequence activeTransitionSequence;

    // Input referansları
    private InputAction skipAction;
    private InputActionMap dialogueMap;
    private InputActionMap playerMap;

    public bool IsDialogueActive => isDialogueActive;
    public bool IsTyping => typewriter != null && typewriter.IsTyping;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dialogueRoot != null)
        {
            rootCanvas = dialogueRoot.GetComponent<Canvas>();
        }
        else
        {
            rootCanvas = GetComponent<Canvas>();
            if (rootCanvas != null)
            {
                dialogueRoot = gameObject;
            }
        }

        if (typewriter == null)
        {
            typewriter = GetComponentInChildren<DialogueTypewriter>(true);
            if (typewriter == null)
            {
                typewriter = gameObject.AddComponent<DialogueTypewriter>();
            }
        }

        SetupInput();
        HideAllUI();
    }

    private void SetDialogueRootActive(bool active)
    {
        if (rootCanvas != null)
        {
            rootCanvas.enabled = active;
        }
        else if (dialogueRoot != null)
        {
            if (dialogueRoot != gameObject || active)
            {
                dialogueRoot.SetActive(active);
            }
        }
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

        if (playerUI != null) playerUI.ClearText();
        if (npcUI != null) npcUI.ClearText();

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

        SetDialogueRootActive(true);

        OnDialogueStarted?.Invoke();
        DisplayLine(currentLineIndex);
    }

    /// <summary>
    /// Oyuncunun Skip tuşuna bastığında tetiklenen metod.
    /// </summary>
    public void Skip()
    {
        if (isClosing)
        {
            if (activeTransitionSequence != null && activeTransitionSequence.IsActive())
            {
                activeTransitionSequence.Complete();
            }
            return;
        }

        if (!isDialogueActive) return;

        if (isTransitioning)
        {
            if (activeTransitionSequence != null && activeTransitionSequence.IsActive())
            {
                activeTransitionSequence.Complete();
            }
            return;
        }

        if (typewriter != null && typewriter.IsTyping)
        {
            typewriter.CompleteImmediately();
        }
        else
        {
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
        bool isPlayer = line.speaker == SpeakerType.Player;
        DialogueSpeakerUI currentUI = isPlayer ? playerUI : npcUI;
        DialogueSpeakerUI otherUI = isPlayer ? npcUI : playerUI;

        if (typewriter != null)
        {
            typewriter.StopTyping();
        }

        if (currentUI != null)
        {
            currentUI.Setup(line.speakerName, line.customAvatar);
            currentUI.ClearText();
        }

        // Geçiş durumunu yönet (İlk Açılış, Aynı Konuşmacı veya Konuşmacı Değişimi)
        if (!lastSpeaker.HasValue)
        {
            lastSpeaker = line.speaker;
            isTransitioning = true;

            AnimateFirstSpeakerEnter(currentUI, otherUI, openDuration, () =>
            {
                isTransitioning = false;
                StartTypewriter(currentUI, line);
            });
        }
        else if (lastSpeaker.Value == line.speaker)
        {
            isTransitioning = false;
            if (punchAvatarOnSameSpeaker && currentUI != null)
            {
                currentUI.PunchAvatar();
            }
            StartTypewriter(currentUI, line);
        }
        else
        {
            // Konuşmacı değişti: Eski konuşmacı soluklaşır, yeni konuşmacı öne çıkar
            lastSpeaker = line.speaker;
            isTransitioning = true;

            AnimateSpeakerSwitch(otherUI, currentUI, switchDuration, () =>
            {
                isTransitioning = false;
                StartTypewriter(currentUI, line);
            });
        }
    }

    private void StartTypewriter(DialogueSpeakerUI targetUI, DialogueLine line)
    {
        if (typewriter != null && targetUI != null && targetUI.DialogueText != null)
        {
            typewriter.StartTyping(targetUI.DialogueText, line.dialogueText, line.voiceClip);
        }
    }

    #region Geçiş Animasyon Koordinasyonu

    private void AnimateFirstSpeakerEnter(DialogueSpeakerUI incoming, DialogueSpeakerUI other, float duration, Action onComplete)
    {
        KillActiveTransition();

        if (other != null) other.HideImmediately();

        if (incoming != null)
        {
            activeTransitionSequence = incoming.TransitionToActiveSpeaker(duration);
            activeTransitionSequence.OnComplete(() =>
            {
                activeTransitionSequence = null;
                onComplete?.Invoke();
            });
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private void AnimateSpeakerSwitch(DialogueSpeakerUI outgoing, DialogueSpeakerUI incoming, float duration, Action onComplete)
    {
        KillActiveTransition();

        activeTransitionSequence = DOTween.Sequence().SetUpdate(true);

        // Çıkan konuşmacı: Avatarı soluklaşıp arka plana geçer, kutusu kapanır
        if (outgoing != null)
        {
            activeTransitionSequence.Join(outgoing.TransitionToInactiveSpeaker(duration));
        }

        // Gelen konuşmacı: Avatarı öne çıkarak parlar, kutusu açılır
        if (incoming != null)
        {
            activeTransitionSequence.Join(incoming.TransitionToActiveSpeaker(duration));
        }

        activeTransitionSequence.OnComplete(() =>
        {
            activeTransitionSequence = null;
            onComplete?.Invoke();
        });
    }

    private void AnimateClose(float duration, Action onComplete)
    {
        KillActiveTransition();

        activeTransitionSequence = DOTween.Sequence().SetUpdate(true);

        if (playerUI != null)
        {
            activeTransitionSequence.Join(playerUI.PlayFullExitAnimation(duration));
        }
        if (npcUI != null)
        {
            activeTransitionSequence.Join(npcUI.PlayFullExitAnimation(duration));
        }

        activeTransitionSequence.OnComplete(() =>
        {
            activeTransitionSequence = null;
            onComplete?.Invoke();
        });
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

    /// <summary>
    /// Diyalog sürecini sonlandırır ve kapanış animasyonunun ardından UI'ı kapatır.
    /// </summary>
    public void EndDialogue()
    {
        if (!isDialogueActive) return;

        if (typewriter != null)
        {
            typewriter.StopTyping();
        }

        isDialogueActive = false;
        isClosing = true;

        AnimateClose(closeDuration, () =>
        {
            isClosing = false;
            lastSpeaker = null;

            HideAllUI();

            if (pauseTimeScale)
            {
                Time.timeScale = cachedTimeScale;
            }

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

    private void HideAllUI()
    {
        KillActiveTransition();

        if (playerUI != null) playerUI.HideImmediately();
        if (npcUI != null) npcUI.HideImmediately();

        SetDialogueRootActive(false);
    }
}
