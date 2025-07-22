using JetBrains.Annotations;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField]
    AudioSource musicCrossfadeChannelX;
    [SerializeField]
    AudioSource musicCrossfadeChannelY;
    [SerializeField, Range(0f, 3f)]
    float crossfadeDuration = 1.0f;

    [SerializeField]
    AudioSource soundEffectPlayer;

    public void Start()
    {
        this.musicCrossfadeChannelX.loop = true;
        this.musicCrossfadeChannelY.loop = true;
    }
    public void PlayMusic(AudioClip toPlay)
    {
        // Start crossfade for when nothing is playing
        if (this.musicCrossfadeChannelX.time == 0 && musicCrossfadeChannelY.time == 0)
        {
            musicCrossfadeChannelX.clip = toPlay;
            musicCrossfadeChannelX.volume = 0;
            LeanTween.value(this.gameObject, (val) =>
            {
                this.musicCrossfadeChannelX.volume = val;
            }, 0, 1, crossfadeDuration);

            musicCrossfadeChannelX.Play();

        }
        else
        {
            // Determine where the audio is coming from
            AudioSource playing;
            AudioSource toBePlayedIn;
            if (this.musicCrossfadeChannelX.time > 0 || this.musicCrossfadeChannelX.clip == null)
            {
                playing = musicCrossfadeChannelX;
                toBePlayedIn = musicCrossfadeChannelY;
            }
            else
            {
                playing = musicCrossfadeChannelY;
                toBePlayedIn = musicCrossfadeChannelX;
            }
            // Remove audio clip and silence the currently playing AS.
            LeanTween.value(this.gameObject, (val) =>
            {
                playing.volume = val;
            }, 1, 0, crossfadeDuration).setOnComplete(() =>
            {
                playing.time = 0;
                playing.clip = null;
                playing.Stop();
            });

            // Start the AS to be played in
            toBePlayedIn.clip = toPlay;
            toBePlayedIn.time = 0;
            LeanTween.value(gameObject, (val) =>
            {
                toBePlayedIn.volume = val;
            }, 0, 1, crossfadeDuration);
            toBePlayedIn.Play();


        }
    }

    public void PlaySfx(AudioClip sfx)
    {
        this.soundEffectPlayer.PlayOneShot(sfx);
    }
}
