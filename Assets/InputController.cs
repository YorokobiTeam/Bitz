using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class InputController : MonoBehaviour
{
    [SerializeField] GameController gameController;
    [SerializeField, Range(0.2f, 0.7f)] float padAreaHeight = 0.5f; // bottom fraction of screen

    public event System.Action<int> LanePressed;   // lane index 0..3
    public float PadAreaHeight => padAreaHeight;

    // left-to-right pad order
    static readonly BeatDirection[] Lanes =
        { BeatDirection.Left, BeatDirection.Down, BeatDirection.Up, BeatDirection.Right };

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
        TouchSimulation.Enable(); // mouse acts as touch in the editor
#endif
        Touch.onFingerDown += OnFingerDown;
    }

    void OnDisable()
    {
        Touch.onFingerDown -= OnFingerDown;
#if UNITY_EDITOR
        TouchSimulation.Disable();
#endif
        EnhancedTouchSupport.Disable();
    }

    void OnFingerDown(Finger finger)
    {
        var p = finger.screenPosition;
        if (p.y > Screen.height * padAreaHeight) return;
        int lane = Mathf.Clamp((int)(p.x / Screen.width * 4f), 0, 3);
        LanePressed?.Invoke(lane);
        gameController.HitDirection(Lanes[lane]);
    }
}