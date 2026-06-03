using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    const string MainMenuSceneName = "StartScreen";

    static Sprite _cachedButtonSprite;
    static TMP_FontAsset _cachedButtonFont;
    static Material _cachedButtonFontMaterial;
    static Color _cachedButtonColor = Color.black;
    static Color _cachedTextColor = new Color(1f, 0.8941177f, 0.7686275f, 1f);

    Canvas _canvas;
    GameObject _overlay;
    GameObject _pauseButtons;
    GameObject _settingsPanel;
    bool _isPaused;
    bool _lockedPlayerCamera;
    CursorLockMode _previousLockState;
    bool _previousCursorVisible;

    private GammaController brightnessChanger;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateRuntimePauseMenu()
    {
        if (FindObjectOfType<PauseMenu>() != null) return;

        GameObject pauseMenuObject = new GameObject("PauseMenu");
        DontDestroyOnLoad(pauseMenuObject);
        pauseMenuObject.AddComponent<PauseMenu>();
    }

    public static void CacheButtonStyle(Image buttonImage, TMP_Text buttonText)
    {
        if (buttonImage != null)
        {
            _cachedButtonSprite = buttonImage.sprite;
            _cachedButtonColor = buttonImage.color;
        }

        if (buttonText != null)
        {
            _cachedButtonFont = buttonText.font;
            _cachedButtonFontMaterial = buttonText.fontSharedMaterial;
            _cachedTextColor = buttonText.color;
        }
    }

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        brightnessChanger = FindObjectOfType<GammaController>();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name == MainMenuSceneName) return;
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (_isPaused)
        {
            if (_settingsPanel != null && _settingsPanel.activeSelf)
            {
                ShowPauseButtons();
                return;
            }

            Resume();
            return;
        }

        if (Time.timeScale == 0f) return;

        Pause();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == MainMenuSceneName)
        {
            _isPaused = false;
            if (_overlay != null)
            {
                _overlay.SetActive(false);
            }
            ShowPauseButtons();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;
            return;
        }

        if (_isPaused)
        {
            Resume();
        }

        Time.timeScale = 1f;
    }

    void Pause()
    {
        EnsurePauseUi();

        _isPaused = true;
        _previousLockState = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;
        if (Player.Instance != null)
        {
            Player.Instance.LockCamera();
            _lockedPlayerCamera = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _overlay.SetActive(true);
        Time.timeScale = 0f;
    }

    void Resume()
    {
        _isPaused = false;
        if (_overlay != null)
        {
            _overlay.SetActive(false);
        }

        if (_lockedPlayerCamera && Player.Instance != null)
        {
            Player.Instance.UnlockCamera();
        }
        _lockedPlayerCamera = false;

        Cursor.lockState = _previousLockState;
        Cursor.visible = _previousCursorVisible;
        Time.timeScale = 1f;
    }

    void LoadMainMenu()
    {
        if (_lockedPlayerCamera && Player.Instance != null)
        {
            Player.Instance.UnlockCamera();
        }
        _lockedPlayerCamera = false;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    void EnsurePauseUi()
    {
        if (_overlay != null) return;

        EnsureEventSystem();

        GameObject canvasObject = new GameObject("PauseMenuCanvas");
        canvasObject.transform.SetParent(transform);
        _canvas = canvasObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 1000;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        _overlay = new GameObject("PauseMenuOverlay");
        _overlay.transform.SetParent(canvasObject.transform, false);
        RectTransform overlayRect = _overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image dimmer = _overlay.AddComponent<Image>();
        dimmer.color = new Color(0f, 0f, 0f, 0.45f);

        _pauseButtons = new GameObject("PauseButtons");
        _pauseButtons.transform.SetParent(_overlay.transform, false);
        RectTransform pauseButtonsRect = _pauseButtons.AddComponent<RectTransform>();
        pauseButtonsRect.anchorMin = Vector2.zero;
        pauseButtonsRect.anchorMax = Vector2.one;
        pauseButtonsRect.offsetMin = Vector2.zero;
        pauseButtonsRect.offsetMax = Vector2.zero;

        Button settingsButton = CreatePauseButton(_pauseButtons.transform, "Settings", new Vector2(0f, 40f));
        settingsButton.onClick.AddListener(ShowSettingsPanel);

        Button mainMenuButton = CreatePauseButton(_pauseButtons.transform, "Main Menu", new Vector2(0f, -40f));
        mainMenuButton.onClick.AddListener(LoadMainMenu);

        CreateSettingsPanel();
        _overlay.SetActive(false);
    }

    Button CreatePauseButton(Transform parent, string label, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(label);
        buttonObject.transform.SetParent(parent, false);
        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(290f, 58f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = _cachedButtonColor;
        if (_cachedButtonSprite != null)
        {
            buttonImage.sprite = _cachedButtonSprite;
        }

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        GameObject textObject = new GameObject("Text (TMP)");
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.color = _cachedTextColor;
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        if (_cachedButtonFont != null)
        {
            text.font = _cachedButtonFont;
        }
        if (_cachedButtonFontMaterial != null)
        {
            text.fontSharedMaterial = _cachedButtonFontMaterial;
        }

        return button;
    }

    void CreateSettingsPanel()
    {
        _settingsPanel = new GameObject("SettingsPanel");
        _settingsPanel.transform.SetParent(_overlay.transform, false);

        RectTransform panelRect = _settingsPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(420f, 300f);

        Image panelImage = _settingsPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.9f);

        CreateSettingsText(_settingsPanel.transform, "Settings", new Vector2(0f, 105f), 30f);
        CreateSettingsText(_settingsPanel.transform, "Sensitivity", new Vector2(-125f, 40f), 22f);
        CreateSettingsText(_settingsPanel.transform, "Music", new Vector2(-125f, -25f), 22f);
        CreateSettingsText(_settingsPanel.transform, "Brightness", new Vector2(-125f, -90f), 22f);

        Slider sensitivitySlider = CreateSettingsSlider(_settingsPanel.transform, "SensitivitySlider", new Vector2(55f, 40f));
        sensitivitySlider.minValue = 0f;
        sensitivitySlider.maxValue = PlayerCamera.MouseSensitivitySliderMax;
        sensitivitySlider.value = PlayerCamera.MouseSensitivity;
        sensitivitySlider.onValueChanged.AddListener(PlayerCamera.SetMouseSensitivity);

        Slider musicSlider = CreateSettingsSlider(_settingsPanel.transform, "MusicSlider", new Vector2(55f, -25f));
        musicSlider.value = BackgroundMusicPlayer.MusicVolume;
        musicSlider.onValueChanged.AddListener(BackgroundMusicPlayer.SetMusicVolume);

        Slider brightnessSlider = CreateSettingsSlider(_settingsPanel.transform, "BrightnessSlider", new Vector2(55f, -90f));
        brightnessSlider.minValue = -1f;
        brightnessSlider.maxValue = 1f;
        brightnessSlider.value = 0.2f;
        brightnessSlider.onValueChanged.AddListener(brightnessChanger.AdjustSceneGamma);

        Button backButton = CreatePauseButton(_settingsPanel.transform, "Back", new Vector2(0f, -170f));
        backButton.onClick.AddListener(ShowPauseButtons);

        _settingsPanel.SetActive(false);
    }

    TextMeshProUGUI CreateSettingsText(Transform parent, string label, Vector2 anchoredPosition, float fontSize)
    {
        GameObject textObject = new GameObject(label);
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = anchoredPosition;
        textRect.sizeDelta = new Vector2(260f, 45f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.color = _cachedTextColor;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        if (_cachedButtonFont != null)
        {
            text.font = _cachedButtonFont;
        }
        if (_cachedButtonFontMaterial != null)
        {
            text.fontSharedMaterial = _cachedButtonFontMaterial;
        }

        return text;
    }

    Slider CreateSettingsSlider(Transform parent, string sliderName, Vector2 anchoredPosition)
    {
        GameObject sliderObject = new GameObject(sliderName);
        sliderObject.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = anchoredPosition;
        sliderRect.sizeDelta = new Vector2(220f, 24f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        Image background = CreateSliderImage(sliderObject.transform, "Background", Color.black);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.25f);
        backgroundRect.anchorMax = new Vector2(1f, 0.75f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillAreaObject = new GameObject("Fill Area");
        fillAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillAreaObject.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        Image fill = CreateSliderImage(fillAreaObject.transform, "Fill", _cachedTextColor);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image handle = CreateSliderImage(sliderObject.transform, "Handle", _cachedTextColor);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(22f, 22f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;

        return slider;
    }

    Image CreateSliderImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);
        RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    void ShowSettingsPanel()
    {
        _pauseButtons.SetActive(false);
        _settingsPanel.SetActive(true);
    }

    void ShowPauseButtons()
    {
        if (_pauseButtons != null)
        {
            _pauseButtons.SetActive(true);
        }
        if (_settingsPanel != null)
        {
            _settingsPanel.SetActive(false);
        }
    }

    void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        DontDestroyOnLoad(eventSystemObject);
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }
}
