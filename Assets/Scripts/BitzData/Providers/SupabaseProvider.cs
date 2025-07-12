using System;
using Supabase;

public class SupabaseProvider
{
    private static Client supabase;

    static SupabaseProvider()
    {
        // Static constructor to ensure the provider is initialized once
        InitializeAsync();
    }

    public static void InitializeAsync()
    {
        var options = new Supabase.SupabaseOptions
        {
            AutoConnectRealtime = true,
            AutoRefreshToken = true,
        };
        supabase = new Supabase.Client(
            Constants.SUPABASE_URL,
            Constants.SUPABASE_ANON_KEY,
            options
        );
    }

    public static Client GetInstance()
    {
        return supabase
            ?? throw new InvalidOperationException("SupabaseProvider has not been initialized.");
    }
}
