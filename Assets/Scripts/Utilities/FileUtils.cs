
using System.Windows.Forms;
using UnityEngine;


static class FileUtils
{
    public static string? OpenFilePicker()
    {
        var fileDialog = new OpenFileDialog()
        {
            Filter = "Bitz Map Files (*.bitzmap)|*.bitzmap",
            Title = "Open the beatmap file."
        };

        if (fileDialog.ShowDialog() == DialogResult.OK)
        {
            Debug.Log(fileDialog.FileName);
            return fileDialog.FileName;

        }
        return null;
    }
}
