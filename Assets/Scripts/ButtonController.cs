using UnityEngine;

public class ButtonController : MonoBehaviour
{
    private SpriteRenderer sr;
    [SerializeField] Sprite normal;
    [SerializeField] Sprite pressed;
    [SerializeField] KeyCode keyCode;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(keyCode))
            sr.sprite = pressed;
        if (Input.GetKeyUp(keyCode))
            sr.sprite = normal;
    }
}
