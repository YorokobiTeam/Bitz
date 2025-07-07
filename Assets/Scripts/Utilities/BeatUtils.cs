

using System;
using System.IO;
using UnityEngine;

class BeatUtils
{
    private static readonly string tempTestDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Bitz");
    public static BeatmapData ReadBeatmap(string saveName)
    {
        string saveFile = Path.Join(tempTestDir, saveName);
        if (!File.Exists(saveFile))
        {
            return null;
        }
        return JsonUtility.FromJson<BeatmapData>(
            File.ReadAllText(saveFile));
    }
    // Utility to make life easier for now
    public static void SaveBeatmap(BeatmapData bm, string saveName)
    {
        if (!Directory.Exists(tempTestDir)) Directory.CreateDirectory(tempTestDir);

        FileStream f = File.Open(Path.Join(tempTestDir, saveName), FileMode.OpenOrCreate);
        f.Close();
        File.WriteAllText(Path.Join(tempTestDir, saveName), JsonUtility.ToJson(bm));
    }
}
