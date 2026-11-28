#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Run explicitly in batch mode: -executeMethod BitzLocalServerSmokeTest.Run (without -quit).
[InitializeOnLoad]
public static class BitzLocalServerSmokeTest
{
    private const string RunningKey = "Bitz.LocalServerSmoke.Running";

    static BitzLocalServerSmokeTest() { EditorApplication.playModeStateChanged += OnPlayModeChanged; }

    public static void Run()
    {
        SessionState.SetBool(RunningKey, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(RunningKey, false)) return;
        var go = new GameObject("Local server smoke test");
        go.AddComponent<Runner>().Begin();
    }

    private sealed class Runner : MonoBehaviour
    {
        public void Begin() => StartCoroutine(Check());

        private IEnumerator Check()
        {
            var client = gameObject.AddComponent<BitzLocalServerClient>();
            BitzLocalServerClient.Result result = null;
            client.SignIn("unity_" + Guid.NewGuid().ToString("N").Substring(0, 12), r => result = r);
            yield return new WaitUntil(() => result != null);
            if (!result.success || result.lives != 3) { Finish(false, "Sign-in or initial lives failed: " + result.code); yield break; }
            var map = Guid.NewGuid().ToString(); var request = Guid.NewGuid().ToString();
            result = null; client.StartSong(map, request, r => result = r);
            yield return new WaitUntil(() => result != null);
            if (!result.success || result.lives != 2) { Finish(false, "Start failed: " + result.code); yield break; }
            result = null; client.StartSong(map, request, r => result = r);
            yield return new WaitUntil(() => result != null);
            if (!result.success || result.lives != 2) { Finish(false, "Start retry failed: " + result.code); yield break; }
            for (var i = 0; i < 2; i++)
            {
                result = null; client.StartSong(map, Guid.NewGuid().ToString(), r => result = r);
                yield return new WaitUntil(() => result != null);
                if (!result.success) { Finish(false, "Life consumption failed: " + result.code); yield break; }
            }
            result = null; client.PrepareSimulatedRevive(Guid.NewGuid().ToString(), r => result = r);
            yield return new WaitUntil(() => result != null);
            if (!result.success || string.IsNullOrEmpty(result.attemptId)) { Finish(false, "Prepare failed: " + result.code); yield break; }
            var attempt = result.attemptId;
            result = null; client.GrantSimulatedRevive(attempt, r => result = r);
            yield return new WaitUntil(() => result != null);
            if (!result.success || result.lives != 1 || result.rewardMode != "simulated") { Finish(false, "Simulated revive failed: " + result.code); yield break; }
            result = null; client.GetState(r => result = r);
            yield return new WaitUntil(() => result != null);
            if (!result.success || result.lives != 1) { Finish(false, "State refresh failed: " + result.code); yield break; }
            Finish(true, "Unity local demo connection and simulated flow passed.");
        }

        private static void Finish(bool success, string message)
        {
            SessionState.SetBool(RunningKey, false);
            if (success) Debug.Log(message); else Debug.LogError(message);
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
#endif
