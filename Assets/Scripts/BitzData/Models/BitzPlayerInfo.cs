using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

[Table("player_info"), Serializable]
public class BitzPlayerInfo : BaseModel
{
    [PrimaryKey("player_id")]
    public string Id { get; set; }
    [Column("username")]
    public string Username { get; set; }
    [Column("pfp_file_id")]
    public string ProfileImageFileId { get; set; }
    [Column("experience")]
    public int Experience { get; set; }
}
