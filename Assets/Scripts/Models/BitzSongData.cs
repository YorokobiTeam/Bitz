using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BitzSongData", menuName = "Scriptable Objects/BitzSongData")]
public class BitzSongData : ScriptableObject
{
    public BeatmapData beatmaps;
    public AudioClip musicFile;
    public Sprite backgroundImage;
    public Sprite albumCoverImage;
}
