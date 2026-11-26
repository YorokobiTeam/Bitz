using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Client for the Bitz demo game server. Auto-connects on startup via <see cref="Shared"/>
/// with backoff retries and never blocks gameplay when offline. Keep request IDs and reuse them
/// when retrying an interrupted mutation.
/// </summary>
public sealed class BitzLocalServerClient : MonoBehaviour
{
    public enum ConnectionState { Disconnected, Connecting, Connected, Offline }

    [Serializable]
    private sealed class ServerConfig
    {
        public string serverUrl;
        public bool allowSimulatedConfirmation = true;
    }

    private const string DefaultDevBaseUrl = "http://127.0.0.1:5030";
    private const string ConfigResourceName = "BitzLocalServerConfig";
    private const string PlayerNameKey = "bitz.localDemoPlayer.v1";
    private const string ServerUrlOverrideKey = "bitz.localServerUrlOverride.v1";

    private string sessionToken;
    private int sessionGeneration;
    private string configuredBaseUrl;
    private bool allowSimulatedConfirmation = true;
    private bool autoConnectEnabled;
    private Coroutine reconnectRoutine;
    private readonly List<TaskCompletionSource<bool>> pendingSignInWaiters = new List<TaskCompletionSource<bool>>();
    private bool signInInFlight;

    public static BitzLocalServerClient Shared { get; private set; }

    public bool IsSignedIn => !string.IsNullOrEmpty(sessionToken);
    public string PlayerId { get; private set; }
    public string PlayerName { get; private set; }
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public string LastError { get; private set; }
    public int? LastKnownLives { get; private set; }
    public int? LastKnownMaxLives { get; private set; }
    public string ActiveBaseUrl => ResolveBaseUrl();

