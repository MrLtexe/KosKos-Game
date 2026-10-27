using System;
using System.Collections.Generic;
using UnityEngine;

public enum SpeakerType
{
    Player,
    NPC
}

[Serializable]
public class DialogueLine
{
    [Tooltip("Konuşmayı yapan taraf (Player / NPC)")]
    public SpeakerType speaker = SpeakerType.Player;

    [Tooltip("Konuşanın adı (boş bırakılırsa varsayılan isim kullanılır)")]
    public string speakerName = "";

    [Tooltip("Ekrana daktilo efektiyle yazılacak diyalog metni")]
    [TextArea(3, 6)]
    public string dialogueText = "";

    [Tooltip("Bu satır için özel avatar (boş bırakılırsa varsayılan avatar kullanılır)")]
    public Sprite customAvatar;

    [Tooltip("Bu satıra özel ses klibi (opsiyonel seslendirme veya özel diyalog sesi)")]
    public AudioClip voiceClip;
}

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

    public List<DialogueLine> Lines => lines;
}
