using UnityEngine;
using UnityEngine.InputSystem;

public class GameController : MonoBehaviour
{
    public GameData gamedata;
    public InputAction hitAction;
    public BeatController beatController;
    public UIController loader;

    public void Start()
    {
        hitAction = InputSystem.actions.FindAction("Hit");
        this.hitAction.performed += Hit;
        this.gamedata.score = 0;
        this.beatController.OnNotifyBeatHit += ProcessHitResult;
        loader.OnFinishLoadingAssets += Loader_OnFinishLoadingAssets;
        beatController.OnFinishLoadingBeats += BeatController_OnFinishLoadingBeats;
    }

    private void BeatController_OnFinishLoadingBeats()
    {

    }

    private void Loader_OnFinishLoadingAssets()
    {
        loader.musicPlayer.Play();

    }

    public void Hit(InputAction.CallbackContext callback)
    {
        this.beatController.TryHit(callback);
    }

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
        this.gamedata.score += Mathf.FloorToInt(multiplier * diff * 100);
    }


}