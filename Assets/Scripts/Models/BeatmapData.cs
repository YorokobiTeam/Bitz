using System;
using System.Collections.Generic;

[Serializable]
public class BeatmapData
{
    public string identifier;
    public float speed;
    public float offsetMs;
    public string musicFileName;
    public string backgroundFileName;
    public string albumCoverFileName;
    public List<BeatData> beats;

    public string songTitle;
    public string songAuthor;

    public string mapAuthor;

}
