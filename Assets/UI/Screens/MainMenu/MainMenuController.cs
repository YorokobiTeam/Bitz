using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;


public class MainMenuController : MonoBehaviour
{
    [SerializeField]
    VisualTreeAsset mapCard;
    [SerializeField]
    StyleSheet mapCardStylesheet;

    private VisualElement menuPanelRoot;
    private VisualElement rightPanelRoot;
    private List<VisualElement> buttons = new();
    private List<VisualElement> maps = new();
    private VisualElement logo;
    private VisualElement root;

    private Label recentPlaysTitle;
    private UIDocument ui;

    private int leftPanelCurrentIndex = 0;
    private int rightPanelCurrentIndex = 0;
    private bool isLeft = true;
    private Button openFileButton;
    private bool importingMap;
    private List<MapMetadataObject> mapMetadataObjects;
    private VisualElement privacyDialog;


    public AudioImporter audioImporter;
    [SerializeField] AudioClip navigateSfx;

    private float bounceHeight = 20f;
    private float[] spectrum = new float[64];

    [SerializeField]
    SoundManager sound;

    public Texture2D defaultBackground;
    public AudioClip defaultMusic;

    public async void Start()
    {
        ui = GetComponent<UIDocument>();

        menuPanelRoot = ui.rootVisualElement.Q<VisualElement>("MenuPanel");

        menuPanelRoot = ui.rootVisualElement.Q<VisualElement>("MenuPanel");
        rightPanelRoot = ui.rootVisualElement.Q<VisualElement>("MapList");
        recentPlaysTitle = ui.rootVisualElement.Q<Label>("RecentPlaysTitle");
        root = ui.rootVisualElement.Q<VisualElement>("Root");
        logo = ui.rootVisualElement.Q<VisualElement>("VisualElement");

        menuPanelRoot.Children().FirstOrDefault().Focus();

        ui.rootVisualElement.Q<Button>("Exit").clicked += () =>
        {
            Application.Quit();
        };


        buttons.AddRange(menuPanelRoot.Children());
        ui.rootVisualElement.Q<Button>("AdPrivacy").clicked += ShowAdPrivacy;
        if (!BitzAds.HasAdvertisingChoice) ShowAdPrivacy();

        this.openFileButton = menuPanelRoot.Q<Button>("OpenMap");
        openFileButton.clicked += OpenFileButton_clicked;

        ui.rootVisualElement.RegisterCallback<NavigationMoveEvent>(HandleNavigation);
        await ReloadMaps();
        RelinkClasses();
        if (this.mapMetadataObjects.Count > 0)
        {
            root.style.backgroundImage = mapMetadataObjects[0].backgroundArtwork;
            sound.PlayMusic(await FileUtils.LoadAudio(Path.Join(Constants.APPLICATION_DATA,
                mapMetadataObjects[rightPanelCurrentIndex].mapData.identifier,
                mapMetadataObjects[rightPanelCurrentIndex].mapData.musicFileName),
                audioImporter));
        }
        else
        {
            root.style.backgroundImage = defaultBackground;
            sound.PlayMusic(defaultMusic);
        }

    }
    [SerializeField]
    GameData gameData;
    [SerializeField]
    UnityEngine.UI.Image sceneTransitioner;
    [SerializeField, Range(0.5f, 10f)]
    float sceneTransitionTime;
    public void PlayMap(MapMetadataObject map)
    {
        var basePath = Path.Combine(Constants.APPLICATION_DATA, map.mapData.identifier);
        gameData.beatMapFileDir = Path.Join(basePath, Constants.FILENAME_MAP);
        gameData.musicFileDir = Path.Join(basePath, map.mapData.musicFileName);
        gameData.albumCoverImageDir = Path.Join(basePath, map.mapData.albumCoverFileName);
        gameData.backgroundImageDir = Path.Join(basePath, map.mapData.backgroundFileName);
        LeanTween.move(sceneTransitioner.rectTransform, Vector3.zero, sceneTransitionTime).setEaseOutExpo().setOnComplete(() =>
        {
            SceneManager.LoadScene("BitzPlayer");
        });

    }

    public async Task ReloadMaps()
    {
        maps.Clear();
        rightPanelRoot.Clear();
        this.mapMetadataObjects = await FileUtils.LoadAllMaps(Constants.APPLICATION_DATA, this.audioImporter);

        foreach (var obj in mapMetadataObjects)
        {
            Debug.Log("boo");
            var ve = mapCard.Instantiate().Q<VisualElement>("Root");
            ve.styleSheets.Add(mapCardStylesheet);
            ve.style.flexGrow = 0;
            ve.dataSource = obj;
            ve.name = "MapCard";
            ve.RegisterCallback<PointerDownEvent>((callback) =>
            {
                PlayMap(obj);
            });
            rightPanelRoot.Add(ve);
        }
        maps.AddRange(rightPanelRoot.Children());

    }

