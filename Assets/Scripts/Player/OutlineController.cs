using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;
using System;

public class OutlineController : MonoBehaviour
{
    [SerializeField] public Camera mainCamera;
    public GameObject textPrefab;
    [SerializeField] public GameObject ToastBackground;
    private Player _player;
    private Outline _currentOutline;
    private bool _interactPressed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public OutlineController(Player player)
    {
        _player = player;
    }

    void Start()
    {

        ToastBackground.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        _interactPressed = InputSystem.actions["Interact"].WasPressedThisFrame();
        Camera sourceCamera = mainCamera != null ? mainCamera : Camera.main;
        if (sourceCamera == null)
            return;

        Ray ray = new Ray(sourceCamera.transform.position, sourceCamera.transform.forward);
        RaycastHit hit;
        Outline hitOutline = null;

        if (Physics.Raycast(ray, out hit, 5f))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (hitObject.CompareTag("InteractableDialogue") || hitObject.CompareTag("InteractablePickup"))
            {
                hitOutline = hitObject.GetComponent<Outline>();
                ToastBackground.gameObject.SetActive(true);

            }
        }

        if (_currentOutline != hitOutline)
        {
            if (_currentOutline != null)
                _currentOutline.enabled = false;
                ToastBackground.gameObject.SetActive(false);

            _currentOutline = hitOutline;
        }

        if (_currentOutline != null) {
            _currentOutline.enabled = true;
        }

        if (_interactPressed)
        {
            if (_currentOutline != null)
            {
                if (_currentOutline.gameObject.CompareTag("InteractablePickup"))
                {
                    PickupItem();
                }
                else if (_currentOutline.gameObject.CompareTag("InteractableDialogue"))
                {
                    DialogueItem();
                }
            }
        }
    }


    void PickupItem()
    {
        if (_currentOutline != null)
        {
            _currentOutline.gameObject.SetActive(false);
            //rest of logic later lol
             Debug.Log("Picked up: " + _currentOutline.gameObject.name);
        }
    }

    void DialogueItem()
    {
        if (_currentOutline != null)
        {
            Debug.Log("Started dialogue with: " + _currentOutline.gameObject.name);
            GameObject loreText = Instantiate(textPrefab, transform.position + transform.forward * 2, Quaternion.identity);

            DialogueTrigger trigger = loreText.GetComponent<DialogueTrigger>();
            if (trigger != null)
            {
                trigger.dialogueText = _currentOutline.gameObject.GetComponent<DialogueTrigger>().dialogueText;
            }
            else
            {
                Debug.LogWarning("DialogueTrigger not found on loreText prefab");
            }
            Destroy(loreText, 5f);
        }
    }
}
