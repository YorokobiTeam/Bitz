using System.Collections;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
	public BeatmapManager beatmapManager; // Gán qua inspector
	public GameObject[] tapNotePrefab;
    public GameObject[] holdNotePrefab;
    public GameObject target;
    public float noteSpeed = 5f;
    private Transform spawnPosition;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	IEnumerator Start()
    {
		yield return new WaitUntil(() => beatmapManager != null && beatmapManager.IsLoaded);
		StartCoroutine("SpawnNotes");
    }

    IEnumerator SpawnNotes()
    {
        BeatmapData beatMap = beatmapManager.GetBeatMap();
        float scrollTime = Mathf.Abs(target.transform.position.x - transform.position.x) / noteSpeed;

        foreach (var note in beatMap.notes)
        {
            float spawnTime = note.type == "tap" ? note.time - scrollTime : note.start - scrollTime;
            var randomNum = Random.Range(0, 3);

            yield return new WaitForSeconds(spawnTime);

            GameObject noteObj;
			if (note.type == "tap")
              noteObj = Instantiate(tapNotePrefab[randomNum], transform.position, Quaternion.identity);
            else
            {
                noteObj = Instantiate(holdNotePrefab[randomNum], transform.position, Quaternion.identity);
				NoteMoving noteMoving = noteObj.GetComponent<NoteMoving>();
				noteMoving.SetHoldDuration(note.end - note.start);
			}
			noteObj.GetComponent<NoteMoving>().speed = noteSpeed;

		}
    }
}
