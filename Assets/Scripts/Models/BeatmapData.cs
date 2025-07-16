using System;
using System.Collections.Generic;

[Serializable]
public class BeatmapData
{
    public string identifier;
    public float speed;
    public float offsetMs;
    public List<BeatData> beats;

    public string songTitle;
    public string songAuthor;

    public string mapAuthor;

}
