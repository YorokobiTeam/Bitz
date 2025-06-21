using Unity.Properties;
using UnityEngine;

[CreateAssetMenu(fileName = "MapMetadataObject", menuName = "Scriptable Objects/MapMetadataObject")]
public class MapMetadataObject : ScriptableObject
{
    public string mapID { get; private set; }

    public string mapTitle;

    public string musicAuthor;

    public string mapAuthor;

    public string currentPoints;

    public Sprite coverArtwork;

    public bool isOffline = true;

    MapMetadataObject()
    {
        if (isOffline)
        {

        }
    }

}
