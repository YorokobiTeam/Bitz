using Unity.Properties;
using UnityEngine;

[CreateAssetMenu(fileName = "MapMetadataObject", menuName = "Scriptable Objects/MapMetadataObject")]
public class MapMetadataObject : ScriptableObject
{
    [SerializeField, DontCreateProperty]
    public string mapID;

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
