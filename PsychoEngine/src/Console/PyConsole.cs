namespace PsychoEngine;

public static class PyConsole
{
    private readonly struct LogMessage
    {
        public string Message { get; }
        public LogSeverity Severity { get; }
        public string Category { get; }
        
        public LogMessage(string message, LogSeverity severity, string category)
        {
            Message       = message;
            Severity      = severity;
            Category = category;
        }
    }
    
    private static readonly List<LogMessage> LoggedMessages;
    
    static PyConsole()
    {
        LoggedMessages = [];
    }
    
    public static void Log(string message, LogSeverity severity = LogSeverity.Info, string category = "") { }
    public static void LogDebug(string message, string category = "") { }
    public static void LogInfo(string message, string category = "") { }
    public static void LogSuccess(string message, string category = "") { }
    public static void LogWarning(string message, string category = "") { }
    public static void LogError(string message, string category = "") { }
    public static void LogFatal(string message, string category = "") { }
}