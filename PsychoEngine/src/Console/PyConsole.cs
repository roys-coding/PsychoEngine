namespace PsychoEngine;

public static class PyConsole
{
    private readonly struct LogMessage
    {
        public string      Message  { get; }
        public LogSeverity Severity { get; }
        public string      Category { get; }

        public LogMessage(string message, LogSeverity severity, string category)
        {
            Message  = message;
            Severity = severity;
            Category = category;
        }
    }
    
    private static readonly List<LogMessage> LoggedMessages;
    
    static PyConsole()
    {
        LoggedMessages = [];
    }

    public static bool Clear()
    {
        if (LoggedMessages.Count == 0)
        {
            return false;
        }
        
        LoggedMessages.Clear();
        return true;
    }

    public static void Log(string message, LogSeverity severity = LogSeverity.Info, string category = "")
    {
        LoggedMessages.Add(new LogMessage(message, severity, category));
    }

    public static void LogDebug(string message, string category = "")
    {
        Log(message, LogSeverity.Debug, category);
    }

    public static void LogInfo(string message, string category = "")
    {
        Log(message, LogSeverity.Info, category);
    }

    public static void LogSuccess(string message, string category = "")
    {
        Log(message, LogSeverity.Success, category);
    }

    public static void LogWarning(string message, string category = "")
    {
        Log(message, LogSeverity.Warning, category);
    }

    public static void LogError(string message, string category = "")
    {
        Log(message, LogSeverity.Error, category);
    }

    public static void LogFatal(string message, string category = "")
    {
        Log(message, LogSeverity.Fatal, category);
    }
}