

using System.Text;

public class UIUtils
{
    public static string GetScoreText(long score, int totalNumberCount)
    {
        StringBuilder sb = new StringBuilder(score.ToString(), totalNumberCount);
        while (totalNumberCount - sb.Length > 0)
        {
            sb.Insert(0, "0");
        }
        return sb.ToString();
    }
}
