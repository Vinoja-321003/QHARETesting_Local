namespace QualiFrame.Mocking.Utilities;

/// <summary>
/// Simple logging helper for mock framework (can be extended to plug into Serilog/Allure/Extent).
/// </summary>
public class LoggingHelper
{
    public void Info(string message, params (string Key, object? Value)[] properties)
    {
        var parts = new List<string> { message };
        foreach (var (k, v) in properties)
            parts.Add($"{k}={v}");
        System.Diagnostics.Debug.WriteLine("[QualiFrame.Mocking] " + string.Join(" ", parts));
    }

    public void Warn(string message, params (string Key, object? Value)[] properties)
    {
        var parts = new List<string> { message };
        foreach (var (k, v) in properties)
            parts.Add($"{k}={v}");
        System.Diagnostics.Debug.WriteLine("[QualiFrame.Mocking WARN] " + string.Join(" ", parts));
    }

    public void Error(string message, Exception? ex = null, params (string Key, object? Value)[] properties)
    {
        var parts = new List<string> { message };
        foreach (var (k, v) in properties)
            parts.Add($"{k}={v}");
        if (ex != null)
            parts.Add($"Exception={ex.Message}");
        System.Diagnostics.Debug.WriteLine("[QualiFrame.Mocking ERROR] " + string.Join(" ", parts));
    }
}
