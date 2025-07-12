using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

public class GameDataService : GenericSupabaseService
{
    /// <summary>
    /// This class manages IO and file related operations in Bitz.
    /// </summary>
    // Singleton boilerplate
    internal static GameDataService Instance { get; private set; }

    public static GameDataService GetInstance()
    {
        if (Instance is null)
        {
            Initialize();
        }
        return Instance!;
    }

    private static void Initialize() => Instance ??= new GameDataService();

    // You can't instantiate this.
    private GameDataService() { }

#nullable enable

    public async Task<BitzMap?> GetBitzMapAsync(string id)
    {
        return (
            await supabase
                .From<BitzMap>()
                .Select("*")
                .Filter("map_id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Get()
        ).Models[0];
    }

#nullable disable
    public async Task<List<BitzMap>> QueryMapsAsync(string query = "")
    {
        return (
            await supabase.From<BitzMap>().Select("*").Where(x => x.SongName.Contains(query)).Get()
        ).Models;
    }

#nullable enable
    public async Task<PlayerInfo?> GetPlayerInfoAsync(Guid id)
    {
        return (
            await supabase.From<PlayerInfo>().Select("*").Where(x => x.PlayerId == id).Get()
        ).Models[0];
    }

#nullable disable
    public async Task UpsertBeatmap(BitzMap map)
    {
        await supabase.From<BitzMap>().Upsert(map);
    }

    public async Task UpdatePlayerInfo(PlayerInfo info)
    {
        await supabase.From<PlayerInfo>().Where(x => x.PlayerId == info.PlayerId).Update(info);
    }

    public async void DeleteBeatmap(Guid map_id)
    {
        await supabase.From<BitzMap>().Where(x => x.Id == map_id).Delete();
    }
}
