using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject settingsScreen;
    public UIController UIControllerInstance;
    public static bool isPaused = false;

    public Button resumeButton;
    public Button restartButton;
    public Button settingsButton;
    public Button exitButton;

    private List<Button> menuButtons = new List<Button>();
    private int currentButtonIndex = 0;
    


    private float selectedScale = 1.2f;
    private float defaultScale = 1f;
    private float animationDuration = 0.2f;
    private Color selectedColor = Color.white;
    private Color defaultColor = Color.black;
    private Color selectedBackgroundColor = new Color(0.23f, 0.23f, 0.23f, 0.7f); 
    private Color defaultBackgroundColor = new Color(0, 0, 0, 0);

    private void Awake()
    {
        if (resumeButton != null) menuButtons.Add(resumeButton);
        if (restartButton != null) menuButtons.Add(restartButton);
        if (settingsButton != null) menuButtons.Add(settingsButton);
        if (exitButton != null) menuButtons.Add(exitButton);

    }

    public void Start()
    {
        pauseMenu.SetActive(false);
        settingsScreen.SetActive(false);
        Debug.Log("PauseMenu initialized. Press Escape to toggle pause menu.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !settingsScreen.activeSelf)
        {
            Debug.Log("Escape key pressed. Toggling pause menu.");
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

    //private void OnNavigate(NavigationMoveEvent evt)
    //{
    //    if (!isPaused) return;

    //    switch (evt.direction)
    //    {
    //        case NavigationMoveEvent.Direction.Up:
    //            currentButtonIndex = WrapIndex(--currentButtonIndex, 0, menuButtons.Count - 1);
    //            break;
    //        case NavigationMoveEvent.Direction.Down:
    //            currentButtonIndex = WrapIndex(++currentButtonIndex, 0, menuButtons.Count - 1);
    //            break;
    //        case NavigationMoveEvent.Direction.None:
    //            break;
    //    }
    //    menuButtons[currentButtonIndex].Focus();
    //    UpdateButtonSelection();
    //}
    //private void UpdateButtonSelection()
    //{
    //    for (int i = 0; i < menuButtons.Count; i++)
    //    {
    //        if (i == currentButtonIndex)
    //        {
    //            menuButtons[i].AddToClassList("selected");
    //            menuButtons[i].Focus();
    //        }
    //        else
    //        {
    //            menuButtons[i].RemoveFromClassList("selected");
    //        }
    //    }
    //}

    public void PauseGame()
    {
        Time.timeScale = 0f;
        pauseMenu.SetActive(true);
        isPaused = true;
        currentButtonIndex = 0;
        //UIControllerInstance.musicPlayer.Pause();

    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);
        isPaused = false;
        //UIControllerInstance.musicPlayer.UnPause();
    }

    public void OpenSettings()
    {
        Debug.Log("Openning Settings...");
        settingsScreen.SetActive(true);
    }
    public void ExitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    private int WrapIndex(int indx, int min, int max)
    {
        int range = max - min + 1;
        return ((indx - min) % range + range) % range + min;
    }
}
