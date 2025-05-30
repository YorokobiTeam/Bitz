using UnityEngine;

public class NoteMoving : MonoBehaviour
{
    public float speed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        speed = speed / 60;
    }

    // Update is called once per frame
    void Update()
    {
         transform.position -= new Vector3(speed * Time.deltaTime, 0f, 0f);
    }
}
