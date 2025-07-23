using UnityEngine;
using UnityEngine.Audio;

public class SettingMenuController : MonoBehaviour
{
    public GameObject settingMenu;
    public GameObject pauseMenu;

    private void Awake()
    {
        settingMenu.SetActive(false);

    }

    public void CloseSettingMenu()
    {
        settingMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }


    // ---------------------------------- //

    public AudioMixer mainMixer;

    public void SetMasterVolume(float value)
    {
        mainMixer.SetFloat("MasterMixer", value);
    }

    public void SetMusicVolume(float value)
    {
        mainMixer.SetFloat("MusicMixer", value);
    }

    public void SetSFXVolume(float value)
    {
        mainMixer.SetFloat("SoundMixer", value);
    }
}
