using System;
using System.Collections.Generic;


[Serializable]
public struct PlayHistory
{
    public List<Attempt> attempts;
}

[Serializable]
public struct Attempt
{
    public string mapId;
    public int score;
    public DateTime time;
}