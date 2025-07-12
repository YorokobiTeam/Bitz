
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// This object holds metadata that should be synced between client and DB.
/// </summary>
public class StorageObjectMetadata
{
    [JsonPropertyName("e_updated_at")]
    public float UpdatedAt { get; set; }

    [JsonPropertyName("md5")]
    public string MD5 { get; set; }

    [JsonPropertyName("name")]
    public string ServerFilePath { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("e_created_at")]
    public float CreatedAt { get; set; }

    [JsonPropertyName("relative_dir")]
    public string RelativeLocalDirectory { get; set; }



}