    /// <summary>
    /// True only in Editor / Development builds when config allows calling the local demo server's
    /// simulated grant endpoint after a real rewarded ad finishes playing. Never enabled in release.
    /// </summary>
    public bool AllowSimulatedConfirmation
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return allowSimulatedConfirmation;
#else
            return false;
#endif
        }
    }

    public event Action<ConnectionState> StateChanged;

    [Serializable] public sealed class Result
    {
        public bool success;
        public string code;
        public string message;
        public string sessionToken;
        public string playerId;
        public string attemptId;
        public string customData;
        public string appId;
        public string adUnitId;
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Shared != null) return;
        var go = new GameObject("Bitz Server Client");
        DontDestroyOnLoad(go);
        Shared = go.AddComponent<BitzLocalServerClient>();
        Shared.BeginAutoConnect();
    }

    private void Awake()
    {
        LoadConfig();
    }

    private void OnDestroy()
    {
        StopReconnectLoop();
        ResolveSignInWaiters(false);
        if (Shared == this) Shared = null;
    }

    public static string GetOrCreatePersistentPlayerName()
    {
        var saved = PlayerPrefs.GetString(PlayerNameKey, "");
        if (!string.IsNullOrWhiteSpace(saved)) return saved.Trim();
        var generated = "bitz_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        PlayerPrefs.SetString(PlayerNameKey, generated);
        PlayerPrefs.Save();
        return generated;
    }

    public static string GetServerUrlOverride() => PlayerPrefs.GetString(ServerUrlOverrideKey, "").Trim();

    public static void SetServerUrlOverride(string url)
    {
        var trimmed = string.IsNullOrWhiteSpace(url) ? "" : url.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(trimmed)) PlayerPrefs.DeleteKey(ServerUrlOverrideKey);
        else PlayerPrefs.SetString(ServerUrlOverrideKey, trimmed);
        PlayerPrefs.Save();
        if (Shared != null) Shared.ReconnectNow();
    }

    public void BeginAutoConnect()
    {
        autoConnectEnabled = true;
        if (string.IsNullOrEmpty(ResolveBaseUrl()))
        {
            LastError = "Game server URL is not configured.";
            SetState(ConnectionState.Offline);
            return;
        }
        ReconnectNow();
    }

    public void ReconnectNow()
    {
        StopReconnectLoop();
        if (!isActiveAndEnabled) return;
        reconnectRoutine = StartCoroutine(AutoConnectLoop());
    }

    public Task<bool> EnsureSignedInAsync()
    {
        if (IsSignedIn) return Task.FromResult(true);
        if (string.IsNullOrEmpty(ResolveBaseUrl()))
        {
            LastError = "Game server URL is not configured.";
            SetState(ConnectionState.Offline);
            return Task.FromResult(false);
        }

        var tcs = new TaskCompletionSource<bool>();
        pendingSignInWaiters.Add(tcs);
        if (!signInInFlight)
        {
            var name = !string.IsNullOrWhiteSpace(PlayerName) ? PlayerName : GetOrCreatePersistentPlayerName();
            SignInInternal(name, null);
        }
        return tcs.Task;
    }

    public void SignIn(string playerName, Action<Result> completed)
    {
        if (string.IsNullOrWhiteSpace(playerName)) { completed?.Invoke(Error("invalid_name", "Enter a demo player name.")); return; }
        StopReconnectLoop();
        SignInInternal(playerName.Trim(), result =>
        {
            completed?.Invoke(result);
            if (!result.success && autoConnectEnabled && isActiveAndEnabled && reconnectRoutine == null)
                reconnectRoutine = StartCoroutine(AutoConnectLoop(initialDelaySeconds: 4f));
        });
    }

    public void SignOut()
    {
        sessionGeneration++;
        sessionToken = null;
        PlayerId = null;
        LastKnownLives = null;
        LastKnownMaxLives = null;
        SetState(ConnectionState.Disconnected);
    }

    public void GetState(Action<Result> completed)
    {
        if (!RequireSession(completed)) return;
        StartCoroutine(Post("/demo/state", new SessionBody { sessionToken = sessionToken }, result =>
        {
            UpdateCachedState(result);
            completed?.Invoke(result);
        }));
    }

    public void StartSong(string mapId, string requestId, Action<Result> completed)
    {
        if (!RequireSession(completed) || !RequireGuids(completed, mapId, requestId)) return;
        StartCoroutine(Post("/demo/start", new StartBody { sessionToken = sessionToken, mapId = mapId, requestId = requestId }, result =>
        {
            UpdateCachedState(result);
            completed?.Invoke(result);
        }));
    }

    public void PrepareSimulatedRevive(string requestId, Action<Result> completed)
    {
        if (!RequireSession(completed) || !RequireGuids(completed, requestId)) return;
        StartCoroutine(Post("/demo/revive/prepare", new OperationBody { sessionToken = sessionToken, requestId = requestId }, result =>
        {
            UpdateCachedState(result);
            completed?.Invoke(result);
        }));
    }

    // This endpoint grants a simulated reward on the local demo server. Never invoke unless a real
    // rewarded ad finished playing or explicitly requested from the developer debug panel.
    public void GrantSimulatedRevive(string attemptId, Action<Result> completed)
    {
        if (!RequireSession(completed) || !RequireGuids(completed, attemptId)) return;
        StartCoroutine(Post("/demo/revive/simulate", new OperationBody { sessionToken = sessionToken, requestId = attemptId }, result =>
        {
            UpdateCachedState(result);
            completed?.Invoke(result);
        }));
    }

    public Task<Result> GetStateAsync() => RunAsync(GetState);
    public Task<Result> StartSongAsync(string mapId, string requestId) => RunAsync(cb => StartSong(mapId, requestId, cb));
    public Task<Result> PrepareSimulatedReviveAsync(string requestId) => RunAsync(cb => PrepareSimulatedRevive(requestId, cb));
    public Task<Result> GrantSimulatedReviveAsync(string attemptId) => RunAsync(cb => GrantSimulatedRevive(attemptId, cb));

    private static Task<Result> RunAsync(Action<Action<Result>> call)
    {
        var tcs = new TaskCompletionSource<Result>();
        call(r => tcs.TrySetResult(r));
        return tcs.Task;
    }

    private IEnumerator AutoConnectLoop(float initialDelaySeconds = 0f)
    {
        if (initialDelaySeconds > 0f) yield return new WaitForSecondsRealtime(initialDelaySeconds);

        float delay = 2f;
        while (autoConnectEnabled && this != null)
        {
            if (IsSignedIn)
            {
                reconnectRoutine = null;
                yield break;
            }

            if (string.IsNullOrEmpty(ResolveBaseUrl()))
            {
                LastError = "Game server URL is not configured.";
                SetState(ConnectionState.Offline);
                reconnectRoutine = null;
                yield break;
            }

            if (!signInInFlight)
            {
                bool finished = false;
                bool succeeded = false;
                SignInInternal(GetOrCreatePersistentPlayerName(), result =>
                {
                    succeeded = result != null && result.success;
                    finished = true;
                });
                yield return new WaitUntil(() => finished);
                if (succeeded)
                {
                    reconnectRoutine = null;
                    yield break;
                }
            }

            yield return new WaitForSecondsRealtime(delay);
            delay = Mathf.Min(delay * 2f, 30f);
        }
        reconnectRoutine = null;
    }

    private void SignInInternal(string trimmedName, Action<Result> completed)
    {
        // A new sign-in must not silently retain the previous player's session.
        SignOut();
        PlayerName = trimmedName;
        signInInFlight = true;
        SetState(ConnectionState.Connecting);
        StartCoroutine(Post("/demo/login", new LoginBody { playerName = trimmedName }, result =>
        {
            signInInFlight = false;
            if (result.success && !string.IsNullOrEmpty(result.sessionToken))
            {
                sessionToken = result.sessionToken;
                PlayerId = result.playerId;
                result.sessionToken = null;
                LastError = null;
                UpdateCachedState(result);
                SetState(ConnectionState.Connected);
                ResolveSignInWaiters(true);
                // Fetch current lives in the background after auto sign-in so cached state is fresh.
                if (autoConnectEnabled && isActiveAndEnabled) GetState(_ => { });
            }
            else
            {
                LastError = result.message;
                SetState(ConnectionState.Offline);
                ResolveSignInWaiters(false);
            }
            completed?.Invoke(result);
        }));
    }

    private void ResolveSignInWaiters(bool signedIn)
    {
        if (pendingSignInWaiters.Count == 0) return;
        var copy = pendingSignInWaiters.ToArray();
        pendingSignInWaiters.Clear();
        foreach (var waiter in copy) waiter.TrySetResult(signedIn);
    }

    private void StopReconnectLoop()
    {
        if (reconnectRoutine == null) return;
        StopCoroutine(reconnectRoutine);
        reconnectRoutine = null;
    }

    private void UpdateCachedState(Result result)
    {
        if (result == null) return;
        if (result.success)
        {
            LastError = null;
            if (result.maxLives > 0 || result.lives > 0)
            {
                LastKnownLives = result.lives;
                LastKnownMaxLives = result.maxLives;
            }
            if (IsSignedIn && State != ConnectionState.Connected) SetState(ConnectionState.Connected);
        }
        else if (result.code == "connection_failed")
        {
            LastError = result.message;
            SetState(ConnectionState.Offline);
            if (autoConnectEnabled && isActiveAndEnabled && reconnectRoutine == null)
                reconnectRoutine = StartCoroutine(AutoConnectLoop(initialDelaySeconds: 4f));
        }
        else if (result.code == "unauthorized")
        {
            LastError = result.message;
            if (autoConnectEnabled && isActiveAndEnabled && reconnectRoutine == null)
                reconnectRoutine = StartCoroutine(AutoConnectLoop(initialDelaySeconds: 1f));
        }
    }

    private void SetState(ConnectionState next)
    {
        if (State == next) return;
        State = next;
        StateChanged?.Invoke(next);
    }

    private void LoadConfig()
    {
        var asset = Resources.Load<TextAsset>(ConfigResourceName);
        if (asset == null) return;
        try
        {
            var cfg = JsonUtility.FromJson<ServerConfig>(asset.text);
            if (cfg == null) return;
            configuredBaseUrl = NormalizeUrl(cfg.serverUrl);
            allowSimulatedConfirmation = cfg.allowSimulatedConfirmation;
        }
        catch (Exception e)
        {
            Debug.LogWarning("BitzLocalServerConfig could not be read: " + e.Message);
        }
    }

    private string ResolveBaseUrl()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var overrideUrl = NormalizeUrl(GetServerUrlOverride());
        if (!string.IsNullOrEmpty(overrideUrl)) return overrideUrl;
        if (!string.IsNullOrEmpty(configuredBaseUrl)) return configuredBaseUrl;
        return DefaultDevBaseUrl;
