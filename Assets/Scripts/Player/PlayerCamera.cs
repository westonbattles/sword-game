using UnityEngine;
using System.Runtime;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] Transform cameraTarget;
    [SerializeField] float defaultFov = 90f;
    [SerializeField] float mouseSensitivity = 0.15f;
    [Range(0, 90), SerializeField] float maxCameraPitch = 85f;
    Vector3 _eulerAngles;

    void Awake()
    {
        Camera.main.fieldOfView = defaultFov;
        transform.position = cameraTarget.position;
        transform.eulerAngles = _eulerAngles = cameraTarget.eulerAngles;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Vector2 look = InputSystem.actions["Look"].ReadValue<Vector2>(); //Vector2 look = new Vector2(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y"));
        Vector2 rawLook = look;
        UpdateRotation(rawLook);
    }

    void LateUpdate()
    {
        if (cameraTarget != null)
        {
            transform.position = cameraTarget.position; 
        }
    }

    public void UpdateRotation(Vector2 look)
    {
        _eulerAngles.x += -look.y * mouseSensitivity;
        _eulerAngles.y += look.x * mouseSensitivity;
        _eulerAngles.x = Mathf.Clamp(_eulerAngles.x, -maxCameraPitch, maxCameraPitch);
        transform.eulerAngles = new Vector3(_eulerAngles.x, _eulerAngles.y, 0f);
    }
}
