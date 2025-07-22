using JetBrains.Annotations;
using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Networking;

public class FileController : MonoBehaviour
{
    public GameData gameData;
    private OpenFileDialog fileDialog;


    public static void LoadNewBitzmap(IProgress<string>? progressCallback)
    {
        Debug.Log(progressCallback);
        LoadNewBitzmap(FileUtils.OpenFilePicker(), progressCallback);
    }

    /// <summary>
    /// This function opens the bitzmap, reads and verify the content, then overrides the folder with bitzmap's data.
    /// </summary>
    /// <param name="path">Path of the .bitzmap file</param>
    /// <param name="progressCallback">Progress callback messages</param>
    public static void LoadNewBitzmap(string path, IProgress<string>? progressCallback)
    {
        var tempFolder = Path.Join(Constants.APPLICATION_DATA, "temp");
        try
        {
            if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

            Debug.Log("Extracting Bitzmap...");
            System.IO.Compression.ZipFile.ExtractToDirectory(path, tempFolder);

            Debug.Log("Verifying content...");
            var scanResult = VerifyBitzMapDirectoryStructure(tempFolder);

            if (!scanResult.isValid) throw new Exception();

            var mapFolder = Path.Join(Constants.APPLICATION_DATA, scanResult.mapData.identifier.ToString());
            if (Directory.Exists(mapFolder))
            {
                Debug.Log("Deleting previous folder...");
                Directory.Delete(mapFolder, true);
            }
            Directory.CreateDirectory(mapFolder);
            Debug.Log("Moving files...");
            // Lets just move all the files im too lazy to type it all out...
            foreach (string fileName in Directory.GetFiles(tempFolder))
            {
                File.Move(fileName, Path.Join(mapFolder, Path.GetFileName(fileName)));
            }
            Debug.Log("Done!");
            return;
        }
        catch
        {
            throw;
        }
        finally
        {
            Directory.Delete(tempFolder, true);
        }


    }



    public static DirectoryScanResult VerifyBitzMapDirectoryStructure(string directory)
    {
        var dirFiles = Directory.GetFiles(directory);
        DirectoryScanResult directoryScanResult = new()
        {
            isValid = true,
            backgroundImage = false,
            albumCoverImage = false
        };
        if (!dirFiles.Contains(Path.Join(directory, Constants.FILENAME_MUSIC)) || !dirFiles.Contains(Path.Join(directory, Constants.FILENAME_MAP)))
        {
            directoryScanResult.isValid = false;
            return directoryScanResult;
        }
        try
        {
            directoryScanResult.mapData = BeatUtils.ReadBeatmap(Path.Join(directory, Constants.FILENAME_MAP)) ?? throw new Exception("Invalid beatmap file");
        }
        catch
        {
            directoryScanResult.isValid = false;
        }
        if (dirFiles.Contains(Path.Join(directory, Constants.FILENAME_BACKGROUND))) directoryScanResult.backgroundImage = true;
        if (dirFiles.Contains(Path.Join(directory, Constants.FILENAME_ALBUM_COVER))) directoryScanResult.albumCoverImage = true;

        return directoryScanResult;
    }


    public void Start()
    {
        
        Debug.Log("FileController started, setting up game files");
        
        var mapFolder = Path.Join(Constants.APPLICATION_DATA, "maps");
        if(!Directory.Exists(mapFolder))
        {
            throw new Exception("Error while trying to read maps folder:" + mapFolder);
        }
        UnzipAllMaps(mapFolder);
        LoadALlMaps(mapFolder);


    }
    public static void UnzipAllMaps(string path)
    {

        if (!Directory.Exists(path))
        {
            Debug.LogError($"Directory {path} does not exist.");
            return;
        }
        DirectoryInfo dirInfo = new DirectoryInfo(path);
        var fileInfo = dirInfo.GetFiles();
        foreach (var file in fileInfo)
        {
            if (file.Extension != ".bitzdata")
            {
                Debug.LogWarning($"File {file.Name} is not a .bitzdata file, skipping.");
                continue;
            }
            try
            {
                LoadNewBitzmap(file.FullName, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load bitzdata {file.Name}: {ex.Message}");

            }
        }
    }
    public static async void LoadALlMaps(string path)
    {
        
        if (!Directory.Exists(path))
        {
            Debug.Log($"Path not found {path}");
            return;
        }
        DirectoryInfo workingDir = new DirectoryInfo(path);
        var mapDir = workingDir.GetDirectories();
        foreach (var dir in mapDir)
        {
            var mapFiles = dir.GetFiles();
            foreach(var file in mapFiles)
            {
                try
                {
                    BeatmapData tempMapdata = null;
                    AudioClip tempSong = null;
                    Sprite tempBackground = null;
                    Sprite tempAlbumCover = null;
                    if (file.Extension == ".json")
                    {
                        Debug.Log("Found .json, loading");
                        tempMapdata = JsonUtility.FromJson<BeatmapData>(File.ReadAllText(file.FullName));
                    }
                    if (file.Extension == ".mp3")
                    {
                        Debug.Log("Found .mp3, loading");
                        tempSong = await LoadAudio(file.FullName);
                    }
                    if(file.Extension == ".png" && (file.Name.ToLower().Contains("bg") || file.Name.ToLower().Contains("background")))
                    {
                        Debug.Log("Found .png background, loading");
                        tempBackground = LoadSprite(file.FullName);
                    }
                    if( file.Extension == ".png" && (file.Name.ToLower().Contains("album") || file.Name.ToLower().Contains("cover")))
                    {
                        Debug.Log("Found .png cover, loading");
                        tempAlbumCover = LoadSprite(file.FullName);
                    }

                    if(tempMapdata == null || tempSong == null)
                    {
                        Debug.LogError($"Invalid map data:{file.Name}, skipping");
                        continue;
                    }
                    MapDataLoader.AddToSOList(MapDataLoader.PopulateSOs(tempMapdata, tempSong, tempBackground, tempAlbumCover));
                }
                catch (Exception ex)
                {
                    Debug.Log($"error while loading files {ex.Message}");
                }

            }
        }
        
    }
    static async Task<AudioClip> LoadAudio(string path)
    {
        string uri = "file://" + path;
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                return DownloadHandlerAudioClip.GetContent(request);
            }
            else
            {
                Debug.LogError($"Failed to load audio from {path}: {request.error}");
                return null;
            }
        }
    }
    static Sprite LoadSprite(string path)
    {
        try
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(fileData))
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
    
}

public struct DirectoryScanResult
{
    public bool isValid;
    public bool backgroundImage;
    public bool albumCoverImage;
    public BeatmapData mapData;
}

