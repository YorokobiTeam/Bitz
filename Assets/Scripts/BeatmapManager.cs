using System.IO;
using UnityEngine;

public class BeatmapManager : MonoBehaviour
{
	public string fileName = "beatmap.json";
	private BeatmapData beatMap;

	public bool IsLoaded => beatMap != null;

	void Awake()
	{
		LoadBeatmap();
	}

	void LoadBeatmap()
	{
		string path = Path.Combine(Application.streamingAssetsPath, fileName);

		if (File.Exists(path))
		{
			try
			{
				string jsonText = File.ReadAllText(path);
				Debug.Log($"JSON Content: {jsonText}"); // Log raw JSON for debugging

				beatMap = JsonUtility.FromJson<BeatmapData>(jsonText);

				if (beatMap == null)
				{
					Debug.LogError("Deserialization failed: beatMap is null.");
					return;
				}

				if (beatMap.notes == null || beatMap.notes.Count == 0)
				{
					Debug.LogError($"Notes list is null or empty. BPM: {beatMap.bpm}, Offset: {beatMap.offset}");
					return;
				}

				Debug.Log($"BPM: {beatMap.bpm}, Offset: {beatMap.offset}, Notes Count: {beatMap.notes.Count}");
				foreach (Note note in beatMap.notes)
				{
					if (note.type == "tap")
					{
						Debug.Log($"Tap Note at time: {note.time}");
					}
					else if (note.type == "hold")
					{
						Debug.Log($"Hold Note from {note.start} to {note.end}");
					}
					else
					{
						Debug.LogWarning($"Unknown note type: {note.type}");
					}
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError($"Error deserializing beatmap: {e.Message}");
			}
		}
		else
		{
			Debug.LogError("Beatmap file not found: " + path);
		}
	}

	public BeatmapData GetBeatMap()
	{
		return beatMap;
	}
}