using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class EndScreenAnimator : MonoBehaviour
{
    public GameData gameData;

    [SerializeField]
    Sprite rankingTextS,
        rankingTextA,
        rankingTextB,
        rankingTextF,
        rankingS,
        rankingA,
        rankingB,
        rankingF;

    [SerializeField]
    Image albumCover,
        resultRanking,
        resultRankingText,
        resultStrip,
        resultBox,
        scoreBox,
        statsBox,
        rankingBg,
        background;

    [SerializeField]
    TMP_Text score,
        countEpic,
        countCool,
        countNice,
        countMissed,
        statusText,
        songName,
        songAuthor,
        mapAuthor;

    [SerializeField]
    GameObject songAuthorBox,
        mapAuthorBox;

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
        RectTransform rankingRect = rankingBg.GetComponent<RectTransform>();
        RectTransform songAuthorRect = songAuthorBox.GetComponent<RectTransform>();
        RectTransform mapAuthorRect = mapAuthorBox.GetComponent<RectTransform>();

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
        songNameRect.localScale = Vector3.zero;
        songAuthorRect.localScale = Vector3.zero;
        mapAuthorRect.localScale = Vector3.zero;

        LeanTween.moveY(statusRect, 0f, 1f).setEase(LeanTweenType.easeOutExpo);

        LeanTween.scale(albumRect, Vector3.one, 1f).setEaseOutBounce().setDelay(0.5f);

        LeanTween.rotateAround(albumRect, Vector3.forward, -360, 3f).setLoopClamp().setDelay(0.5f);
        LeanTween.scale(songNameRect, Vector3.one, 1f).setEaseOutBounce().setDelay(0.5f);
        LeanTween.scale(songAuthorRect, Vector3.one, 1f).setEaseOutBounce().setDelay(0.8f);
        LeanTween.scale(mapAuthorRect, Vector3.one, 1f).setEaseOutBounce().setDelay(0.8f);

        LeanTween.moveLocalY(stripRect.gameObject, 0f, 0.5f).setEaseOutCubic().setDelay(1.5f);

        LeanTween.scale(boxRect, Vector3.one, 0.4f).setEaseOutBack().setDelay(2.0f);
        LeanTween
            .rotateAround(rankingRect, Vector3.forward, -360, 10f)
            .setEaseInOutCubic()
            .setLoopClamp()
            .setDelay(2.5f);

        LeanTween.scale(rankRect, Vector3.one, 0.4f).setEaseOutBack().setDelay(2.3f);
        LeanTween.scale(rankTextRect, Vector3.one, 0.4f).setEaseOutBack().setDelay(2.6f);

        LeanTween.scale(scoreBoxRect, Vector3.one, 0.5f).setEaseOutBack().setDelay(3f);
        LeanTween.scale(statsBoxRect, Vector3.one, 0.5f).setEaseOutCubic().setDelay(3.5f);
    }

    public void Start()
    {
        switch (getResult())
        {
            case GameResult.F:
                this.resultRanking.sprite = this.rankingF;
                this.resultRankingText.sprite = this.rankingTextF;
                statusText.text = "FAILED";
                break;
            case GameResult.A:
                this.resultRanking.sprite = this.rankingA;
                this.resultRankingText.sprite = this.rankingTextA;
                break;
            case GameResult.B:
                this.resultRanking.sprite = this.rankingB;
                this.resultRankingText.sprite = this.rankingTextB;
                break;
            case GameResult.S:
                this.resultRanking.sprite = this.rankingS;
                this.resultRankingText.sprite = this.rankingTextS;
                break;
        }
        this.score.text = UIUtils.GetScoreText(this.gameData.score, 11);
        this.countMissed.text = gameData.missedHitCount.ToString();
        this.countCool.text = gameData.coolHitCount.ToString();
        this.countNice.text = gameData.niceHitCount.ToString();
        this.countEpic.text = gameData.epicHitCount.ToString();

        this.songAuthor.text = gameData.beatmapData.songAuthor;
        this.songName.text = gameData.beatmapData.songTitle;
        this.mapAuthor.text = gameData.beatmapData.mapAuthor;

        this.albumCover.sprite = gameData.albumCoverSprite;
        this.background.sprite = gameData.backgroundSprite;
    }

    private GameResult getResult()
    {
        if (this.gameData.score < gameData.scoreRankB)
            return GameResult.F;
        if (this.gameData.score >= gameData.scoreRankB && this.gameData.score < gameData.scoreRankA)
            return GameResult.B;
        if (this.gameData.score >= gameData.scoreRankA && this.gameData.score < gameData.scoreRankS)
            return GameResult.A;
        return GameResult.S;
    }

    enum GameResult
    {
        S,
        A,
        B,
        F,
    }
}
