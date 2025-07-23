using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject settingMenu;

    public UIController UIControllerInstance;

    public static bool isPaused = false;

    private List<Button> menuButtons = new List<Button>();
    private int currentButtonIndex = 0;

    private void Awake()
    {
        pauseMenu.SetActive(false);
        settingMenu.SetActive(false);
    }

    public void Start()
    {
        pauseMenu.SetActive(false);
        settingMenu.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !settingMenu.activeSelf)
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

        UIControllerInstance.musicPlayer.Pause(); 

    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);
        isPaused = false;
        UIControllerInstance.musicPlayer.UnPause();
    }

    public void OpenSettings()
    {
        Debug.Log("Openning Settings...");
        settingMenu.SetActive(true);
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
