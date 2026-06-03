using System.Collections.Generic;

public static class DialogueMemory
{
    static readonly HashSet<string> SeenDialogueIds = new HashSet<string>();

    public static bool HasSeen(string dialogueId)
    {
        return !string.IsNullOrWhiteSpace(dialogueId) && SeenDialogueIds.Contains(dialogueId);
    }

    public static void MarkSeen(string dialogueId)
    {
        if (!string.IsNullOrWhiteSpace(dialogueId))
        {
            SeenDialogueIds.Add(dialogueId);
        }
    }

    public static void Reset()
    {
        SeenDialogueIds.Clear();
    }
}
