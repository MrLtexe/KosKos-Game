using System.Collections.Generic;
using UnityEngine;

// Bölüm sonu (A.2): oyuncu buraya ulaşınca bölümü kazanmış sayılır.
// Opsiyonel olarak atanmış diyalog varsa önce diyaloğu oynatır, diyalog bitince bölümü bitirir (ResultsScreen açılır).
[RequireComponent(typeof(Collider))]
public class LevelGoal : MonoBehaviour
{
    [Header("Diyalog Ayarları")]
    [Tooltip("Bölüm sonunda oynatılacak diyalog verisi (boş bırakılırsa diyalog olmadan doğrudan bölüm biter)")]
    [SerializeField] private DialogueData dialogueData;

    [Tooltip("ScriptableObject yerine doğrudan buradan diyalog satırları girmek isterseniz kullanabilirsiniz")]
    [SerializeField] private List<DialogueLine> dialogueLines = new List<DialogueLine>();

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        Rigidbody body = other.attachedRigidbody;
        if (body == null || !body.TryGetComponent(out PlayerDeath death) || death.IsDead) return;

        triggered = true;

        // Oyuncu hedefe değdiği anda bölüm süresini hemen durdur (diyalog oynasa bile süre artmasın)
        if (LevelRun.Current != null)
        {
            LevelRun.Current.Paused = true;
        }

        bool hasData = dialogueData != null && dialogueData.Lines != null && dialogueData.Lines.Count > 0;
        bool hasLines = dialogueLines != null && dialogueLines.Count > 0;

        if ((hasData || hasLines) && DialogueSystem.Instance != null)
        {
            if (hasData)
            {
                DialogueSystem.Instance.StartDialogue(dialogueData, CompleteLevel);
            }
            else
            {
                DialogueSystem.Instance.StartDialogue(dialogueLines, CompleteLevel);
            }
        }
        else
        {
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        // Finish birden fazla çağrılsa da LevelRun sadece ilkini işler
        if (LevelRun.Current != null)
        {
            LevelRun.Current.Finish();
        }
    }
}
