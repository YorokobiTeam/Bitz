using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityEngine.UIElements;


public class MainMenuController : MonoBehaviour
{
    private VisualElement menuPanelRoot;
    private VisualElement rightPanelRoot;
    private VisualElement logo;
    private List<VisualElement> buttons = new List<VisualElement>();
    private List<VisualElement> maps = new List<VisualElement>();

    private Label recentPlaysTitle;
    private UIDocument ui;

    private int leftPanelCurrentIndex = 0;
    private int rightPanelCurrentIndex = 0;
    private bool isLeft = true;

    [SerializeField] AudioClip move;
    private AudioSource audioSource;
    [SerializeField] AudioClip backgroundMusic;
    private AudioSource musicPlayer;

    private float bounceHeight = 20f;
    private float[] spectrum = new float[64];

    [SerializeField]
    SoundManager manager;


    public void Start()
    {
        ui = GetComponent<UIDocument>();
        audioSource = GetComponent<AudioSource>();
        musicPlayer = GetComponent<AudioSource>();
        musicPlayer.PlayOneShot(backgroundMusic);

        menuPanelRoot = ui.rootVisualElement.Q<VisualElement>("MenuPanel");
        rightPanelRoot = ui.rootVisualElement.Q<VisualElement>("MapList");
        recentPlaysTitle = ui.rootVisualElement.Q<Label>("RecentPlaysTitle");
        // Add menu items to the left panel list
        //TODO: Initialize and place the root into the container.
        logo = ui.rootVisualElement.Q<VisualElement>("VisualElement");


        menuPanelRoot.Children().FirstOrDefault().Focus();


        buttons.AddRange(menuPanelRoot.Children());
        maps.AddRange(rightPanelRoot.Children());

        ui.rootVisualElement.RegisterCallback<NavigationMoveEvent>(HandleNavigation);
        RelinkClasses();

        AnimateLogoBounceToMusic();
    }

    private void HandleNavigation(NavigationMoveEvent e)
    {
        bool shouldPlaySound = false;

        switch (e.direction)
        {
            case NavigationMoveEvent.Direction.Up:
                if (isLeft)
                {
                    leftPanelCurrentIndex = WrapIndex(--leftPanelCurrentIndex, 0, buttons.Count - 1);
                    shouldPlaySound = true;
                }
                else
                    rightPanelCurrentIndex = WrapIndex(--rightPanelCurrentIndex, 0, maps.Count - 2);
                break;
            case NavigationMoveEvent.Direction.Down:
                if (isLeft)
                {
                    leftPanelCurrentIndex = WrapIndex(++leftPanelCurrentIndex, 0, buttons.Count - 1);
                    shouldPlaySound = true;
                }
                else
                    rightPanelCurrentIndex = WrapIndex(++rightPanelCurrentIndex, 0, maps.Count - 1);
                break;
            case NavigationMoveEvent.Direction.Left:
            case NavigationMoveEvent.Direction.Right:
                isLeft = !isLeft;
                shouldPlaySound = true;

                break;
        }
        ;

        if (shouldPlaySound == true)
        {
            PlayMoveSound();
        }

        if (isLeft) buttons[leftPanelCurrentIndex].Focus();
        else maps[rightPanelCurrentIndex].Focus();
        RelinkClasses();


    }

    private void RelinkClasses(InteractionSrc interactionSrc = InteractionSrc.None)
    {
        if (interactionSrc == InteractionSrc.None || interactionSrc == InteractionSrc.Left)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].ClearClassList();
                buttons[i].AddToClassList("main-menu-button");
                if (isLeft)
                {
                    buttons[i].AddToClassList("menu-dist-" + (isLeft ? Math.Abs(i - leftPanelCurrentIndex) : 2));
                }
            }
        }
        if (interactionSrc == InteractionSrc.None || interactionSrc == InteractionSrc.Right)
        {
            for (int i = 0; i < maps.Count; i++)
            {
                maps[i].ClearClassList();
                maps[i].AddToClassList("enlarge-card-" + Math.Abs(i - rightPanelCurrentIndex));
            }
        }

        if (!isLeft)
        {
            recentPlaysTitle.AddToClassList("recent-plays-title-selected");

        }
        else
        {
            recentPlaysTitle.RemoveFromClassList("recent-plays-title-selected");
        }
    }

    private void PopulateRightPanelTree(VisualElement ve)
    {
        //todo: read file from Bitz/user/<0 if unauth> <id if auth>/history.json
    }

    private int WrapIndex(int indx, int min, int max)
    {
        int range = max - min + 1;
        return ((indx - min) % range + range) % range + min;
    }

    private void PlayMoveSound()
    {
        if (audioSource != null && move != null)
        {
            audioSource.PlayOneShot(move);
        }
    }

    private void AnimateLogoBounceToMusic()
    {
        logo.schedule.Execute(() =>
        {
            //Get audio spectrum data
            musicPlayer.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

            //Low frequency analysis to generate rhythm
            float intensity = 0f;
            for (int i = 1; i <= 5; i++)
            {
                intensity += spectrum[i];
            }
            intensity *= 400f;

            float yOffset = Mathf.Clamp(intensity, 0, bounceHeight);
            //make the logo bounce
            logo.style.scale = new Scale(new Vector3(1.2f, 1.2f));

        }).Every(16); //60fps
    }

    enum InteractionSrc
    {
        Left,
        Right,
        None
    }
}
