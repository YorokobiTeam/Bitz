using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class TouchPadsUI : MonoBehaviour
{
    [SerializeField] InputController input;
    [SerializeField] bool showOnDesktop = true; // untick once testing is done

    // Same order as InputController.Lanes: Left, Down, Up, Right
    static readonly float[] ChevronAngles = { 45f, -45f, 135f, 225f };
    static readonly Color Idle = new Color(0, 0, 0, 0.35f);
    static readonly Color Pressed = new Color(1, 1, 1, 0.45f);

    VisualElement container;
    readonly VisualElement[] pads = new VisualElement[4];

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        container = new VisualElement { pickingMode = PickingMode.Ignore };
        var c = container.style;
        c.position = Position.Absolute;
        c.left = 0; c.right = 0; c.bottom = 0;
        c.height = Length.Percent(input.PadAreaHeight * 100f);
        c.flexDirection = FlexDirection.Row;

        for (int i = 0; i < 4; i++)
        {
            // Transparent cell = the full touch quarter; it only centers the visible pad
            var cell = new VisualElement { pickingMode = PickingMode.Ignore };
            cell.style.flexGrow = 1; cell.style.flexBasis = 0;
            cell.style.alignItems = Align.Center;
            cell.style.justifyContent = Justify.Center;

            var pad = new VisualElement { pickingMode = PickingMode.Ignore };
            var s = pad.style;
            s.width = Length.Percent(75);
            s.height = Length.Percent(50);
            s.alignItems = Align.Center;
            s.justifyContent = Justify.Center;
            s.backgroundColor = Idle;
            s.borderTopLeftRadius = s.borderTopRightRadius =
            s.borderBottomLeftRadius = s.borderBottomRightRadius = 24;

            var chevron = new VisualElement { pickingMode = PickingMode.Ignore };
            var cs = chevron.style;
            cs.width = 52; cs.height = 52;
            cs.borderLeftWidth = 12; cs.borderBottomWidth = 12;
            cs.borderLeftColor = cs.borderBottomColor = Color.white;
            cs.rotate = new Rotate(new Angle(ChevronAngles[i], AngleUnit.Degree));
            pad.Add(chevron);

            cell.Add(pad);
            pads[i] = pad;
            container.Add(cell);
        }

        root.Add(container);
        input.LanePressed += Flash;
    }

    void OnDisable()
    {
        if (input != null) input.LanePressed -= Flash;
    }

    void Flash(int lane)
    {
        var pad = pads[lane];
        pad.style.backgroundColor = Pressed;
        pad.schedule.Execute(() => pad.style.backgroundColor = Idle).ExecuteLater(100);
    }

    void Update()
    {
        bool show = (showOnDesktop || Application.isMobilePlatform)
                    && !PauseMenu.isPaused && !BitzAds.IsShowing;
        container.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }
}