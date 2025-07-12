using Supabase.Postgrest.Attributes;

[Table("player_map_score")]
class BitzMapScore
{
    [Column("player_id")]
    public string PlayerId { get; set; }
    [Column("map_id")]
    public string MapId { get; set; }
    [Column("score")]
    public int Score { get; set; }
}
