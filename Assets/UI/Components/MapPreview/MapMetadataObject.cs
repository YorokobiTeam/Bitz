using Unity.Properties;
using UnityEngine;

[CreateAssetMenu(fileName = "MapMetadataObject", menuName = "Scriptable Objects/MapMetadataObject")]
public class MapMetadataObject : ScriptableObject
{

    public BeatmapData mapData;

    public string currentPoints = "To be implemented";

    public Texture2D coverArtwork;
    public Texture2D backgroundArtwork;
    public AudioClip music;



}
