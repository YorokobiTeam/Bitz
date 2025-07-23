using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;

    public UIController UIControllerInstance;

    public static bool isPaused = false;

    public UIDocument uiDocument;
    private List<Button> menuButtons = new List<Button>();
    private int currentButtonIndex = 0;
    


    public void Start()
    {
        var root = uiDocument.rootVisualElement;

        var resumeButton = root.Q<Button>("ResumeBtn");
        var settingsButton = root.Q<Button>("SettingsBtn");
        var exitButton = root.Q<Button>("ExitBtn");

        resumeButton.clicked += ResumeGame;
        settingsButton.clicked += OpenSettings;
        exitButton.clicked += ExitGame;
        menuButtons.Add(resumeButton);
        menuButtons.Add(settingsButton);
        menuButtons.Add(exitButton);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

    }

    private void OnNavigate(NavigationMoveEvent evt)
    {
        if (!isPaused) return;

        switch (evt.direction)
        {
            case NavigationMoveEvent.Direction.Up:
                currentButtonIndex = WrapIndex(--currentButtonIndex, 0, menuButtons.Count - 1);
                break;
            case NavigationMoveEvent.Direction.Down:
                currentButtonIndex = WrapIndex(++currentButtonIndex, 0, menuButtons.Count - 1);
                break;
            case NavigationMoveEvent.Direction.None:
                break;
        }
        menuButtons[currentButtonIndex].Focus();
        UpdateButtonSelection();
    }
    private void UpdateButtonSelection()
    {
        for (int i = 0; i < menuButtons.Count; i++)
        {
            if (i == currentButtonIndex)
            {
                menuButtons[i].AddToClassList("selected");
                menuButtons[i].Focus();
            }
            else
            {
                menuButtons[i].RemoveFromClassList("selected");
            }
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        pauseMenu.SetActive(true);
        isPaused = true;
        currentButtonIndex = 0;
        UIControllerInstance.musicPlayer.Pause(); // Pause the music player

        //UpdateButtonSelection();
        //uiDocument.rootVisualElement.style.display = DisplayStyle.Flex; // Show the pause menu
        //// Maybe add disable spawning notes here and disable player's input?

    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);
        isPaused = false;
        UIControllerInstance.musicPlayer.UnPause();
        //uiDocument.rootVisualElement.style.display = DisplayStyle.None; // Hide the pause menu
    }

    public void OpenSettings()
    {
        Debug.Log("Openning Settings...");
    }
    public void ExitGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene("MainMenu"); // Assuming you have a MainMenu scene
    }
    private int WrapIndex(int indx, int min, int max)
    {
        int range = max - min + 1;
        return ((indx - min) % range + range) % range + min;
    }
}
