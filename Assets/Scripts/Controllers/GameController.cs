using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public GameData gameData;
    public InputAction hitAction;
    public BeatController beatController;
    public UIController uiController;
    public Animator hitComboAnimator;
    public AudioSource hitSfx;
    public TMP_Text scoreText;

    public void Start()
    {
        this.hitComboAnimator.SetInteger("ComboAmount", 0);
        this.hitComboText.text = 0.ToString() + "x";

        hitAction = InputSystem.actions.FindAction("Hit");
        this.hitAction.performed += Hit;
        this.hitAction.performed += (InputAction.CallbackContext callback) => this.hitSfx.Play();
        this.gameData.score = 0;
        this.beatController.OnNotifyBeatHit += ProcessHitResult;
        this.beatController.OnNotifyBeatHit += UpdateGameDataCount;
        uiController.OnFinishLoadingAssets += Loader_OnFinishLoadingAssets;
        beatController.SpawnBeats();
        this.scoreText.text = UIUtils.GetScoreText(this.gameData.score, 9);
        uiController.OnUpdateGameState += HandleGameStateUpdate;
    }

    private void HandleGameStateUpdate(GameState gameState)
    {
        switch (gameState)
        {
            case GameState.Paused:
                this.beatController.PauseBeats();
                break;
            case GameState.Ingame:
                this.beatController.StartBeats();
                break;
        }
    }

    private void UpdateGameDataCount(HitResult hitResult, float distanceDiff)
    {
        switch (hitResult)
        {

            case HitResult.Missed:
                this.gameData.missedHitCount++;
                break;
            case HitResult.Nice:
                this.gameData.niceHitCount++;
                break;
            case HitResult.Cool:
                this.gameData.coolHitCount++;
                break;
            case HitResult.Epic:
                this.gameData.epicHitCount++;
                break;
        }
    }

    private void Loader_OnFinishLoadingAssets()
    {
        this.beatController.StartBeats();
        this.uiController.StartPlay();
    }

    public void Hit(InputAction.CallbackContext callback)
    {
        this.beatController.TryHit(callback);

    }
    public TMP_Text hitComboText;
    private int comboCount;

    public void ProcessHitResult(HitResult hitResult, float diff)
    {
        var multiplier = hitResult switch
        {
            HitResult.Missed => 0,
            HitResult.Nice => 2,
            HitResult.Cool => 4,
            HitResult.Epic => 6,
            _ => 0
        };

        multiplier += comboCount switch
        {
            < 10 => 0,
            (>= 10 and < 20) => 5,
            (>= 20 and < 30) => 8,
            (>= 30 and < 40) => 10,
            (>= 40) => 12
        };

        if (hitResult == HitResult.Missed) this.comboCount = 0;
        else if (hitResult != HitResult.Ignored)
        {
            this.comboCount = Math.Clamp(++comboCount, 0, 99);
            this.hitComboAnimator.ResetTrigger("TriggerNewHit");
            if (this.comboCount > 1) this.hitComboAnimator.SetTrigger("TriggerNewHit");

        }
        this.hitComboAnimator.SetInteger("ComboAmount", comboCount);
        this.hitComboText.text = this.comboCount.ToString() + "x";

        this.gameData.score += Mathf.FloorToInt(multiplier * diff * 100);
        this.scoreText.text = UIUtils.GetScoreText(this.gameData.score, 9);
    }

    public void Retry()
    {
        SceneManager.LoadScene("BitzPlayer");
    }


}