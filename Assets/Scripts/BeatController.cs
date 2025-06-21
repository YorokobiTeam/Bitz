using UnityEngine;

public class BeatController : MonoBehaviour
{

    [SerializeField] bool canBePressed;
    [SerializeField] KeyCode keyCode;

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
