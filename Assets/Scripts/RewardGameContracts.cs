using MagicOnion;
using MessagePack;

namespace Bitz_Server
{

// A separate versioned contract: never send signing or token-derivation keys to Unity.
public interface IBitzRewardGameV1 : IService<IBitzRewardGameV1>
{
    UnaryResult<RewardGameReply> GetLivesAsync(RewardGameRequest request);
    UnaryResult<RewardGameReply> StartAsync(RewardGameRequest request);
    UnaryResult<RewardGameReply> PrepareAsync(RewardGameRequest request);
    UnaryResult<RewardGameReply> GetRewardStatusAsync(RewardGameRequest request);
    UnaryResult<RewardGameReply> SubmitReceiptAsync(RewardGameRequest request);
}

[MessagePackObject(true)] public sealed class RewardGameRequest
{
    public string AccessToken { get; set; } = "";
    public string RequestId { get; set; } = "";
    public string MapId { get; set; } = "";
    public string AttemptId { get; set; } = "";
    public string Receipt { get; set; } = "";
}

[MessagePackObject(true)] public sealed class RewardGameReply
{
    public string Code { get; set; } = "unavailable";
    public int? Lives { get; set; }
    public bool AlreadyStarted { get; set; }
    public string? AttemptId { get; set; }
    public string? CustomData { get; set; }
    public string? AppId { get; set; }
    public string? AdUnitId { get; set; }
    public long? ExpiresAtUnixMs { get; set; }
    public bool AttemptExpired { get; set; }
}
}
