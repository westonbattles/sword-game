using UnityEngine;
using System.Collections;
using TMPro;

public class OutlineController : MonoBehaviour
{
    [SerializeField] public Camera mainCamera;
    private Player _player;
    private Outline _currentOutline;
    [SerializeField] public GameObject ToastBackground;

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

        if (_currentOutline != null)
            _currentOutline.enabled = true;
    }

    void PickupItem()
    {
        if (_currentOutline != null)
        {
            // Implement item pickup logic here
            Debug.Log("Picked up: " + _currentOutline.gameObject.name);
        }
    }

    void DialogueItem()
    {
        if (_currentOutline != null)
        {
            // Implement dialogue logic here
            Debug.Log("Started dialogue with: " + _currentOutline.gameObject.name);
        }
    }
}
