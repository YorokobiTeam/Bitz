using System;
using UnityEngine;

/// <summary>Optional local testing panel. Add this component to a GameObject in a test scene.</summary>
[RequireComponent(typeof(BitzLocalServerClient))]
public sealed class BitzLocalServerPanel : MonoBehaviour
{
    private BitzLocalServerClient client;
    private string playerName = "local_tester";
    private string mapId;
    private string startRequestId;
    private string prepareRequestId;
    private string attemptId;
    private string message = "Start the local Bitz demo server, then connect.";
    private bool busy;

    private void Awake()
    {
        client = GetComponent<BitzLocalServerClient>();
        mapId = Guid.NewGuid().ToString();
        startRequestId = Guid.NewGuid().ToString();
        prepareRequestId = Guid.NewGuid().ToString();
    }

    private void OnGUI()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        GUILayout.BeginArea(new Rect(12, 12, 390, 330), GUI.skin.box);
        GUILayout.Label("Local demo server — simulated rewards");
        playerName = GUILayout.TextField(playerName, 32);
        GUI.enabled = !busy;
        if (GUILayout.Button("Connect"))
        {
            busy = true;
            client.SignIn(playerName, result =>
            {
                if (result.success) { attemptId = null; startRequestId = Guid.NewGuid().ToString(); prepareRequestId = Guid.NewGuid().ToString(); }
                ShowResult(result);
            });
        }
        GUI.enabled = !busy && client.IsSignedIn;
        if (GUILayout.Button("Refresh lives")) { busy = true; client.GetState(ShowResult); }
        if (GUILayout.Button("Simulate song start"))
        {
            busy = true;
            client.StartSong(mapId, startRequestId, result =>
            {
                if (result.success) startRequestId = Guid.NewGuid().ToString();
                ShowResult(result);
            });
        }
        if (GUILayout.Button("Prepare simulated revive"))
        {
            busy = true;
            client.PrepareSimulatedRevive(prepareRequestId, result =>
            {
                if (result.success) { attemptId = result.attemptId; prepareRequestId = Guid.NewGuid().ToString(); }
                ShowResult(result);
            });
        }
        GUI.enabled = !busy && client.IsSignedIn && !string.IsNullOrEmpty(attemptId);
        if (GUILayout.Button("Grant simulated revive (no ad)"))
        { busy = true; client.GrantSimulatedRevive(attemptId, ShowResult); }
        GUI.enabled = true;
        GUILayout.Label(message);
        GUILayout.EndArea();
#endif
    }

    private void ShowResult(BitzLocalServerClient.Result result)
    {
        busy = false;
        message = result.success ? $"{result.message} Lives: {result.lives}/{result.maxLives}." : $"{result.code}: {result.message}";
    }
}
