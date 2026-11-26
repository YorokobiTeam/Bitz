#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Run explicitly in batch mode: -executeMethod BitzHealthSmokeTest.Run (without -quit).
[InitializeOnLoad]
public static class BitzHealthSmokeTest
{
    private const string RunningKey = "Bitz.HealthSmoke.Running";
    static BitzHealthSmokeTest() { EditorApplication.playModeStateChanged += OnStateChanged; }
    public static void Run()
    {
        SessionState.SetBool(RunningKey, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
    }
    private static void OnStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(RunningKey, false)) return;
        try
        {
            var go = new GameObject("Health smoke test");
            var health = go.AddComponent<BitzHealth>();
            var deaths = 0; var revives = 0;
            health.Died += () => deaths++; health.Revived += () => revives++;
            Check(health.Current == 5 && !health.IsDead, "initial HP");
            health.RegisterHit(HitResult.Epic); Check(health.Current == 5, "successful hit");
            for (var i = 0; i < 4; i++) health.RegisterHit(HitResult.Missed);
            Check(health.Current == 1 && deaths == 0, "damage per miss");
            health.RegisterHit(HitResult.Missed); health.RegisterHit(HitResult.Missed);
            Check(health.IsDead && health.Current == 0 && deaths == 1, "death exactly once");
            health.RequestRevive(); Check(health.IsDead, "no revive provider");
            var provider = new FakeProvider(); health.SetReviveProvider(provider);
            provider.Result = BitzReviveResult.Pending; health.RequestRevive(); Check(health.IsDead && revives == 0, "pending verification");
            provider.Result = BitzReviveResult.Cancelled; health.RequestRevive(); Check(health.IsDead, "cancelled ad");
            provider.Result = BitzReviveResult.Confirmed; health.RequestRevive();
            Check(!health.IsDead && health.Current == 5 && revives == 1, "server-confirmed revive");
            Debug.Log("HP damage, death and verified revive smoke test passed.");
            Finish(0);
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    private static void Check(bool ok, string name) { if (!ok) throw new Exception("HP smoke test failed: " + name); }
    private static void Finish(int code) { SessionState.SetBool(RunningKey, false); EditorApplication.Exit(code); }
    private sealed class FakeProvider : IBitzReviveProvider
    {
        public BitzReviveResult Result;
        public void RequestRevive(Action<BitzReviveResult> completed) => completed(Result);
    }
}
#endif
