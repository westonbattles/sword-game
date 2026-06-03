using UnityEngine;
using System.Runtime;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class PlayerCamera : MonoBehaviour
{
    static bool _hasSensitivityDefault;
    const string ViewModelLayerName = "Player";

    [SerializeField] Transform cameraTarget;
    [SerializeField] float defaultFov = 90f;
    [SerializeField] float mouseSensitivity = 0.15f;
    [Range(0, 90), SerializeField] float maxCameraPitch = 85f;

    [Header("Crouch")]
    [SerializeField] float cameraStandY = 1.65f;
    [SerializeField] float cameraCrouchY = 1f;
    [SerializeField] float cameraLerpSpeed = 12f;

    Vector3 _eulerAngles;
    Camera _mainCamera;
    Camera _viewModelCamera;
    int _viewModelLayer = -1;
    int _viewModelLayerMask;
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

        _mainCamera = Camera.main;
        if (_mainCamera != null)
        {
            _mainCamera.fieldOfView = defaultFov;
            SetupViewModelOverlayCamera();
        }
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

        SyncViewModelOverlayCamera();
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

    void SetupViewModelOverlayCamera()
    {
        if (_mainCamera == null) return;

        _viewModelLayer = LayerMask.NameToLayer(ViewModelLayerName);
        if (_viewModelLayer < 0) return;

        _viewModelLayerMask = 1 << _viewModelLayer;
        Transform viewModel = transform.Find("ViewModel");
        if (viewModel != null)
        {
            SetLayerRecursively(viewModel, _viewModelLayer);
        }

        _mainCamera.cullingMask &= ~_viewModelLayerMask;

        GameObject overlayCameraObject = new GameObject("ViewModelOverlayCamera");
        overlayCameraObject.transform.SetParent(_mainCamera.transform, false);
        _viewModelCamera = overlayCameraObject.AddComponent<Camera>();
        SyncViewModelOverlayCamera();

        _viewModelCamera.clearFlags = CameraClearFlags.Depth;
        _viewModelCamera.cullingMask = _viewModelLayerMask;
        _viewModelCamera.depth = _mainCamera.depth + 1f;
        _viewModelCamera.useOcclusionCulling = false;

        UniversalAdditionalCameraData mainCameraData = _mainCamera.GetUniversalAdditionalCameraData();
        UniversalAdditionalCameraData overlayCameraData = _viewModelCamera.GetUniversalAdditionalCameraData();
        overlayCameraData.renderType = CameraRenderType.Overlay;
        overlayCameraData.renderPostProcessing = false;
        overlayCameraData.requiresDepthTexture = false;
        overlayCameraData.requiresColorTexture = false;

        if (!mainCameraData.cameraStack.Contains(_viewModelCamera))
        {
            mainCameraData.cameraStack.Add(_viewModelCamera);
        }
    }

    void SyncViewModelOverlayCamera()
    {
        if (_mainCamera == null || _viewModelCamera == null) return;

        _viewModelCamera.fieldOfView = _mainCamera.fieldOfView;
        _viewModelCamera.nearClipPlane = _mainCamera.nearClipPlane;
        _viewModelCamera.farClipPlane = _mainCamera.farClipPlane;
        _viewModelCamera.rect = _mainCamera.rect;
        _viewModelCamera.allowHDR = _mainCamera.allowHDR;
        _viewModelCamera.allowMSAA = _mainCamera.allowMSAA;
    }

    static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;

        foreach (Transform child in root)
        {
            SetLayerRecursively(child, layer);
        }
    }
}