#else
        if (!string.IsNullOrEmpty(configuredBaseUrl) && configuredBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return configuredBaseUrl;
        return null;
#endif
    }

    private static string NormalizeUrl(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return null;
        var trimmed = candidate.Trim().TrimEnd('/');
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
        if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return null;
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
        if (uri.Scheme != Uri.UriSchemeHttps) return null;
#endif
        return trimmed;
    }

    private bool RequireSession(Action<Result> completed)
    {
        if (IsSignedIn) return true;
        completed?.Invoke(Error("not_signed_in", "Connect to the game server first."));
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
        var baseUrl = ResolveBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            completed?.Invoke(Error("unavailable", "Game server URL is not configured for this build."));
            yield break;
        }

        var generation = sessionGeneration;
        var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));
        using (var request = new UnityWebRequest(baseUrl + path, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 8;
            yield return request.SendWebRequest();
            if (generation != sessionGeneration)
            {
                completed?.Invoke(Error("session_changed", "The session changed while the request was in progress."));
                yield break;
            }
            if (request.result != UnityWebRequest.Result.Success)
            {
                completed?.Invoke(Error("connection_failed", "Cannot reach the game server. Check that it is running."));
                yield break;
            }
            Result result;
            try { result = JsonUtility.FromJson<Result>(request.downloadHandler.text); }
            catch (Exception) { result = null; }
            if (result == null || string.IsNullOrEmpty(result.code)) result = Error("invalid_response", "The game server returned an invalid response.");
            if (result.code == "unauthorized") SignOut();
            completed?.Invoke(result);
        }
    }
}
