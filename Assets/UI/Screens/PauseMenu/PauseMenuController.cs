using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class PauseMenuController : MonoBehaviour
{
    public GameObject pauseMenu;

    public bool isPaused { get; private set; } = false;

    private UIDocument uiDocument;
    private List<Button> menuButtons = new List<Button>();
    private int currentButtonIndex = 0;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
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
        UpdateButtonSelection();
        uiDocument.rootVisualElement.style.display = DisplayStyle.Flex; // Show the pause menu
        // Maybe add disable spawning notes here and disable player's input?

    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);
        isPaused = false;
        uiDocument.rootVisualElement.style.display = DisplayStyle.None; // Hide the pause menu
    }
    private int WrapIndex(int indx, int min, int max)
    {
        int range = max - min + 1;
        return ((indx - min) % range + range) % range + min;
    }
}
