using UnityEngine;

public class BeatController : MonoBehaviour
{

    [SerializeField] bool canBePressed;
    [SerializeField] KeyCode keyCode;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (canBePressed)
        {
            if (Input.GetKeyDown(keyCode))
                Destroy(gameObject);
        }
    }

	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Button"))
            canBePressed = true;
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (other.CompareTag("Button"))
			canBePressed = false;
	}
}
