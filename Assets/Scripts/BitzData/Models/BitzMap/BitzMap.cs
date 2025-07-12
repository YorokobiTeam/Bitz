using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Text.Json.Serialization;


[Table("map")]
public class BitzMap : BaseModel
{
    [PrimaryKey("map_id"), JsonPropertyName("map_id")]
    public Guid Id { get; set; }

    [Column("music_file_id"), JsonPropertyName("music_file_id")]
    public Guid MusicFileId { get; set; }

    [Column("background_file_id"), JsonPropertyName("background_file_id")]
    public Guid BackgroundFileId { get; set; }

    [Column("cover_file_id"), JsonPropertyName("cover_file_id")]
    public Guid CoverFileId { get; set; }

    [Column("map_author_id"), JsonPropertyName("map_author_id")]
    public Guid MapAuthorId { get; set; }

    [Column("song_name"), JsonPropertyName("song_name")]
    public string SongName { get; set; }

    [Column("song_author"), JsonPropertyName("song_author")]
    public string SongAuthor { get; set; }

    [Column("difficulty"), JsonPropertyName("song_author")]
    public string Difficulty { get; set; }

    [JsonPropertyName("is_offline")]
    public bool IsOffline { get; set; }
}