using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Diyalog sistemini Inspector'dan test etmek için tetikleyici script.
/// 'startDialogue' kutucuğunu işaretlediğinizde veya sağ tıklayıp 'Start Dialogue Test' dediğinizde diyalog başlar.
/// </summary>
public class DialogueTestTrigger : MonoBehaviour
{
    [Header("Test Tetikleyici")]
    [Tooltip("Play modundayken bu kutucuğu işaretlediğinizde test diyaloğu başlar.")]
    [SerializeField] private bool startDialogue = false;

    [Header("Diyalog Kaynağı")]
    [Tooltip("Varsa oluşturduğunuz ScriptableObject (boş bırakılırsa aşağıdaki inline liste kullanılır)")]
    [SerializeField] private DialogueData dialogueData;

    [Tooltip("ScriptableObject olmadan doğrudan buradan satır girmek isterseniz kullanabilirsiniz")]
    [SerializeField] private List<DialogueLine> testLines = new List<DialogueLine>();

    private void Reset()
    {
        // Varsayılan örnek test konuşmaları
        testLines = new List<DialogueLine>
        {
            new DialogueLine
            {
                speaker = SpeakerType.Player,
                speakerName = "KosKos",
                dialogueText = "Hey! Burası neresi? Etrafta tuhaf makineler ve engeller var..."
            },
            new DialogueLine
            {
                speaker = SpeakerType.NPC,
                speakerName = "Gözlemci Drone",
                dialogueText = "Dikkatli ol KosKos! İlerideki sektör henüz temizlenmedi. Zıplama ve sapan yeteneklerini kullanmalısın."
            },
            new DialogueLine
            {
                speaker = SpeakerType.Player,
                speakerName = "KosKos",
                dialogueText = "Anlaşıldı, yola koyuluyorum!"
            }
        };
    }

    private void Update()
    {
        if (startDialogue)
        {
            startDialogue = false;
            TriggerDialogue();
        }
    }

    [ContextMenu("Start Dialogue Test")]
    public void TriggerDialogue()
    {
        if (DialogueSystem.Instance == null)
        {
            Debug.LogError("[DialogueTestTrigger] Sahnede aktif bir DialogueSystem bileşeni bulunamadı!", this);
            return;
        }

        if (dialogueData != null)
        {
            DialogueSystem.Instance.StartDialogue(dialogueData, OnTestComplete);
        }
        else if (testLines != null && testLines.Count > 0)
        {
            DialogueSystem.Instance.StartDialogue(testLines, OnTestComplete);
        }
        else
        {
            Debug.LogWarning("[DialogueTestTrigger] Başlatılacak diyalog bulunamadı! 'dialogueData' veya 'testLines' tanımlayın.", this);
        }
    }

    private void OnTestComplete()
    {
        Debug.Log("[DialogueTestTrigger] Test diyaloğu başarıyla tamamlandı.");
    }
}
