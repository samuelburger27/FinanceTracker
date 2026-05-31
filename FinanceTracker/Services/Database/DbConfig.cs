namespace FinanceTracker.Services.Database;

public static class DbConfig
{
    public static string DatabasePath =>
        Path.Combine(FileSystem.AppDataDirectory, "finance_tracker_v1.db");
}