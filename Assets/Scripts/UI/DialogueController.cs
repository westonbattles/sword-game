using UnityEngine;
using System.Collections;
using TMPro;

public class DialogueController : MonoBehaviour
{
    // State variables
    private bool dialogueActive = false;
    private bool arrowActive = false;
    // Dialogue objects
    private GameObject DialogueBase;

    private GameObject DialogueArrow;
    private TextMeshProUGUI DialogueBox;
    private TextMeshProUGUI DialogueAdvance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DialogueBase = GameObject.Find("DialogueBase");
        DialogueArrow = GameObject.Find("DialogueArrow");
        DialogueBox = GameObject.Find("DialogueBox").GetComponent<TextMeshProUGUI>();
        DialogueAdvance = GameObject.Find("DialogueAdvance").GetComponent<TextMeshProUGUI>();

        DialogueBase.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (dialogueActive)
        {
            DialogueBase.SetActive(true);
            DialogueBox.text = "Simply running a test right now";
        }
        else
        {
            DialogueBase.SetActive(false);
        }

        if (arrowActive)
        {
            ArrowFlash();
        }
        else
        {
            DialogueArrow.SetActive(false);
        }
    }

    void ArrowFlash()
    {
        DialogueArrow.SetActive(true);
        StartCoroutine(ArrowFlashCoroutine());
    }

    private IEnumerator ArrowFlashCoroutine()
    {
        arrowActive = true;
        yield return new WaitForSeconds(0.5f);
        arrowActive = false;
    }
}