    private async void OpenFileButton_clicked()
    {
        if (importingMap) return;
        importingMap = true;
        openFileButton.SetEnabled(false);
        try
        {
            if (await FileUtils.AddNewBitzmap(audioImporter, null))
            {
                await ReloadMaps();
                RelinkClasses();
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            importingMap = false;
            openFileButton.SetEnabled(true);
        }
    }
    [SerializeField, Range(0, 5f)]
    float backgroundFadeTime = 0.5f;

    private async void HandleNavigation(NavigationMoveEvent e)
    {
        if (privacyDialog != null) return;
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
                {
                    rightPanelCurrentIndex = WrapIndex(--rightPanelCurrentIndex, 0, maps.Count - 2);

                }

                break;
            case NavigationMoveEvent.Direction.Down:
                if (isLeft)
                {
                    leftPanelCurrentIndex = WrapIndex(++leftPanelCurrentIndex, 0, buttons.Count - 1);
                    shouldPlaySound = true;
                }
                else
                {
                    rightPanelCurrentIndex = WrapIndex(++rightPanelCurrentIndex, 0, maps.Count - 1);
                }
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
            sound.PlaySfx(navigateSfx);
        }


        if (isLeft) buttons[leftPanelCurrentIndex].Focus();
        else
        {

            maps[rightPanelCurrentIndex].Focus();
            // Fade the bg img
            LeanTween.value(gameObject, (val) =>
            {
                root.style.unityBackgroundImageTintColor = new Color(1, 1, 1, val);
            }, 1, 0, backgroundFadeTime).setOnComplete(() =>
            {
                root.style.backgroundImage = mapMetadataObjects[rightPanelCurrentIndex].backgroundArtwork;
                LeanTween.value(gameObject, (val) =>
                {
                    root.style.unityBackgroundImageTintColor = new Color(1, 1, 1, val);
                }, 0, 1, backgroundFadeTime);
            });


            sound.PlayMusic(await FileUtils.LoadAudio(Path.Join(Constants.APPLICATION_DATA, mapMetadataObjects[rightPanelCurrentIndex].mapData.identifier, mapMetadataObjects[rightPanelCurrentIndex].mapData.musicFileName), audioImporter));
        }
        RelinkClasses();


    }

    private void ShowAdPrivacy()
    {
        if (privacyDialog != null) return;
        root.SetEnabled(false);
        privacyDialog = new VisualElement();
        privacyDialog.style.position = Position.Absolute;
        privacyDialog.style.left = 0;
        privacyDialog.style.right = 0;
        privacyDialog.style.top = 0;
        privacyDialog.style.bottom = 0;
        privacyDialog.style.backgroundColor = new Color(0, 0, 0, 0.85f);
        privacyDialog.style.alignItems = Align.Center;
        privacyDialog.style.justifyContent = Justify.Center;
        privacyDialog.RegisterCallback<NavigationMoveEvent>(evt => evt.StopPropagation());

        var panel = new VisualElement();
        panel.style.width = Length.Percent(85);
        panel.style.maxWidth = 620;
        panel.style.paddingLeft = 24;
        panel.style.paddingRight = 24;
        panel.style.paddingTop = 24;
        panel.style.paddingBottom = 24;
        panel.style.backgroundColor = new Color(0.1f, 0.1f, 0.14f, 1);
        var title = new Label("Sandbox ad privacy");
        title.style.fontSize = 24;
        title.style.color = Color.white;
        title.style.whiteSpace = WhiteSpace.Normal;
        var explanation = new Label("Bitz can request sandbox test ads from mGAds. Ad requests may include device and advertising information. You can continue without ads and change this choice here at any time.");
        explanation.style.whiteSpace = WhiteSpace.Normal;
        explanation.style.color = Color.white;
        explanation.style.marginTop = 12;
        explanation.style.marginBottom = 16;
        panel.Add(title);
        panel.Add(explanation);
        var allow = new Button(() => SaveAdPrivacy(true)) { text = "Allow sandbox ads" };
        var decline = new Button(() => SaveAdPrivacy(false)) { text = "Continue without ads" };
        panel.Add(allow);
        panel.Add(decline);
        privacyDialog.Add(panel);
        ui.rootVisualElement.Add(privacyDialog);
        decline.Focus();
    }

    private void SaveAdPrivacy(bool allowed)
    {
        BitzAds.SetAdvertisingAllowed(allowed);
        privacyDialog.RemoveFromHierarchy();
        privacyDialog = null;
        root.SetEnabled(true);
        ui.rootVisualElement.Q<Button>("AdPrivacy").Focus();
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


    private int WrapIndex(int indx, int min, int max)
    {
        int range = max - min + 1;
        return ((indx - min) % range + range) % range + min;
    }




    enum InteractionSrc
    {
        Left,
        Right,
        None
    }
}
