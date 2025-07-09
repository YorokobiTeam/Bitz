using UnityEngine;

public class BeatPrefab : MonoBehaviour
{
    public BeatData data = null;
    public void Start()
    {
        if (data == null)
        {
            Destroy(gameObject);
        }
        else
        {
            this.transform.eulerAngles = new Vector3(0, 0, this.data.beatDirection switch
            {
                BeatDirection.Up => 180f,
                BeatDirection.Down => -90f,
                BeatDirection.Left => 90f,
                BeatDirection.Right => 0f,
                _ => 0
            });
        }
    }

    public void FixedUpdate()
    {
        var pos = Camera.main.WorldToScreenPoint(transform.position);
        if (pos.x < (Screen.safeArea.xMin) - 5)
        {
            this.enabled = false;
        }
    }
}