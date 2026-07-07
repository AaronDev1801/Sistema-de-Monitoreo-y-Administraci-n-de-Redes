namespace MonitoringPlatform.Services;

public static class SupabaseRetentionPolicy
{
    public static IReadOnlyList<int> GetIdsToDelete(int currentCount, int maxRows)
    {
        if (currentCount <= maxRows)
        {
            return Array.Empty<int>();
        }

        var excess = currentCount - maxRows;
        return Enumerable.Range(1, excess).ToArray();
    }
}
