using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [SerializeField, Range(0f, 0.4f)] float fromLeft = 0.12f; // fraction of the safe-area width

    void Awake() // must run before GameController.Start spawns the beats
    {
        var cam = Camera.main;
        var safe = Screen.safeArea;
        float vx = safe.xMin / Screen.width + fromLeft * (safe.width / Screen.width);
        var p = transform.position;
        p.x = cam.ViewportToWorldPoint(new Vector3(vx, 0.5f, cam.nearClipPlane)).x;
        transform.position = p;
    }
}