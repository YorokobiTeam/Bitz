using System;

[Serializable]
public class BeatData
{
    public BeatType type;

    // Currently to simplify things, we're only doing a single beat direction.
    public BeatDirection beatDirection;
    public float speed;
    public float start;
    public float end = -1;
}
