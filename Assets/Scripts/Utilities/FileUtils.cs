
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;


static class FileUtils
{
    public static string? OpenFilePicker()
    {
        var fileDialog = new System.Windows.Forms.OpenFileDialog()
        {
            Filter = "Bitz Map Files (*.bitzmap)|*.bitzmap",
            Title = "Open the beatmap file."
        };

        if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            Debug.Log(fileDialog.FileName);
            return fileDialog.FileName;

        }
        return null;
    }
    //public static void UnzipAllMaps(string path)
    //{

    //    if (!Directory.Exists(path))
    //    {
    //        Debug.LogError($"Directory {path} does not exist.");
    //        return;
    //    }
    //    DirectoryInfo dirInfo = new DirectoryInfo(path);
    //    var fileInfo = dirInfo.GetFiles();
    //    foreach (var file in fileInfo)
    //    {
    //        if (file.Extension != ".bitzmap")
    //        {
    //            Debug.LogWarning($"File {file.Name} is not a .bitzmap file, skipping.");
    //            continue;
    //        }
    //        try
    //        {
    //            AddNewBitzMap(file.FullName, null);
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.LogError($"Failed to load map {file.Name}: {ex.Message}");

    //        }
    //    }
    //}

    public async static Task<List<MapMetadataObject>?> LoadAllMaps(string path, AudioImporter importer)
    {

        if (!Directory.Exists(path))
        {
            Debug.Log($"Dir not found {path}");
            return new List<MapMetadataObject>();
        }
        var mapDir = Directory.GetDirectories(path);
        List<MapMetadataObject> availableMaps = new();
        foreach (var dir in mapDir)
        {
            var folderCheckResult = await ScanMapDirectory(dir, importer);
            if (!folderCheckResult.isValid) continue;

            availableMaps.Add(BeatUtils.CreateMapMetadata(folderCheckResult.mapData, folderCheckResult.backgroundImage, folderCheckResult.albumCoverImage));


        }
        return availableMaps;

    }
    public static Task<AudioClip> LoadAudio(string path, AudioImporter importer)
    {
        var promise = new TaskCompletionSource<AudioClip>();
        importer.Loaded += (clip) =>
        {
            if (clip != null)
            {
                promise.TrySetResult(clip);
            }
        };
        importer.Import(path);
        return promise.Task;
    }

    public static Texture2D LoadTexture2D(string path)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new(1, 1);
            if (ImageConversion.LoadImage(texture, bytes))
            {
                return texture;
            }
            throw new Exception("Texture can't be loaded");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading sprite from {path}: {ex.Message}");
            return null;
        }
    }
    public static Sprite LoadSprite(string path)
    {
        try
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new(1, 1);
            if (ImageConversion.LoadImage(texture, fileData))
            {
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            }
            Debug.LogError($"Failed to load sprite from {path}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading sprite from {path}: {ex.Message}");
            return null;
        }
    }
    public static void AddNewBitzmap(AudioImporter importer, IProgress<string>? progressCallback)
    {
        Debug.Log(importer);
        AddNewBitzMap(OpenFilePicker(), importer, progressCallback);
    }

    /// <summary>
    /// This function opens the bitzmap, reads and verify the content, then overrides the folder with bitzmap's data.
    /// </summary>
    /// <param name="path">Path of the .bitzmap file</param>
    /// <param name="progressCallback">Progress callback messages</param>
    public static async void AddNewBitzMap(string path, AudioImporter importer, IProgress<string>? progressCallback)
    {
        var tempFolder = Path.Join(Constants.APPLICATION_DATA, "temp");
        try
        {
            if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

            Debug.Log("Extracting Bitzmap...");
            System.IO.Compression.ZipFile.ExtractToDirectory(path, tempFolder);

            var mapFolder = Path.Join(Constants.APPLICATION_DATA, BeatUtils.ReadBeatmap(Path.Join(tempFolder, Constants.FILENAME_MAP)).identifier);

            if (Directory.Exists(mapFolder))
            {
                Debug.Log("Deleting previous folder...");
                Directory.Delete(mapFolder, true);
            }
            Directory.CreateDirectory(mapFolder);

            Debug.Log("Moving files...");
            foreach (string fileName in Directory.GetFiles(tempFolder))
            {
                File.Move(fileName, Path.Join(mapFolder, Path.GetFileName(fileName)));
            }
            Debug.Log("Verifying content...");

            var scanResult = await ScanMapDirectory(mapFolder, importer);

            if (!scanResult.isValid)
            {
                Directory.Delete(mapFolder, true);
                throw new Exception("Invalid beatmap file given.");
            }

            Debug.Log("Done!");
            return;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            throw;
        }
        finally
        {
            Directory.Delete(tempFolder, true);
        }


    }



    public static async Task<DirectoryScanResult> ScanMapDirectory(string directory, AudioImporter importer)
    {
        var dirFiles = Directory.GetFiles(directory);
        Debug.Log(importer);
        DirectoryScanResult directoryScanResult = new()
        {
            isValid = true,
            backgroundImage = null,
            albumCoverImage = null
        };
        if (!dirFiles.Contains(Path.Join(directory, Constants.FILENAME_MAP)))
        {
            directoryScanResult.isValid = false;
            return directoryScanResult;
        }
        try
        {
            directoryScanResult.mapData = BeatUtils.ReadBeatmap(Path.Join(directory, Constants.FILENAME_MAP)) ?? throw new Exception("Invalid beatmap file");
            //Debug.Log(directoryScanResult.mapData.musicFileName);
            //directoryScanResult.music = await LoadAudio(Path.Join(directory, directoryScanResult.mapData.musicFileName), importer);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            directoryScanResult.isValid = false;
        }

        if (dirFiles.Contains(Path.Join(directory, directoryScanResult.mapData.backgroundFileName))) directoryScanResult.backgroundImage = LoadTexture2D(Path.Join(directory, directoryScanResult.mapData.backgroundFileName));
        if (dirFiles.Contains(Path.Join(directory, directoryScanResult.mapData.albumCoverFileName))) directoryScanResult.albumCoverImage = LoadTexture2D(Path.Join(directory, directoryScanResult.mapData.albumCoverFileName));

        return directoryScanResult;
    }
}


public struct DirectoryScanResult
{
    public bool isValid;
    public BeatmapData mapData;
    public Texture2D? albumCoverImage;
    public Texture2D? backgroundImage;
}

