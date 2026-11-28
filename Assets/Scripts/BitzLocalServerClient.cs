using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Client for the localhost-only simulated Bitz demo server. This does not authenticate a real
/// account or verify ads. Keep request IDs and reuse them when retrying an interrupted mutation.
/// </summary>
public sealed class BitzLocalServerClient : MonoBehaviour
{
    private const string BaseUrl = "http://127.0.0.1:5030";
    private string sessionToken;
    private int sessionGeneration;

    public bool IsSignedIn => !string.IsNullOrEmpty(sessionToken);
    public string PlayerId { get; private set; }

    [Serializable] public sealed class Result
    {
        public bool success;
        public string code;
        public string message;
        public string sessionToken;
        public string playerId;
        public string attemptId;
        public int lives;
        public int maxLives;
        public string serverTimeUtc;
        public string nextReviveUtc;
        public string attemptExpiresUtc;
        public string rewardMode;
    }

    [Serializable] private sealed class LoginBody { public string playerName; }
    [Serializable] private class SessionBody { public string sessionToken; }
    [Serializable] private sealed class OperationBody : SessionBody { public string requestId; }
    [Serializable] private sealed class StartBody : SessionBody { public string requestId; public string mapId; }

    public void SignIn(string playerName, Action<Result> completed)
    {
        if (string.IsNullOrWhiteSpace(playerName)) { completed?.Invoke(Error("invalid_name", "Enter a demo player name.")); return; }
        // A new sign-in must not silently retain the previous player's session.
        SignOut();
        StartCoroutine(Post("/demo/login", new LoginBody { playerName = playerName.Trim() }, result =>
        {
            if (result.success && !string.IsNullOrEmpty(result.sessionToken))
            {
                sessionToken = result.sessionToken;
                PlayerId = result.playerId;
                result.sessionToken = null;
            }
            completed?.Invoke(result);
        }));
    }

    public void SignOut() { sessionGeneration++; sessionToken = null; PlayerId = null; }

    public void GetState(Action<Result> completed)
    {
        if (!RequireSession(completed)) return;
        StartCoroutine(Post("/demo/state", new SessionBody { sessionToken = sessionToken }, completed));
    }

    public void StartSong(string mapId, string requestId, Action<Result> completed)
    {
        if (!RequireSession(completed) || !RequireGuids(completed, mapId, requestId)) return;
        StartCoroutine(Post("/demo/start", new StartBody { sessionToken = sessionToken, mapId = mapId, requestId = requestId }, completed));
    }

    public void PrepareSimulatedRevive(string requestId, Action<Result> completed)
    {
        if (!RequireSession(completed) || !RequireGuids(completed, requestId)) return;
        StartCoroutine(Post("/demo/revive/prepare", new OperationBody { sessionToken = sessionToken, requestId = requestId }, completed));
    }

    // This endpoint grants a simulated reward without showing or verifying an advertisement.
    public void GrantSimulatedRevive(string attemptId, Action<Result> completed)
    {
        if (!RequireSession(completed) || !RequireGuids(completed, attemptId)) return;
        StartCoroutine(Post("/demo/revive/simulate", new OperationBody { sessionToken = sessionToken, requestId = attemptId }, completed));
    }

    private bool RequireSession(Action<Result> completed)
    {
        if (IsSignedIn) return true;
        completed?.Invoke(Error("not_signed_in", "Connect with a demo player name first."));
        return false;
    }

    private static bool RequireGuids(Action<Result> completed, params string[] ids)
    {
        foreach (var id in ids)
            if (!Guid.TryParse(id, out var value) || value == Guid.Empty)
            { completed?.Invoke(Error("invalid_request", "Request, map and attempt IDs must be UUIDs.")); return false; }
        return true;
    }

    private static Result Error(string code, string message) => new Result { success = false, code = code, message = message, rewardMode = "simulated" };

    private IEnumerator Post(string path, object body, Action<Result> completed)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var generation = sessionGeneration;
        var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));
        using (var request = new UnityWebRequest(BaseUrl + path, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;
            yield return request.SendWebRequest();
            if (generation != sessionGeneration)
            {
                completed?.Invoke(Error("session_changed", "The demo session changed while the request was in progress."));
                yield break;
            }
            if (request.result != UnityWebRequest.Result.Success)
            {
                completed?.Invoke(Error("connection_failed", "Cannot reach the local demo server. Check that it is running."));
                yield break;
            }
            Result result;
            try { result = JsonUtility.FromJson<Result>(request.downloadHandler.text); }
            catch (Exception) { result = null; }
            if (result == null || string.IsNullOrEmpty(result.code)) result = Error("invalid_response", "The demo server returned an invalid response.");
            if (result.code == "unauthorized") SignOut();
            completed?.Invoke(result);
        }
#else
        completed?.Invoke(Error("unavailable", "The local demo client is available only in the Editor and development builds."));
        yield break;
#endif
    }
}
