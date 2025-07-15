

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
}
