using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatController : MonoBehaviour
{

    public BeatPrefab beatPrefab;
    public BeatmapData mapData;
    private readonly Queue<BeatPrefab> activeBeatObjects = new();
    public GameObject hitboxObject;
    public GameData gameData;
    public GameObject beatHitbox;

    internal void SpawnBeats()
    {
        foreach (BeatData beatData in mapData.beats)
        {
            var beatObj = Instantiate(beatPrefab,
                hitboxObject.transform.position + new Vector3(mapData.offsetMs * mapData.speed + (mapData.speed / 1000) * beatData.start, 0)
                , Quaternion.identity
                                );
            var rb = beatObj.rb;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.angularVelocity = 0f;
            rb.linearVelocityX = 0f;

            beatObj.data = beatData;
            activeBeatObjects.Enqueue(beatObj);
        }
        SetHitboxOrientation();
        OnFinishSpawningBeats?.Invoke();
    }


    public event NotifyFinishLoadingBeats OnFinishSpawningBeats;

    private void LoadBeatmapData()
    {
        this.mapData = BeatUtils.ReadBeatmap(gameData.beatMapFileDir);
        this.gameData.beatmapData = mapData;
    }
    public void StartBeats()
    {
        foreach (BeatPrefab beat in this.activeBeatObjects)
        {

            beat.rb.linearVelocityX = -gameData.beatmapData.speed;
        }
    }

    public void SetHitboxOrientation()
    {
        this.beatHitbox.transform.eulerAngles = new Vector3(0, 0, this.activeBeatObjects.Peek().data.beatDirection switch
        {
            BeatDirection.Up => 90f,
            BeatDirection.Down => -90f,
            BeatDirection.Left => 180f,
            BeatDirection.Right => 0f,
            _ => 0
        });
    }

    public void PauseBeats()
    {
        foreach (BeatPrefab beat in this.activeBeatObjects)
        {

            beat.rb.linearVelocityX = 0;
        }
    }

    public void DestroyBeat(BeatPrefab beat, DestructionReason reason)
    {
        if (beat == null) return;
        if (reason is DestructionReason.OutOfBounds) this.NotifyBeatHit(HitResult.Missed, -1f);
        Destroy(beat.gameObject);
    }

    public event NotifyBeatHit OnNotifyBeatHit;

    protected virtual void NotifyBeatHit(HitResult hitResult, float distanceDiff)
    {
        OnNotifyBeatHit?.Invoke(hitResult, distanceDiff);
        SetHitboxOrientation();
    }


    public void TryHit(InputAction.CallbackContext callback)
    {

        var beat = this.activeBeatObjects.Peek();
        var inputDirection = callback.ReadValue<Vector2>();

        var diff = beat.transform.position.x - this.hitboxObject.transform.position.x;
        var dist = Mathf.Abs(diff);
        var hitResult = this.GetHitResult(diff, dist);
        if (hitResult != HitResult.Ignored)
        {
            BeatDirection? beatDirection = null;
            if (inputDirection.x > 0) beatDirection = BeatDirection.Right;
            if (inputDirection.x < 0) beatDirection = BeatDirection.Left;
            if (inputDirection.y > 0) beatDirection = BeatDirection.Up;
            if (inputDirection.y < 0) beatDirection = BeatDirection.Down;
            Debug.Log(inputDirection);
            Debug.Log(beat.data.beatDirection);
            if (beatDirection is null || !(beatDirection == (beat.data.beatDirection)))
            {
                hitResult = HitResult.Missed;
            }
            Destroy(this.activeBeatObjects.Dequeue().gameObject);
        }

        this.NotifyBeatHit(hitResult, diff);

    }

    public HitResult GetHitResult(float diff, float dist)
    {
        if (diff >= 0)
        {
            if (dist > gameData.hitRegisterThreshold.Item2)
                return HitResult.Ignored;
            if (dist > gameData.niceHitRegisterThreshold.Item2)
                return HitResult.Missed;
            if (dist > gameData.coolHitRegisterThreshold.Item2)
                return HitResult.Nice;
            if (dist > gameData.epicHitRegisterThreshold.Item2)
                return HitResult.Cool;
            return HitResult.Epic;
        }
        else
        {
            if (dist > gameData.niceHitRegisterThreshold.Item1)
                return HitResult.Missed;
            if (dist > gameData.coolHitRegisterThreshold.Item1)
                return HitResult.Nice;
            if (dist > gameData.epicHitRegisterThreshold.Item1)
                return HitResult.Cool;
            return HitResult.Epic;
        }
    }


    public void Start()
    {
        LoadBeatmapData();
    }

    public void Update()
    {
        activeBeatObjects.TryPeek(out BeatPrefab frontOfQueue);
        if (frontOfQueue != null)
        {
            var secondInQueue = activeBeatObjects.Skip(1).FirstOrDefault();
            var xHitbox = this.hitboxObject.transform.position.x;
            var xFoq = frontOfQueue.transform.position.x;
            if (secondInQueue != null)
            {
                var xSoq = secondInQueue.transform.position.x;
                if (xFoq - xHitbox < 0 && Mathf.Abs(xFoq - xHitbox) > Mathf.Abs(xSoq - xHitbox))
                {
                    this.MissBeat();
                    return;
                }
            }

            if (xFoq - xHitbox < 0 && (xHitbox - xFoq < this.gameData.hitRegisterThreshold.Item1)) this.MissBeat();

        }
    }

    private void MissBeat()
    {
        if (this.activeBeatObjects.Peek() != null)
        {
            NotifyBeatHit(HitResult.Missed, Mathf.Abs(this.activeBeatObjects.Peek().transform.position.x - this.hitboxObject.transform.position.x));
            this.activeBeatObjects.Dequeue();
        }

    }

}
public delegate void NotifyFinishLoadingBeats();
public delegate void NotifyBeatHit(HitResult hitResult, float distanceDiff);