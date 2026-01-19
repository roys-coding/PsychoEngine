namespace PsychoEngine;

public static class PyConsole
{
    private readonly struct LogMessage
    {
        public string Message { get; }
        public LogSeverity Severity { get; }
        
        public LogMessage(string message, LogSeverity severity)
        {
            Message       = message;
            Severity = severity;
        }
    }
    
    private static readonly List<LogMessage> LoggedMessages;
    
    static PyConsole()
    {
        LoggedMessages = [];
    }
    
    public static void Log(string message, LogSeverity severity) { }
    public static void LogDebug(string message) { }
    public static void LogInfo(string message) { }
    public static void LogSuccess(string message) { }
    public static void LogWarning(string message) { }
    public static void LogError(string message) { }
    public static void LogFatal(string message) { }
}