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

    /// <summary>
    /// This function opens the bitzmap, reads and verify the content, then overrides the folder with bitzmap's data.
    /// </summary>
    /// <param name="path">Path of the .bitzmap file</param>
    /// <param name="progressCallback">Progress callback messages</param>
    public static async void LoadNewBitzmap(string path, IProgress<string>? progressCallback)
    {
        await Task.Run(() =>
        {
            try
            {
                var tempFolder = Path.Join(Constants.APPLICATION_DATA, "temp");
                if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

                progressCallback?.Report("Extracting Bitzmap...");
                System.IO.Compression.ZipFile.ExtractToDirectory(path, tempFolder);

                progressCallback?.Report("Verifying content...");
                var scanResult = VerifyBitzMapDirectoryStructure(tempFolder);
              
                if (!scanResult.isValid) throw new Exception();

                var mapFolder = Path.Join(Constants.APPLICATION_DATA, scanResult.mapData.identifier.ToString());
                if (Directory.Exists(mapFolder))
                {
                    progressCallback?.Report("Deleting previous folder...");
                    Directory.Delete(mapFolder, true);
                    Directory.CreateDirectory(mapFolder);
                }
                progressCallback?.Report("Moving files...");
                // Lets just move all the files im too lazy to type it all out...
                foreach (string fileName in Directory.GetFiles(tempFolder))
                {
                    File.Move(Path.Join(tempFolder, fileName), Path.Join(mapFolder, fileName));
                }
                progressCallback?.Report("Done!");
                return;
            }
            catch (SecurityException)
            {
                progressCallback?.Report("Bitz does not have access to specified file");
            }
            catch (FileNotFoundException)
            {
                progressCallback?.Report("File was not found on local drive.");
            }
            catch (IOException)
            {
                progressCallback?.Report("Bitz can't read specified files. Please try again later.");
            }
            catch (Exception)
            {
                progressCallback?.Report("Invalid Bitz Map file selected, please try again!");
            }
        });

    }




    public void LoadPreviousBeatmaps()
    {

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
        if (!dirFiles.Contains(Constants.FILENAME_MUSIC) || !dirFiles.Contains(Constants.FILENAME_MAP))
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
        if (dirFiles.Contains(Constants.FILENAME_BACKGROUND)) directoryScanResult.backgroundImage = true;
        if (dirFiles.Contains(Constants.FILENAME_ALBUM_COVER)) directoryScanResult.albumCoverImage = true;

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
