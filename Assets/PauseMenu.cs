using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject settingMenu;
    public UIController UIControllerInstance;
    public static bool isPaused;

    BitzHealth health;
    bool IsDead
    {
        get
        {
            if (health == null) health = FindFirstObjectByType<BitzHealth>();
            return health != null && health.IsDead;
        }
    }

    void Awake()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenu) pauseMenu.SetActive(false);
        if (settingMenu) settingMenu.SetActive(false);
    }

    void Update()
    {
        if (BitzAds.IsShowing || IsDead) return;
        var kb = Keyboard.current; // Android Back usually arrives as Escape; verify on device
        if (kb != null && kb.escapeKey.wasPressedThisFrame && !settingMenu.activeSelf) TogglePause();
    }

    public void TogglePause() { if (isPaused) ResumeGame(); else PauseGame(); }

    void OnApplicationPause(bool paused) { if (paused) AutoPause(); }
    void OnApplicationFocus(bool hasFocus) { if (!hasFocus) AutoPause(); }

    void AutoPause()
    {
        if (isPaused || BitzAds.IsShowing || IsDead) return;
        if (UIControllerInstance.gameEndScreen.activeSelf) return;
        if (UIControllerInstance.gameData.gameState == GameState.Ingame) PauseGame();
    }

    public void PauseGame()
    {
        if (IsDead) return;
        Time.timeScale = 0f;
        pauseMenu.SetActive(true);
        isPaused = true;
        UIControllerInstance.musicPlayer.Pause();
    }

    public void ResumeGame()
    {
        if (IsDead) return;
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);
        isPaused = false;
        UIControllerInstance.musicPlayer.UnPause();
    }

    public void OpenSettings() => settingMenu.SetActive(true);
    public void ExitGame() { Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); }
    public void RestartGame() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
}