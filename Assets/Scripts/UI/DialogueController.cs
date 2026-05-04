using UnityEngine;
using System.Collections;
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
    public float arrowFlashDuration = 1f;
    public float wordDisplayInterval = 0.3f;
    private string currentDialogueText;

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
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (!textDone)
                {
                    // Skip to full text
                    if (currentTextCoroutine != null)
                    {
                        StopCoroutine(currentTextCoroutine);
                    }
                    DialogueBox.text = currentDialogueText;
                    textDone = true;
                }
                else
                {
                    // Advance to next or close
                    dialogueActive = false;
                    DialogueAdvance.text = "Press space to skip.";
                }
            }
        }

        if (textDone)
        {
            DialogueAdvance.text = "Press space to advance.";
            if (!arrowFlashStarted)
            {
                ArrowFlash();
            }

            DialogueArrow.SetActive(arrowActive);
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
            StartDialogue(trigger);
        }
    }

    void StartDialogue(DialogueTrigger trigger)
    {
        dialogueActive = true;
        textDone = false;
        arrowFlashStarted = false;
        currentDialogueText = trigger.dialogueText;
        DialogueBox.text = "";
        currentTextCoroutine = StartCoroutine(DisplayTextWordByWord(currentDialogueText));
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
            yield return new WaitForSeconds(wordDisplayInterval);
        }
        textDone = true;
    }

    private IEnumerator ArrowFlashCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(arrowFlashDuration);
            arrowActive = !arrowActive;
        }
    }
}
