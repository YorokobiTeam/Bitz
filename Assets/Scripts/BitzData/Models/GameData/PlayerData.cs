using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

/// <summary>
/// Represents the 'player_info' table in the database.
/// Stores general information about a player.
/// </summary>
[Table("player_info")]
public class PlayerInfo : BaseModel
{
    /// <summary>
    /// Unique identifier for the player.
    /// Defaults to the authenticated user's UID. Also the primary key.
    /// </summary>
    [PrimaryKey("player_id")]
    public Guid PlayerId { get; set; }

    /// <summary>
    /// The player's username.
    /// Defaults to "Player#XXXX" in the database.
    /// </summary>
    [Column("username")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// The player's experience points.
    /// Defaults to 0.
    /// </summary>
    [Column("experience")]
    public int Experience { get; set; }

    /// <summary>
    /// Foreign key to the player's profile picture file in storage. Nullable.
    /// </summary>
    [Column("pfp_file_id")]
    public Guid? ProfilePictureId { get; set; }
}

/// <summary>
/// Represents the 'player_setting' table in the database.
/// Stores various settings for a player.
/// </summary>
[Table("player_setting")]
public class PlayerSetting : BaseModel
{
    /// <summary>
    /// Unique identifier for the player.
    /// Defaults to the authenticated user's UID. Also the primary key.
    /// </summary>
    [PrimaryKey("player_id")]
    public Guid PlayerId { get; set; }

#nullable enable
    /// <summary>
    /// Foreign key to the menu background image file in storage. Nullable.
    /// </summary>
    [Column("menu_background_img_id")]
    public Guid? MenuBackgroundImgId { get; set; }

    /// <summary>
    /// Foreign key to the timeline background image file in storage. Nullable.
    /// </summary>
    [Column("timeline_background_img_id")]
    public Guid? TimelineBackgroundImgId { get; set; }

    /// <summary>
    /// Color setting for the up arrow. Nullable.
    /// </summary>
    [Column("up_arrow_color")]
    public string? UpArrowColor { get; set; }

    /// <summary>
    /// Color setting for the down arrow. Nullable.
    /// </summary>
    [Column("down_arrow_color")]
    public string? DownArrowColor { get; set; }

    /// <summary>
    /// Color setting for the left arrow. Nullable.
    /// </summary>
    [Column("left_arrow_color")]
    public string? LeftArrowColor { get; set; }

    /// <summary>
    /// Color setting for the right arrow. Nullable.
    /// </summary>
    [Column("right_arrow_color")]
    public string? RightArrowColor { get; set; }
#nullable disable
}
