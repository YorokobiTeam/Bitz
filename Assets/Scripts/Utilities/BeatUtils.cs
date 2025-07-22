

using System;
using System.IO;
using UnityEngine;

static class BeatUtils
{
    public static BeatmapData? ReadBeatmap(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        return JsonUtility.FromJson<BeatmapData>(
            File.ReadAllText(path));
    }
    // Utility to make life easier for now
    public static void SaveBeatmap(BeatmapData bm, string saveName)
    {
        if (!Directory.Exists(Constants.APPLICATION_DATA)) Directory.CreateDirectory(Constants.APPLICATION_DATA);

        FileStream f = File.Open(Path.Join(Constants.APPLICATION_DATA, saveName), FileMode.OpenOrCreate);
        f.Close();
        File.WriteAllText(Path.Join(Constants.APPLICATION_DATA, saveName), JsonUtility.ToJson(bm));
    }

    public static int GetHighestScoreForID(string id)
    {
        string historyFile = Path.Join(Constants.APPLICATION_DATA, Constants.HISTORY_FILE);
        if (!Directory.Exists(Constants.APPLICATION_DATA)) Directory.CreateDirectory(Constants.APPLICATION_DATA);
        if (!File.Exists(historyFile)) File.CreateText(historyFile).Write(JsonUtility.ToJson(new PlayHistory()));
        var history = JsonUtility.FromJson<PlayHistory>(historyFile);
        history.attempts.Sort((x, y) =>
        {
            return x.score - y.score;
        });
        var attempt = history.attempts.Find((attempt) => attempt.mapId == id);

        return attempt.score;
    }

    public static MapMetadataObject CreateMapMetadata(
    BeatmapData mapData,
    Texture2D? background,
    Texture2D? albumCover
    )
    {
        if (mapData == null) return null;
        MapMetadataObject newData = ScriptableObject.CreateInstance<MapMetadataObject>();
        newData.mapData = mapData;
        newData.backgroundArtwork = background;
        newData.coverArtwork = albumCover;
        newData.currentPoints = "0";
        return newData;
    }
}
