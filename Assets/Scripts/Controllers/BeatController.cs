using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BeatController : MonoBehaviour
{

    public BeatPrefab beatPrefab;
    public BeatmapData mapData;
    private readonly Queue<BeatPrefab> activeBeatObjects = new();
    public GameObject hitboxObject;
    public GameData gameData;

    private void SpawnBeats()
    {
        foreach (BeatData beatData in mapData.beats)
        {
            var beatObj = Instantiate(beatPrefab,
                hitboxObject.transform.position + new Vector3(mapData.offsetMs * mapData.speed + (mapData.speed / 100) * beatData.start, 0)
                , Quaternion.identity
                                );
            var rb = beatObj.GetComponent<Rigidbody2D>();
            rb.linearDamping = 0;
            rb.angularDamping = 0;
            rb.angularVelocity = 0;
            rb.linearVelocityX = -mapData.speed;
            beatObj.transform.rotation =
                new Quaternion(0, 0,
                    beatData.beatDirection switch
                    {
                        BeatDirection.Up => 90,
                        BeatDirection.Down => -90,
                        BeatDirection.Left => 180,
                        BeatDirection.Right => 0,
                        _ => 0
                    }, 0);
            beatObj.data = beatData;
            activeBeatObjects.Enqueue(beatObj);
        }
    }


    private void LoadBeatmapData()
    {
        // For now just read the test beatmap (wildly inaccurate but something is more than nothing right?)
        this.mapData = BeatUtils.ReadBeatmap("test.json");
    }

    public void DestroyBeat(BeatPrefab beat, DestructionReason reason)
    {
        if (beat == null) return;
        if (reason is DestructionReason.OutOfBounds) this.NotifyBeatHit(HitResult.Missed, -1f);
    }

    public event NotifyBeatHit OnNotifyBeatHit;

    protected virtual void NotifyBeatHit(HitResult hitResult, float distanceDiff)
    {
        OnNotifyBeatHit?.Invoke(hitResult, distanceDiff);
        Debug.Log(hitResult);
        Debug.Log(distanceDiff);
    }

    public HitResult tryHit()
    {
        return HitResult.Ignored;
    }

    public void Start()
    {
        LoadBeatmapData();
        SpawnBeats();
    }

    public void Update()
    {
        activeBeatObjects.TryPeek(out BeatPrefab frontOfQueue);
        if (frontOfQueue != null)
        {
            var distToHitbox = frontOfQueue.transform.position.x - hitboxObject.transform.position.x;
            if (distToHitbox >= -gameData.hitRegisterThreshold.Item1) return;
            activeBeatObjects.Dequeue();
            NotifyBeatHit(HitResult.Missed, Mathf.Abs(distToHitbox));
        }
    }

}

public delegate void NotifyBeatHit(HitResult hitResult, float distanceDiff);