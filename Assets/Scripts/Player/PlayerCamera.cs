using UnityEngine;
using System.Runtime;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    static bool _hasSensitivityDefault;

    [SerializeField] Transform cameraTarget;
    [SerializeField] float defaultFov = 90f;
    [SerializeField] float mouseSensitivity = 0.15f;
    [Range(0, 90), SerializeField] float maxCameraPitch = 85f;

    [Header("Crouch")]
    [SerializeField] float cameraStandY = 1.65f;
    [SerializeField] float cameraCrouchY = 1f;
    [SerializeField] float cameraLerpSpeed = 12f;

    Vector3 _eulerAngles;
    public bool RotationLocked { get; private set; }
    public static float DefaultMouseSensitivity { get; private set; } = 0.15f;
    public static float MouseSensitivity { get; private set; } = 0.15f;
    public static float MouseSensitivitySliderMax => Mathf.Max(DefaultMouseSensitivity * 2f, 0.01f);

    void Awake()
    {
        if (!_hasSensitivityDefault)
        {
            DefaultMouseSensitivity = mouseSensitivity;
            MouseSensitivity = mouseSensitivity;
            _hasSensitivityDefault = true;
        }
        else
        {
            mouseSensitivity = MouseSensitivity;
        }

        Camera.main.fieldOfView = defaultFov;
        transform.position = cameraTarget.position;
        transform.eulerAngles = _eulerAngles = cameraTarget.eulerAngles;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (!RotationLocked)
        {
            Vector2 look = InputSystem.actions["Look"].ReadValue<Vector2>();
            UpdateRotation(look);
        }

        //move cameraTarget height based on crouch state
        float targetY = Player.Instance.IsCrouching ? cameraCrouchY : cameraStandY;
        Vector3 localPos = cameraTarget.localPosition;
        localPos.y = Mathf.Lerp(localPos.y, targetY, cameraLerpSpeed * Time.deltaTime);
        cameraTarget.localPosition = localPos;
    }

    void LateUpdate()
    {
        if (cameraTarget != null)
            transform.position = cameraTarget.position;
    }

    public void UpdateRotation(Vector2 look)
    {
        _eulerAngles.x += -look.y * mouseSensitivity;
        _eulerAngles.y += look.x * mouseSensitivity;
        _eulerAngles.x = Mathf.Clamp(_eulerAngles.x, -maxCameraPitch, maxCameraPitch);
        transform.eulerAngles = new Vector3(_eulerAngles.x, _eulerAngles.y, 0f);
    }

    public void SetRotationLocked(bool locked)
    {
        RotationLocked = locked;

        if (!locked)
        {
            _eulerAngles = transform.eulerAngles;
        }
    }

    public static void SetMouseSensitivity(float sensitivity)
    {
        MouseSensitivity = Mathf.Max(0f, sensitivity);

        foreach (PlayerCamera playerCamera in FindObjectsOfType<PlayerCamera>())
        {
            playerCamera.mouseSensitivity = MouseSensitivity;
        }
    }
}
