namespace PsychoEngine;

public static partial class PyConsole
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
        
        public static implicit operator string(LogMessage message)
        {
            return message.Message;
        }
    }
    
    private static readonly List<LogMessage> LoggedMessages;
    private static readonly List<string> LoggedCategories;
    
    static PyConsole()
    {
        LoggedMessages   = [];
        LoggedCategories = [];
        
        LogDebug("LogDebug");
        LogInfo("LogInfo");
        LogSuccess("LogSuccess");
        LogWarning("LogWarning");
        LogError("LogError");
        LogFatal("LogFatal");

        InitializeImGui();
    }

    public static bool Clear()
    {
        if (LoggedMessages.Count == 0)
        {
            return false;
        }
        
        LoggedMessages.Clear();
        LoggedCategories.Clear();
        return true;
    }

    public static void Log(string message, LogSeverity severity = LogSeverity.Info, string category = "")
    {
        string categoryLower = category.ToLowerInvariant();
        
        LoggedMessages.Add(new LogMessage(message, severity, categoryLower));

        if (!LoggedCategories.Contains(categoryLower))
        {
            LoggedCategories.Add(categoryLower);
        }
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