using System;
using System.Collections.Generic;
using Unity.VisualScripting;


public class BeatmapData
{
    public Guid identifier = Guid.NewGuid();
    public float speed;
    public float offsetMs;
    public List<BeatData> beats;

    public string songTitle;
    public string songAuthor;

    public string mapAuthor;

}
