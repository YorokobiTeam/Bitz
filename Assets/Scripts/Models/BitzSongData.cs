using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BitzSongData", menuName = "Scriptable Objects/BitzSongData")]
public class BitzSongData : ScriptableObject
{
    public BeatmapData beatmaps;
    public AudioClip musicFile;
    public Texture2D backgroundImage;
    public Texture2D albumCoverImage;
}
