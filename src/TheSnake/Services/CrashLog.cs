namespace TheSnake.Services;

/// <summary>
/// Keeps the last crash in a small text file so the next launch can offer to share it.
/// Written with plain file IO so it works even before MAUI has started.
/// </summary>
public static class CrashLog
{
    private static string? s_path;

    public static void Init(string filesDirectory) => s_path = Path.Combine(filesDirectory, "last-crash.txt");

    public static void Write(string source, object? error)
    {
        if (s_path is null || error is null)
        {
            return;
        }

        try
        {
            string version = typeof(CrashLog).Assembly.GetName().Version?.ToString() ?? "?";
            File.WriteAllText(s_path, $"The Snake {version}\n{DateTime.UtcNow:u}\nSource: {source}\n\n{error}");
        }
        catch (IOException)
        {
        }
    }

    /// <summary>Returns the last crash report once, then forgets it.</summary>
    public static string? TakeLast()
    {
        if (s_path is null || !File.Exists(s_path))
        {
            return null;
        }

        try
        {
            string text = File.ReadAllText(s_path);
            File.Delete(s_path);
            return text;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
