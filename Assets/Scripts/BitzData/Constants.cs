using System;
using System.IO;

public static class Constants
{
    public static readonly string APPLICATION_DATA = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Yorokobi",
        "Bitz"
    );
    public static readonly string CACHE_METADATA = Path.Join(APPLICATION_DATA, "cache", "metadata");
    public static readonly string CACHE_FILES = Path.Join(APPLICATION_DATA, "cache", "files");
    public static readonly string HISTORY_FILE = Path.Join(APPLICATION_DATA, "history.bitz.data");
    public static readonly string PREVIOUSLY_LOADED_MAPS = Path.Join(APPLICATION_DATA, "maps.bitz.data");


    public static readonly string FILENAME_MAP = "map.data";
    public static readonly string FILENAME_MUSIC = "music.mp3";
    // Hardcode for now, in the future these assets will be stored as downloaded from the StorageObjects.
    public static readonly string FILENAME_BACKGROUND = "background.png";
    public static readonly string FILENAME_ALBUM_COVER = "albumCover.png";


    public static readonly string SUPABASE_URL = "https://lkirbjjcaminpbjemsuo.supabase.co";
    public static readonly string SUPABASE_ANON_KEY =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImxraXJiampjYW1pbnBiamVtc3VvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDg0MTIxMjQsImV4cCI6MjA2Mzk4ODEyNH0.nLORgC6joshVn_przF6AFzQRv9KinScq8Ph5GovWmQs";
}
