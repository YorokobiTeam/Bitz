using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PauseButtonUI : MonoBehaviour
{
    [SerializeField] PauseMenu pauseMenu;
    VisualElement button;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        root.pickingMode = PickingMode.Ignore;
        root.style.flexDirection = FlexDirection.Column;
        root.style.alignItems = Align.Center;             // horizontally centered
        root.style.justifyContent = Justify.FlexStart;    // top of the screen

        button = new VisualElement();
        var s = button.style;
        s.width = 120; s.height = 120;
        s.marginTop = 30;
        s.flexDirection = FlexDirection.Row;
        s.alignItems = Align.Center;
        s.justifyContent = Justify.Center;
        s.backgroundColor = new Color(0, 0, 0, 0.55f);
        s.borderTopLeftRadius = s.borderTopRightRadius =
        s.borderBottomLeftRadius = s.borderBottomRightRadius = 60;

        for (int i = 0; i < 2; i++)
        {
            var bar = new VisualElement();
            bar.pickingMode = PickingMode.Ignore;
            bar.style.width = 14; bar.style.height = 48;
            bar.style.marginLeft = bar.style.marginRight = 8;
            bar.style.backgroundColor = Color.white;
            bar.style.borderTopLeftRadius = bar.style.borderTopRightRadius =
            bar.style.borderBottomLeftRadius = bar.style.borderBottomRightRadius = 4;
            button.Add(bar);
        }

        button.RegisterCallback<ClickEvent>(_ => { if (!PauseMenu.isPaused) pauseMenu.PauseGame(); });
        root.Add(button);
    }

    void Update()
    {
        // hide while the pause menu is open or an ad is showing
        bool visible = !PauseMenu.isPaused && !BitzAds.IsShowing;
        button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}