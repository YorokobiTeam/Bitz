using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Toggles the debug panels. F1 on desktop, a four-finger tap on a phone.</summary>
public sealed class BitzDebugOverlay : MonoBehaviour
{
    public static bool Visible { get; private set; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        var go = new GameObject("Bitz Debug Overlay");
        DontDestroyOnLoad(go);
        go.AddComponent<BitzDebugOverlay>();
    }
#endif

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.f1Key.wasPressedThisFrame) Visible = !Visible;

        var ts = Touchscreen.current;
        if (ts == null) return;
        int down = 0; bool began = false;
        foreach (var t in ts.touches)
        {
            if (t.press.isPressed) down++;
            if (t.press.wasPressedThisFrame) began = true;
        }
        if (down >= 4 && began) Visible = !Visible;
    }
}