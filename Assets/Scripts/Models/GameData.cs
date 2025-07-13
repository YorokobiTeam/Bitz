using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "Scriptable Objects/GameData")]
public class GameData : ScriptableObject
{

    public BeatmapData beatmapData;
    public GameState gameState;
    public long score;
    public bool isPaused;
    public (float, float) hitRegisterThreshold = (0.1f, 2.0f);
    public (float, float) niceHitRegisterThreshold = (0.9f, 1.0f);
    public (float, float) coolHitRegisterThreshold = (0.4f, 0.5f);
    public (float, float) epicHitRegisterThreshold = (0.2f, 0.3f);
    public int rankS = 2000;
    public int rankA = 1000;
    public int rankB = 500;

    public string beatMapFileDir;
    public string musicFileDir;
    public string backgroundImageDir;
    public string albumCoverImageDir;
    public Queue<BeatPrefab> beatQueue;
    public int epicHitCount;
    public int coolHitCount;
    public int niceHitCount;
    public int missedHitCount;
}
