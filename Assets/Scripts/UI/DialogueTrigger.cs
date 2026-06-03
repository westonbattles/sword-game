using System;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] string dialogueId;

    [TextArea(3, 10)]
    public string dialogueText;

    public string DialogueId
    {
        get
        {
            EnsureDialogueId(false);
            return dialogueId;
        }
    }

    void Reset()
    {
        EnsureDialogueId(true);
    }

    void OnValidate()
    {
        EnsureDialogueId(true);
    }

    void EnsureDialogueId(bool ensureUniqueInScene)
    {
        if (!string.IsNullOrWhiteSpace(dialogueId))
        {
            if (!ensureUniqueInScene || IsDialogueIdUniqueInScene()) return;
        }

        dialogueId = $"dialogue_{Guid.NewGuid():N}";
    }

    bool IsDialogueIdUniqueInScene()
    {
        DialogueTrigger[] triggers = FindObjectsOfType<DialogueTrigger>(true);
        foreach (DialogueTrigger trigger in triggers)
        {
            if (trigger != this && trigger.dialogueId == dialogueId)
            {
                return false;
            }
        }

        return true;
    }
}
