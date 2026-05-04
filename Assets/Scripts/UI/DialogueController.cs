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
    public float arrowFlashDuration = 0.5f;
    public float wordDisplayInterval = 0.15f;
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
            DialogueAdvance.text = "Press space to skip.";
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
                    gameObject.GetComponent<Player>().Unsuspend();
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
            StartDialogue(trigger);
            gameObject.GetComponent<Player>().Suspend();
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
            yield return new WaitForSecondsRealtime(wordDisplayInterval);
        }
        textDone = true;
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
