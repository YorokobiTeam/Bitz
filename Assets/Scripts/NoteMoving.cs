using UnityEngine;

public class NoteMoving : MonoBehaviour
{
	public float speed;
	private float holdDuration = 0f;
	private bool isHoldNote = false;

	void Start()
	{
		speed = speed / 60f;
	}

	void Update()
	{
		transform.position -= new Vector3(speed * Time.deltaTime, 0f, 0f);
	}

	public void SetHoldDuration(float duration)
	{
		isHoldNote = true;
		holdDuration = duration;
		float distance = duration * speed; // Distance note travels during hold
		transform.localScale = new Vector3(distance, transform.localScale.y, transform.localScale.z);
		// Optionally, add a LineRenderer or sprite adjustment here
	}

	public bool IsHoldNote()
	{
		return isHoldNote;
	}

	public float GetHoldDuration()
	{
		return holdDuration;
	}
}