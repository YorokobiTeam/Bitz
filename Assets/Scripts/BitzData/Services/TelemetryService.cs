using System;

class TelemetryService : GenericSupabaseService
{
    public static void RecordException(Exception e)
    {
        supabase
            .From<ExceptionLog>()
            .Insert(new ExceptionLog { LogText = e.StackTrace ?? e.Message });
    }
}
