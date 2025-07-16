using Unity.Properties;
using UnityEngine;

[CreateAssetMenu(fileName = "MapMetadataObject", menuName = "Scriptable Objects/MapMetadataObject")]
public class MapMetadataObject : ScriptableObject
{

    public BeatmapData mapData;

    public string currentPoints;

    public Sprite coverArtwork;
    public Sprite backgroundArtwork;
    public AudioClip music;



}
