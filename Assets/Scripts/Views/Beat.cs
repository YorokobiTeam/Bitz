using UnityEngine;

public class BeatPrefab : MonoBehaviour
{
    public BeatData data = null;
    internal Rigidbody2D rb;
    public void Awake()
    {
        this.rb = GetComponent<Rigidbody2D>();
    }
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
                BeatDirection.Up => 90f,
                BeatDirection.Down => -90f,
                BeatDirection.Left => 180f,
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

    public void AnimateOut(Vector3 from, Vector3 to)
    {
        LeanTween.scale(this.gameObject, new(0.3f, 0.3f), 0.6f).setEaseOutQuint();
        LeanTween.move(
            this.gameObject,
            new Vector3[] { from, new(1, 5, 0), new(1, 5, 0), to },
            0.6f
        ).setEaseOutQuint().setOnComplete(() =>
        {
            Destroy(this.gameObject);
        });
        //this.GetComponent<SpriteRenderer>().sprite = null;

    }
}