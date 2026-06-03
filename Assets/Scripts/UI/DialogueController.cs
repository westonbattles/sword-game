using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

public class DialogueController : MonoBehaviour
{
    // State variables
    private bool dialogueActive;
    private bool arrowActive;
    private bool textDone;
    private bool arrowFlashStarted;
    private Coroutine currentTextCoroutine;
    // Dialogue objects
    private GameObject Dialogue;
    private GameObject DialogueBase;

    private GameObject DialogueArrow;
    private TextMeshProUGUI DialogueBox;
    private TextMeshProUGUI DialogueAdvance;
    // Arrow flash variables
    public float arrowFlashDuration = 0.5f;
    public float wordDisplayInterval = 0.15f;
    private string currentDialogueText;
    private string[] currentDialoguePages;
    private int currentDialoguePageIndex;
    private string currentDialogueId;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Dialogue = GameObject.Find("Dialogue");
        DialogueBase = GameObject.Find("DialogueBase");
        DialogueArrow = GameObject.Find("DialogueArrow");
        DialogueBox = GameObject.Find("DialogueBox").GetComponent<TextMeshProUGUI>();
        DialogueAdvance = GameObject.Find("DialogueAdvance").GetComponent<TextMeshProUGUI>();
        dialogueActive = false;
        arrowActive = false;
        textDone = false;
        arrowFlashStarted = false;
    }

    void Update()
    {
        Dialogue.SetActive(dialogueActive);

        if (dialogueActive)
        {
            
            DialogueAdvance.text = "Press space to skip.";
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (!textDone)
                {
                    // Skip to full text
                    if (currentTextCoroutine != null)
                    {
                        StopCoroutine(currentTextCoroutine);
                    }
                    currentTextCoroutine = null;
                    DialogueBox.text = GetCurrentDialoguePage();
                    textDone = true;
                }
                else if (HasNextDialoguePage())
                {
                    ShowDialoguePage(currentDialoguePageIndex + 1);
                }
                else
                {
                    CloseDialogue();
                }
            }
        }

        if (textDone)
        {
            DialogueAdvance.text = "Press space to advance.";
            // TODO: Fix arrow flash speed increasing with each dialogue trigger
            // if (!arrowFlashStarted)
            // {
            //     ArrowFlash();
            // }

            // DialogueArrow.SetActive(arrowActive);
            DialogueArrow.SetActive(true);
        }
        else
        {
            DialogueArrow.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("TextTrigger"))
        {
            DialogueTrigger trigger = other.GetComponent<DialogueTrigger>();
            if (trigger == null) return;

            if (DialogueMemory.HasSeen(trigger.DialogueId))
            {
                other.gameObject.SetActive(false);
                return;
            }

            StartDialogue(trigger);
            gameObject.GetComponent<Player>().Suspend();
            gameObject.GetComponent<Player>().LockCamera();
            Time.timeScale = 0f;
            other.gameObject.SetActive(false);
        }
    }

    void StartDialogue(DialogueTrigger trigger)
    {
        dialogueActive = true;
        textDone = false;
        arrowFlashStarted = false;
        currentDialogueText = trigger.dialogueText;
        currentDialoguePages = BuildDialoguePages(currentDialogueText);
        currentDialoguePageIndex = 0;
        currentDialogueId = trigger.DialogueId;
        ShowDialoguePage(currentDialoguePageIndex);
    }

    void ShowDialoguePage(int pageIndex)
    {
        if (currentTextCoroutine != null)
        {
            StopCoroutine(currentTextCoroutine);
        }

        currentDialoguePageIndex = pageIndex;
        textDone = false;
        arrowFlashStarted = false;
        DialogueBox.text = "";
        currentTextCoroutine = StartCoroutine(DisplayTextWordByWord(GetCurrentDialoguePage()));
    }

    string[] BuildDialoguePages(string dialogueText)
    {
        string normalizedText = string.IsNullOrWhiteSpace(dialogueText)
            ? ""
            : dialogueText
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Replace("\\n", "\n")
                .Replace("[page]", "\n")
                .Replace("[PAGE]", "\n");

        string[] rawPages = normalizedText.Split('\n');
        List<string> pages = new List<string>();
        foreach (string rawPage in rawPages)
        {
            string page = rawPage.Trim();
            if (!string.IsNullOrWhiteSpace(page))
            {
                pages.Add(page);
            }
        }

        return pages.Count > 0 ? pages.ToArray() : new[] { "" };
    }

    string GetCurrentDialoguePage()
    {
        if (currentDialoguePages == null || currentDialoguePages.Length == 0) return "";
        return currentDialoguePages[Mathf.Clamp(currentDialoguePageIndex, 0, currentDialoguePages.Length - 1)];
    }

    bool HasNextDialoguePage()
    {
        return currentDialoguePages != null && currentDialoguePageIndex < currentDialoguePages.Length - 1;
    }

    void CloseDialogue()
    {
        if (currentTextCoroutine != null)
        {
            StopCoroutine(currentTextCoroutine);
            currentTextCoroutine = null;
        }

        DialogueMemory.MarkSeen(currentDialogueId);
        currentDialogueId = null;
        currentDialoguePages = null;
        currentDialoguePageIndex = 0;
        dialogueActive = false;
        gameObject.GetComponent<Player>().Unsuspend();
        gameObject.GetComponent<Player>().UnlockCamera();
    }

    void ArrowFlash()
    {
        arrowFlashStarted = true;
        arrowActive = true;
        StartCoroutine(ArrowFlashCoroutine());
    }

    private IEnumerator DisplayTextWordByWord(string text)
    {
        string[] words = text.Split(' ');
        string currentText = "";
        foreach (string word in words)
        {
            currentText += word + " ";
            DialogueBox.text = currentText;
            yield return new WaitForSecondsRealtime(wordDisplayInterval);
        }
        textDone = true;
        currentTextCoroutine = null;
    }

    private IEnumerator ArrowFlashCoroutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(arrowFlashDuration);
            arrowActive = !arrowActive;
        }
    }
}
