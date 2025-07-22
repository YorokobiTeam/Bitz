using UnityEngine;
using System.Collections.Generic;
public class MapDataLoader : MonoBehaviour
{
    public static List<BitzSongData> listSongData;
    public static BitzSongData PopulateSOs(
        BeatmapData mapData,
        AudioClip songData,
        Sprite? backgroundImage,
        Sprite? albumCover
        )
    {
        if (mapData == null || songData == null) return null;
        BitzSongData newData = ScriptableObject.CreateInstance<BitzSongData>();
        newData.beatmaps = mapData;
        newData.musicFile = songData;
        newData.backgroundImage = backgroundImage;
        newData.albumCoverImage = albumCover;
        return newData;
    }
    public static void AddToSOList(BitzSongData xongData)
    {
        listSongData.Add(xongData);
    }
    public void Start()
    {
        listSongData = new List<BitzSongData>();
    }
}
