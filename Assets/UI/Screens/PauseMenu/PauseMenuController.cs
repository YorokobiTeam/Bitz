using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController instance;

    public bool isPaused { get ; private set; }

    private UIDocument uiDocument;
    private List<Button> menuButtons = new List<Button>();
    private int currentButtonIndex = 0;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

    }

    private void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        VisualElement root = uiDocument.rootVisualElement;
        
        // Find all buttons in the pause menu
        menuButtons.AddRange(root.Query<Button>().ToList());
        // Set the first button as focused
        if (menuButtons.Count > 0)
        {
            menuButtons[currentButtonIndex].Focus();
        }
        // Register navigation event
        root.RegisterCallback<NavigationMoveEvent>(OnNavigate);
    }

    private void Update()
    {
        if(!isPaused) return;
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            ResumeGame(); 
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
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
        isPaused = true;
        Time.timeScale = 0f;
        currentButtonIndex = 0; // Reset the index when pausing
        UpdateButtonSelection();
        uiDocument.rootVisualElement.style.display = DisplayStyle.Flex; // Show the pause menu
        // Maybe add disable spawning notes here and disable player's input?

    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }
    private int WrapIndex(int indx, int min, int max)
    {
        int range = max - min + 1;
        return ((indx - min) % range + range) % range + min;
    }
}
