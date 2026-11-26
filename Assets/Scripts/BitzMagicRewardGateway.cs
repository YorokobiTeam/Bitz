using System;
using System.Threading.Tasks;
using Bitz_Server;
using Cysharp.Net.Http;
using Grpc.Net.Client;
using MagicOnion.Client;
using UnityEngine;

[MagicOnionClientGeneration(typeof(IBitzRewardGameV1))]
public partial class BitzRewardClientGeneration { }

public sealed class BitzMagicRewardGateway : IBitzRewardGateway, IDisposable
{
    [Serializable] private sealed class Config { public string serverUrl; public string rootCertificateResource; }
    private readonly GrpcChannel channel;
    private readonly IBitzRewardGameV1 client;

    private BitzMagicRewardGateway(GrpcChannel channel)
    {
        this.channel = channel;
        client = MagicOnionClient.Create<IBitzRewardGameV1>(channel);
    }

    public static BitzMagicRewardGateway CreateIfConfigured()
    {
        var asset = Resources.Load<TextAsset>("BitzRewardServerConfig");
        if (asset == null) return null;
        try
        {
            var config = JsonUtility.FromJson<Config>(asset.text);
            if (config == null || !Uri.TryCreate(config.serverUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo) ||
                uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return null;
            var handler = new YetAnotherHttpHandler();
            if (!string.IsNullOrWhiteSpace(config.rootCertificateResource))
            {
                var certificate = Resources.Load<TextAsset>(config.rootCertificateResource);
                if (certificate == null) { handler.Dispose(); return null; }
                handler.RootCertificates = certificate.text;
            }
            return new BitzMagicRewardGateway(GrpcChannel.ForAddress(uri, new GrpcChannelOptions { HttpHandler = handler, DisposeHttpClient = true }));
        }
        catch (Exception) { return null; }
    }

    public string CurrentPlayerId => SupabaseProvider.GetInstance().Auth.CurrentSession?.User?.Id;
    private string AccessToken => SupabaseProvider.GetInstance().Auth.CurrentSession?.AccessToken;

    public async Task<BitzRewardReply> GetLivesAsync()
    {
        var response = await client.WithDeadline(DateTime.UtcNow.AddSeconds(12)).GetLivesAsync(new RewardGameRequest { AccessToken = AccessToken ?? "" });
        return Convert(response);
    }
    public async Task<BitzRewardReply> SpendLifeAsync(string requestId, string mapId)
    {
        var response = await client.WithDeadline(DateTime.UtcNow.AddSeconds(12)).StartAsync(new RewardGameRequest { AccessToken = AccessToken ?? "", RequestId = requestId, MapId = mapId });
        return Convert(response);
    }

    public async Task<BitzRewardReply> PrepareAsync(string requestId)
    {
        var response = await client.WithDeadline(DateTime.UtcNow.AddSeconds(12)).PrepareAsync(new RewardGameRequest { AccessToken = AccessToken ?? "", RequestId = requestId });
        return Convert(response);
    }
    public async Task<BitzRewardReply> GetStatusAsync(string attemptId)
    {
        var response = await client.WithDeadline(DateTime.UtcNow.AddSeconds(12)).GetRewardStatusAsync(new RewardGameRequest { AccessToken = AccessToken ?? "", AttemptId = attemptId });
        return Convert(response);
    }
    public async Task<BitzRewardReply> SubmitReceiptAsync(string receipt)
    {
        var response = await client.WithDeadline(DateTime.UtcNow.AddSeconds(12)).SubmitReceiptAsync(new RewardGameRequest { AccessToken = AccessToken ?? "", Receipt = receipt });
        return Convert(response);
    }
    private static BitzRewardReply Convert(RewardGameReply result) => new BitzRewardReply
    {
        Code = result.Code, AttemptId = result.AttemptId, CustomData = result.CustomData,
        AppId = result.AppId, AdUnitId = result.AdUnitId, AttemptExpired = result.AttemptExpired, Lives = result.Lives
    };
    public void Dispose() => channel.Dispose();
}
