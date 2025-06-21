using System;
using System.Collections.Generic;

[Serializable]
public class BeatmapData
{
	public float bpm;
	public float offset;
	public List<Note> notes;
}

[Serializable]
public class Note
{
	public string type;
	public float time;
	public float start;
	public float end;
}
