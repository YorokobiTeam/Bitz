using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public class Utilities
{
    public static string GetExtension(string fileName)
    {
        string[] strings = fileName.Split(".");
        if (strings.Length < 2)
            return "";
        return strings.Last<string>();
    }

    public static string ExtractFileNameFromSupabasePath(string filePath)
    {
        string[] strings = filePath.Split("/");
        return strings.Last<string>();
    }

    public static string GetFileMD5(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        using (var md5 = MD5.Create())
        {
            var hashBytes = md5.ComputeHash(bytes) ?? throw new InvalidDataException();
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2")); // Format as hexadecimal
            }
            return sb.ToString();
        }
    }
}
