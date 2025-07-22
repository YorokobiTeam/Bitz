using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    private Label recentPlaysTitle;
    private UIDocument ui;

    private int leftPanelCurrentIndex = 0;
    private int rightPanelCurrentIndex = 0;
    private bool isLeft = true;
    private Button openFileButton;
    private List<MapMetadataObject> mapMetadataObjects;

    [SerializeField]
    AudioImporter audioImporter;


    public void Start()
    {
        ui = GetComponent<UIDocument>();

        menuPanelRoot = ui.rootVisualElement.Q<VisualElement>("MenuPanel");
        rightPanelRoot = ui.rootVisualElement.Q<VisualElement>("MapList");
        recentPlaysTitle = ui.rootVisualElement.Q<Label>("RecentPlaysTitle");
        // Add menu items to the left panel list
        //TODO: Initialize and place the root into the container.


        menuPanelRoot.Children().FirstOrDefault().Focus();


        buttons.AddRange(menuPanelRoot.Children());

        this.openFileButton = menuPanelRoot.Q<Button>("OpenMap");
        openFileButton.clicked += OpenFileButton_clicked;

        ui.rootVisualElement.RegisterCallback<NavigationMoveEvent>(HandleNavigation);
        ReloadMaps();
        RelinkClasses();



    }

    public async void PlayMap(MapMetadataObject map)
    {

    }

    public async void ReloadMaps()
    {

        this.mapMetadataObjects = await FileUtils.LoadAllMaps(Constants.APPLICATION_DATA, this.audioImporter);

        rightPanelRoot.Clear();
        foreach (var obj in mapMetadataObjects)
        {
            Debug.Log("boo");
            var ve = mapCard.Instantiate().Q<VisualElement>("Root");
            ve.styleSheets.Add(mapCardStylesheet);
            ve.style.flexGrow = 0;
            ve.dataSource = obj;
            ve.name = "MapCard";
            rightPanelRoot.Add(ve);
        }
        maps.AddRange(rightPanelRoot.Children());


    }

    private void OpenFileButton_clicked()
    {
        IProgress<string> progressReporter = new Progress<string>(callback =>
        {
            Debug.Log(callback);
        });
        FileUtils.AddNewBitzmap(this.audioImporter, progressReporter);
    }


    private void HandleNavigation(NavigationMoveEvent e)
    {
        switch (e.direction)
        {
            case NavigationMoveEvent.Direction.Up:
                if (isLeft)
                    leftPanelCurrentIndex = WrapIndex(--leftPanelCurrentIndex, 0, buttons.Count - 1);
                else
                    rightPanelCurrentIndex = WrapIndex(--rightPanelCurrentIndex, 0, maps.Count - 2);
                break;
            case NavigationMoveEvent.Direction.Down:
                if (isLeft)
                    leftPanelCurrentIndex = WrapIndex(++leftPanelCurrentIndex, 0, buttons.Count - 1);
                else
                    rightPanelCurrentIndex = WrapIndex(++rightPanelCurrentIndex, 0, maps.Count - 1);
                break;
            case NavigationMoveEvent.Direction.Left:
            case NavigationMoveEvent.Direction.Right:
                isLeft = !isLeft;
                break;
        }
        ;

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

    enum InteractionSrc
    {
        Left,
        Right,
        None
    }
}
