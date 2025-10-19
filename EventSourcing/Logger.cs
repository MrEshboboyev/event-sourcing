namespace EventSourcing;

public static class Logger
{
    public static void Info(string message)
    {
        Log("INFO", message);
    }

    public static void Warn(string message)
    {
        Log("WARN", message);
    }

    public static void Error(string message)
    {
        Log("ERROR", message);
    }

    public static void Debug(string message)
    {
        #if DEBUG
        Log("DEBUG", message);
        #endif
    }

    private static void Log(string level, string message)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var threadId = Environment.CurrentManagedThreadId;
        Console.WriteLine($"[{timestamp}] [{level}] [Thread-{threadId}] {message}");
    }
}
