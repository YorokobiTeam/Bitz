using System;
using UnityEngine;

/// <summary>Optional local testing panel. Hidden by default; toggle with F1 or a four-finger tap.</summary>
[RequireComponent(typeof(BitzLocalServerClient))]
public sealed class BitzLocalServerPanel : MonoBehaviour
{
    private BitzLocalServerClient client;
    private string playerName;
    private string serverUrlInput;
    private string mapId;
    private string startRequestId;
    private string prepareRequestId;
    private string attemptId;
    private string message = "Auto-connecting to Bitz-Server...";
    private bool busy;

    private void Awake()
    {
        client = BitzLocalServerClient.Shared != null ? BitzLocalServerClient.Shared : GetComponent<BitzLocalServerClient>();
        playerName = !string.IsNullOrWhiteSpace(client.PlayerName)
            ? client.PlayerName
            : BitzLocalServerClient.GetOrCreatePersistentPlayerName();
        serverUrlInput = !string.IsNullOrWhiteSpace(BitzLocalServerClient.GetServerUrlOverride())
            ? BitzLocalServerClient.GetServerUrlOverride()
            : client.ActiveBaseUrl ?? "";
        mapId = Guid.NewGuid().ToString();
        startRequestId = Guid.NewGuid().ToString();
        prepareRequestId = Guid.NewGuid().ToString();
    }

    private void OnGUI()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!BitzDebugOverlay.Visible) return;
        if (client == null) client = BitzLocalServerClient.Shared ?? GetComponent<BitzLocalServerClient>();
        if (client == null) return;

        GUILayout.BeginArea(new Rect(12, 12, 420, 410), GUI.skin.box);
        var livesText = client.LastKnownLives.HasValue
            ? $" | Lives: {client.LastKnownLives.Value}/{client.LastKnownMaxLives ?? 0}"
            : "";
        GUILayout.Label($"Bitz-Server ({client.State}{livesText})");
        GUILayout.Label($"URL: {client.ActiveBaseUrl ?? "(unconfigured)"}");

        GUILayout.BeginHorizontal();
        serverUrlInput = GUILayout.TextField(serverUrlInput ?? "", 128);
        if (GUILayout.Button("Save URL", GUILayout.Width(80)))
        {
            BitzLocalServerClient.SetServerUrlOverride(serverUrlInput);
            message = "Updated server URL override and reconnecting.";
        }
        GUILayout.EndHorizontal();

        playerName = GUILayout.TextField(playerName ?? "", 32);
        GUI.enabled = !busy;
        if (GUILayout.Button("Connect / Reconnect"))
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
        if (GUILayout.Button("Simulate song start (spend 1 life)"))
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
        GUILayout.Label(string.IsNullOrEmpty(client.LastError) ? message : client.LastError);
        GUILayout.EndArea();
#endif
    }

    private void ShowResult(BitzLocalServerClient.Result result)
    {
        busy = false;
        message = result.success ? $"{result.message} Lives: {result.lives}/{result.maxLives}." : $"{result.code}: {result.message}";
    }
}
