using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

public class OutlineController : MonoBehaviour
{
    [SerializeField] public Camera mainCamera;
    private Player _player;
    private Outline _currentOutline;
    [SerializeField] public GameObject ToastBackground;
    bool _interactPressed;

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
        _interactPressed = InputSystem.actions["Interact"].IsPressed();
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
            // Implement dialogue logic here
            Debug.Log("Started dialogue with: " + _currentOutline.gameObject.name);
            GameObject dialogueTrigger = new GameObject("LoreTrigger");
            dialogueTrigger.tag = "TextTrigger";
            dialogueTrigger.transform.position = _player.gameObject.transform.position;
        }
    }
}
