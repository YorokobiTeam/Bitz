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


    public void Start()
    {
        
        Debug.Log("FileController started, setting up game files");
        
        var mapFolder = Path.Join(Constants.APPLICATION_DATA, "maps");
        if(!Directory.Exists(mapFolder))
        {
            throw new Exception("Error while trying to read maps folder:" + mapFolder);
        }
        FileUtils.UnzipAllMaps(mapFolder);
        FileUtils.LoadAllMaps(mapFolder);


    }
    
    
}

public struct DirectoryScanResult
{
    public bool isValid;
    public bool backgroundImage;
    public bool albumCoverImage;
    public BeatmapData mapData;
}

