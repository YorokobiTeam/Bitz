using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


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
            return fileDialog.FileName;
        }
        return null;
    }
}
