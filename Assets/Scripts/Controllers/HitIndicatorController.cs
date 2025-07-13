using UnityEngine;

public class HitIndicatorController : MonoBehaviour
{
    public GameData gameData;
    public BeatController beatController;
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    public Sprite missSprite;
    public Sprite niceSprite;
    public Sprite coolSprite;
    public Sprite epicSprite;

    public void Start()
    {
        beatController.OnNotifyBeatHit += BeatController_OnNotifyBeatHit;
    }

    private void BeatController_OnNotifyBeatHit(HitResult hitResult, float distanceDiff)
    {
        Sprite sprite = hitResult switch
        {
            HitResult.Missed => missSprite,
            HitResult.Cool => coolSprite,
            HitResult.Nice => niceSprite,
            HitResult.Epic => epicSprite,
            _ => null
        };
        if (sprite == null) return;
        this.spriteRenderer.sprite = sprite;
        animator.Play("BaseState");
        animator.Play("HitEffect");

    }

}
