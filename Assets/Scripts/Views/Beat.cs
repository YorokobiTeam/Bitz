using UnityEngine;

public class BeatPrefab : MonoBehaviour
{
    public BeatData data;
    public void Start()
    {
        if (data == null)
        {
            Destroy(gameObject);
        }

    }

    // Update is called 100 times per second
    public void FixedUpdate()
    {
        var pos = Camera.main.WorldToScreenPoint(transform.position);
        if (pos.x < (Screen.safeArea.xMin))
        {
            //Destroy(gameObject);
        }
    }
}