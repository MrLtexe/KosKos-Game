using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Metinlerin TextMeshPro bileşeni üzerinde daktilo efektiyle yazılmasını
/// ve harf seslerinin çalınmasını yöneten bileşen.
/// </summary>
public class DialogueTypewriter : MonoBehaviour
{
    [Header("Daktilo (Typewriter) Ayarları")]
    [Tooltip("Her harfin yazılması arasındaki bekleme süresi (saniye)")]
    [SerializeField] private float typingSpeed = 0.03f;

    [Tooltip("Daktilo ses efektini çalacak AudioSource")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Her harf yazıldığında varsayılan çalınacak ses")]
    [SerializeField] private AudioClip defaultTypewriterSound;

    [Tooltip("Ses tonuna hafif rastgelelik eklensin mi?")]
    [SerializeField] private bool randomizePitch = true;

    [Tooltip("Rastgele ton aralığı (min, max)")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);

    [Tooltip("Kaç harfte bir ses çalınsın (1: her harf)")]
    [SerializeField] private int soundFrequency = 1;

    private Coroutine typewriterCoroutine;
    private TextMeshProUGUI currentTextComponent;
    private int currentTotalCharacters = 0;
    private bool isTyping = false;

    public bool IsTyping => isTyping;
    public float TypingSpeed { get => typingSpeed; set => typingSpeed = value; }

    /// <summary>
    /// Belirtilen TextMeshPro bileşenine metni daktilo efektiyle yazdırmaya başlar.
    /// </summary>
    public void StartTyping(TextMeshProUGUI targetText, string fullText, AudioClip voiceClip, Action onComplete = null)
    {
        StopTyping();

        if (targetText == null)
        {
            onComplete?.Invoke();
            return;
        }

        currentTextComponent = targetText;
        typewriterCoroutine = StartCoroutine(TypewriterRoutine(targetText, fullText, voiceClip, onComplete));
    }

    /// <summary>
    /// Yazımı anında tamamlar (tüm metni görünür yapar).
    /// </summary>
    public void CompleteImmediately()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        if (currentTextComponent != null)
        {
            currentTextComponent.maxVisibleCharacters = currentTotalCharacters;
        }

        isTyping = false;
    }

    /// <summary>
    /// Yazım işlemini durdurur.
    /// </summary>
    public void StopTyping()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        isTyping = false;
    }

    private IEnumerator TypewriterRoutine(TextMeshProUGUI targetText, string fullText, AudioClip customClip, Action onComplete)
    {
        isTyping = true;
        targetText.maxVisibleCharacters = 0;
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
                        PlaySound(customClip);
                    }
                }
            }

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
        typewriterCoroutine = null;
        onComplete?.Invoke();
    }

    private void PlaySound(AudioClip customClip)
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = customClip != null ? customClip : defaultTypewriterSound;
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
}

