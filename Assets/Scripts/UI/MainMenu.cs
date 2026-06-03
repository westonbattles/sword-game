using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public struct MainMenuLevel
{
    public string displayName;
    public string sceneName;
}

[Serializable]
public struct MainMenuLevelCategory
{
    public string categoryName;
    public MainMenuLevel[] levels;
}

public class MainMenu : MonoBehaviour
{
    enum MenuPage { Main, Categories, CategoryLevels }

    [SerializeField] Button playButton;
    [SerializeField] Button levelsButton;
    [SerializeField] string defaultLevelSceneName = "1-0 - Tutorial";
    [SerializeField] MainMenuLevelCategory[] levelCategories =
    {
        new MainMenuLevelCategory
        {
            categoryName = "Dungeon",
            levels = new[]
            {
                new MainMenuLevel { displayName = "1-0 - Tutorial", sceneName = "1-0 - Tutorial" },
                new MainMenuLevel { displayName = "1-1 - Fork", sceneName = "1-1 - Fork" }
            }
        },
        new MainMenuLevelCategory
        {
            categoryName = "Town",
            levels = new[]
            {
                new MainMenuLevel { displayName = "Town 2-0", sceneName = "Town 2-0" }
            }
        },
        new MainMenuLevelCategory
        {
            categoryName = "Castle",
            levels = new[]
            {
                new MainMenuLevel { displayName = "Castle 3-0", sceneName = "Castle 3-0" }
            }
        }
    };
    [SerializeField] float buttonSpacing = 80f;

    readonly List<Button> _pageButtons = new List<Button>();
    MenuPage _currentPage = MenuPage.Main;

    void Awake()
    {
        ResolveButtons();
        BuildMainButtons();
        CachePauseMenuButtonStyle();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            GoBack();
        }
    }

    void BuildMainButtons()
    {
        if (playButton == null) return;

        SetButtonText(playButton, "Play");
        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(PlayDefaultLevel);

        if (levelsButton == null)
        {
            levelsButton = Instantiate(playButton, playButton.transform.parent);
            levelsButton.name = "Levels";
            RectTransform levelsRect = levelsButton.GetComponent<RectTransform>();
            RectTransform playRect = playButton.GetComponent<RectTransform>();
            levelsRect.anchoredPosition = playRect.anchoredPosition + Vector2.down * buttonSpacing;
        }

        SetButtonText(levelsButton, "Levels");
        levelsButton.onClick.RemoveAllListeners();
        levelsButton.onClick.AddListener(ShowLevelCategories);
    }

    void ResolveButtons()
    {
        if (playButton == null)
        {
            Transform playTransform = transform.Find("Play");
            if (playTransform != null)
            {
                playButton = playTransform.GetComponent<Button>();
            }
        }

        if (levelsButton == null)
        {
            Transform levelsTransform = transform.Find("Levels");
            if (levelsTransform != null)
            {
                levelsButton = levelsTransform.GetComponent<Button>();
            }
        }
    }

    public void PlayDefaultLevel()
    {
        SceneManager.LoadScene(defaultLevelSceneName);
    }

    public void ShowLevelCategories()
    {
        if (playButton == null) return;

        _currentPage = MenuPage.Categories;
        ShowMainButtons(false);
        ClearPageButtons();

        Vector2 startPosition = GetCenteredStartPosition(levelCategories.Length + 1);

        for (int i = 0; i < levelCategories.Length; i++)
        {
            MainMenuLevelCategory category = levelCategories[i];
            string categoryName = string.IsNullOrWhiteSpace(category.categoryName) ? $"Category {i + 1}" : category.categoryName;
            Button categoryButton = CreatePageButton(categoryName, GetButtonPosition(startPosition, i));
            categoryButton.onClick.AddListener(() => ShowCategoryLevels(category));
        }

        Button backButton = CreatePageButton("Back", GetButtonPosition(startPosition, levelCategories.Length));
        backButton.onClick.AddListener(ShowMainMenu);
    }

    public void ShowCategoryLevels(MainMenuLevelCategory category)
    {
        if (playButton == null) return;

        _currentPage = MenuPage.CategoryLevels;
        ClearPageButtons();

        MainMenuLevel[] categoryLevels = category.levels ?? Array.Empty<MainMenuLevel>();
        Vector2 startPosition = GetCenteredStartPosition(categoryLevels.Length + 1);
        for (int i = 0; i < categoryLevels.Length; i++)
        {
            MainMenuLevel level = categoryLevels[i];
            string displayName = string.IsNullOrWhiteSpace(level.displayName) ? $"Level {i + 1}" : level.displayName;
            string sceneName = string.IsNullOrWhiteSpace(level.sceneName) ? displayName : level.sceneName;
            Button levelButton = CreatePageButton(displayName, GetButtonPosition(startPosition, i));
            levelButton.onClick.RemoveAllListeners();
            levelButton.onClick.AddListener(() => LoadLevel(sceneName));
        }

        Button backButton = CreatePageButton("Back", GetButtonPosition(startPosition, categoryLevels.Length));
        backButton.onClick.AddListener(ShowLevelCategories);
    }

    public void LoadLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    Button CreatePageButton(string text, Vector2 anchoredPosition)
    {
        Button button = Instantiate(playButton, playButton.transform.parent);
        button.name = text;
        button.gameObject.SetActive(true);
        button.onClick.RemoveAllListeners();
        SetButtonText(button, text);

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = anchoredPosition;

        _pageButtons.Add(button);
        return button;
    }

    void ClearPageButtons()
    {
        foreach (Button button in _pageButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        _pageButtons.Clear();
    }

    void ShowMainButtons(bool show)
    {
        playButton.gameObject.SetActive(show);
        if (levelsButton != null)
        {
            levelsButton.gameObject.SetActive(show);
        }
    }

    void ShowMainMenu()
    {
        _currentPage = MenuPage.Main;
        ClearPageButtons();
        ShowMainButtons(true);
    }

    void GoBack()
    {
        if (_currentPage == MenuPage.CategoryLevels)
        {
            ShowLevelCategories();
        }
        else if (_currentPage == MenuPage.Categories)
        {
            ShowMainMenu();
        }
    }

    void CachePauseMenuButtonStyle()
    {
        if (playButton == null) return;

        Image buttonImage = playButton.GetComponent<Image>();
        TMP_Text label = playButton.GetComponentInChildren<TMP_Text>();
        PauseMenu.CacheButtonStyle(buttonImage, label);
    }

    Vector2 GetCenteredStartPosition(int buttonCount)
    {
        if (buttonCount <= 0) return Vector2.zero;

        float topY = (buttonCount - 1) * buttonSpacing * 0.5f;
        return new Vector2(0f, topY);
    }

    Vector2 GetButtonPosition(Vector2 startPosition, int index)
    {
        return startPosition + Vector2.down * buttonSpacing * index;
    }

    void SetButtonText(Button button, string text)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = text;
        }
    }
}
