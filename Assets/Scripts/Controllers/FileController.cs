using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;
using UnityEngine;

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

    }
}

public struct DirectoryScanResult
{
    public bool isValid;
    public bool backgroundImage;
    public bool albumCoverImage;
    public BeatmapData mapData;
}
