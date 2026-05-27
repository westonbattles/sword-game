using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelEndScreen : MonoBehaviour
{

    public GameObject endPanel; // Level End Screen
    public Button retryLevelButton;
    public Button nextLevelButton;
    public Button mainMenuButton;
    public string nextLevelName;

    public GameObject mainMenuScreen;

    public GameObject Player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        endPanel.SetActive(false);
        mainMenuScreen.SetActive(false);

        retryLevelButton.onClick.AddListener(retryLevel);
        nextLevelButton.onClick.AddListener(nextLevel);
        mainMenuButton.onClick.AddListener(mainMenu);
    }

    void Update()
    {
        bool skipLevel = Input.GetKeyDown(KeyCode.N);
        if (skipLevel)
        {
            nextLevel();
        }
    }

    public void levelEnd()
    {
        Player.GetComponent<Player>().Suspend();
        endPanel.SetActive(true);
    }

    void retryLevel()
    {
        UnityEngine.Debug.Log("Retry Pressed");
        Player.GetComponent<Player>().Unsuspend();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void nextLevel()
    {
        UnityEngine.Debug.Log("Next Level Pressed");
        Player.GetComponent<Player>().Unsuspend();
        SceneManager.LoadScene(nextLevelName);
    }

    void mainMenu()
    {
        UnityEngine.Debug.Log("Main Menu Pressed");
        return; // Placeholder, eventually will call the main menu script to wake that one up and then put this one to sleep
    }
}
