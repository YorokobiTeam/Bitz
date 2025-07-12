using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class EndScreenAnimator : MonoBehaviour
{
    [SerializeField]
    Sprite rankingTextS, rankingTextA, rankingTextB, rankingTextF, rankingS, rankingA, rankingB, rankingF;


    [SerializeField]
    Image albumCover, resultRanking, resultRankingText, resultStrip, resultBox, scoreBox, statsBox;

    [SerializeField]
    TMP_Text score, countEpic, countCool, countNice, countMissed, statusText, songName, songAuthor, mapAuthor;



    public void OnEnable()
    {
        // Reset states
        RectTransform statusRect = statusText.GetComponent<RectTransform>();
        RectTransform albumRect = albumCover.GetComponent<RectTransform>();
        RectTransform songNameRect = songName.GetComponent<RectTransform>();
        RectTransform stripRect = resultStrip.GetComponent<RectTransform>();
        RectTransform boxRect = resultBox.GetComponent<RectTransform>();
        RectTransform rankRect = resultRanking.GetComponent<RectTransform>();
        RectTransform rankTextRect = resultRankingText.GetComponent<RectTransform>();
        RectTransform scoreBoxRect = scoreBox.GetComponent<RectTransform>();
        RectTransform statsBoxRect = statsBox.GetComponent<RectTransform>();


        statusRect.anchoredPosition = new Vector2(statusRect.anchoredPosition.x, 400f);
        albumRect.localScale = Vector3.zero;
        albumRect.localRotation = Quaternion.Euler(0, 0, -5f);
        songName.gameObject.LeanAlpha(0, 0);
        stripRect.LeanSetPosY(-1280f);
        boxRect.localScale = Vector3.zero;
        rankRect.localScale = Vector3.zero;
        rankTextRect.localScale = Vector3.zero;
        scoreBoxRect.localScale = Vector3.zero;
        statsBoxRect.localScale = Vector3.zero;
        LeanTween.moveY(statusRect, 0f, 0.5f).setEase(LeanTweenType.easeOutExpo).setDelay(0f);

        LeanTween.scale(albumRect, Vector3.one, 1f).setEaseOutBounce().setDelay(0.5f);
        LeanTween.rotate(albumRect, 2.44f, 1f).setEaseOutBounce().setDelay(0.5f);

        songName.gameObject.LeanAlpha(1f, 1f).setDelay(1.5f);

        LeanTween.moveLocalY(stripRect.gameObject, 0f, 0.5f).setEaseOutCubic().setDelay(1.5f);

        LeanTween.scale(boxRect, Vector3.one, 0.4f).setEaseOutBack().setDelay(2.0f);
        LeanTween.scale(rankRect, Vector3.one, 0.4f).setEaseOutBack().setDelay(2.3f);
        LeanTween.scale(rankTextRect, Vector3.one, 0.4f).setEaseOutBack().setDelay(2.6f);

        LeanTween.scale(scoreBoxRect, Vector3.one, 0.5f).setEaseOutBack().setDelay(3f);
        LeanTween.scale(statsBoxRect, Vector3.one, 0.5f).setEaseOutCubic().setDelay(3.5f);
    }

}
