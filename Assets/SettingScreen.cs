using UnityEngine;
using UnityEngine.Audio;

public class SettingScreen : MonoBehaviour
{
    public GameObject pauseMenu;

    private void Awake()
    {
        this.gameObject.SetActive(false); 
    }

    public void CloseSetting()
    {
        Debug.Log("Closing settings screen.");
        this.gameObject.SetActive(false);
    }


    // --------------------------------------- //

    public AudioMixer mainMixer;

    public void SetMasterVolume(float value)
    {
        mainMixer.SetFloat("MainMixerMaster", value);
    }

    public void SetMusicVolume(float value)
    {
        mainMixer.SetFloat("MainMixerMusic", value);
    }

    public void SetSFXVolume(float value)
    {
        mainMixer.SetFloat("MainMixerSound", value);
    }
}
