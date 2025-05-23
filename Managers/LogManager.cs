using System.Runtime.CompilerServices;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace PKHeX.TemplateRegen;

public static class AppLogManager  // Renamed to avoid conflict with NLog.LogManager
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static RichTextBox? _logTextBox;
    private static readonly object LogLock = new();
    private static readonly Queue<LogEntry> PendingLogs = new();
    private static System.Windows.Forms.Timer? _logTimer;

    public static void Initialize(RichTextBox logTextBox)
    {
        _logTextBox = logTextBox;

        // Configure NLog
        var config = new LoggingConfiguration();

        // Console target
        var consoleTarget = new ConsoleTarget("console")
        {
            Layout = "${longdate} ${level:uppercase=true} ${message}"
        };
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);

        // File target
        var fileTarget = new FileTarget("file")
        {
            FileName = "${basedir}/logs/${shortdate}.log",
            Layout = "${longdate} ${level:uppercase=true} ${message} ${exception:format=tostring}",
            ArchiveEvery = FileArchivePeriod.Day,
            ArchiveNumbering = ArchiveNumberingMode.Rolling,
            MaxArchiveFiles = 7
        };
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, fileTarget);

        // Custom target for UI
        var uiTarget = new CustomUITarget("ui");
        config.AddRule(LogLevel.Info, LogLevel.Fatal, uiTarget);

        LogManager.Configuration = config;

        // Setup timer for UI updates
        _logTimer = new System.Windows.Forms.Timer
        {
            Interval = 100 // Update UI every 100ms
        };
        _logTimer.Tick += OnLogTimerTick;
        _logTimer.Start();

        Log("Log system initialized");
    }

    public static void Log(string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        Logger.Info(message);

        lock (LogLock)
        {
            PendingLogs.Enqueue(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Info,
                Message = message
            });
        }
    }

    public static void LogError(string message, Exception? exception = null)
    {
        if (exception != null)
        {
            Logger.Error(exception, message);
        }
        else
        {
            Logger.Error(message);
        }

        lock (LogLock)
        {
            PendingLogs.Enqueue(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Error,
                Message = message,
                Exception = exception
            });
        }
    }

    public static void LogWarning(string message)
    {
        Logger.Warn(message);

        lock (LogLock)
        {
            PendingLogs.Enqueue(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Warn,
                Message = message
            });
        }
    }

    public static void LogDebug(string message)
    {
        Logger.Debug(message);

        lock (LogLock)
        {
            PendingLogs.Enqueue(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Debug,
                Message = message
            });
        }
    }

    private static void OnLogTimerTick(object? sender, EventArgs e)
    {
        if (_logTextBox == null || _logTextBox.IsDisposed)
            return;

        List<LogEntry> logsToProcess;
        lock (LogLock)
        {
            if (PendingLogs.Count == 0)
                return;

            logsToProcess = new List<LogEntry>(PendingLogs);
            PendingLogs.Clear();
        }

        if (_logTextBox.InvokeRequired)
        {
            _logTextBox.BeginInvoke(new Action(() => ProcessLogs(logsToProcess)));
        }
        else
        {
            ProcessLogs(logsToProcess);
        }
    }

    private static void ProcessLogs(List<LogEntry> logs)
    {
        if (_logTextBox == null || _logTextBox.IsDisposed)
            return;

        _logTextBox.SuspendLayout();

        foreach (var log in logs)
        {
            var timestamp = log.Timestamp.ToString("HH:mm:ss");
            var levelText = GetLevelText(log.Level);
            var color = GetLevelColor(log.Level);

            // Add timestamp
            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.SelectionLength = 0;
            _logTextBox.SelectionColor = Color.Gray;
            _logTextBox.AppendText($"[{timestamp}] ");

            // Add level
            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.SelectionLength = 0;
            _logTextBox.SelectionColor = color;
            _logTextBox.AppendText($"[{levelText}] ");

            // Add message
            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.SelectionLength = 0;
            _logTextBox.SelectionColor = Color.White;
            _logTextBox.AppendText($"{log.Message}\n");

            // Add exception details if present
            if (log.Exception != null)
            {
                _logTextBox.SelectionStart = _logTextBox.TextLength;
                _logTextBox.SelectionLength = 0;
                _logTextBox.SelectionColor = Color.OrangeRed;
                _logTextBox.AppendText($"  Exception: {log.Exception.Message}\n");

                if (!string.IsNullOrEmpty(log.Exception.StackTrace))
                {
                    _logTextBox.SelectionStart = _logTextBox.TextLength;
                    _logTextBox.SelectionLength = 0;
                    _logTextBox.SelectionColor = Color.DarkOrange;
                    _logTextBox.AppendText($"  Stack: {log.Exception.StackTrace}\n");
                }
            }
        }

        // Auto-scroll to bottom
        _logTextBox.SelectionStart = _logTextBox.TextLength;
        _logTextBox.ScrollToCaret();

        _logTextBox.ResumeLayout();

        // Limit log size
        if (_logTextBox.Lines.Length > 1000)
        {
            var lines = _logTextBox.Lines.Skip(_logTextBox.Lines.Length - 800).ToArray();
            _logTextBox.Lines = lines;
        }
    }

    private static string GetLevelText(LogLevel level)
    {
        return level.Name.ToUpper().PadRight(5);
    }

    private static Color GetLevelColor(LogLevel level)
    {
        if (level >= LogLevel.Error)
            return Color.Red;
        if (level >= LogLevel.Warn)
            return Color.Yellow;
        if (level >= LogLevel.Info)
            return Color.LightGreen;
        return Color.Gray;
    }

    public static void Clear()
    {
        lock (LogLock)
        {
            PendingLogs.Clear();
        }

        if (_logTextBox?.InvokeRequired == true)
        {
            _logTextBox.BeginInvoke(new Action(() => _logTextBox.Clear()));
        }
        else
        {
            _logTextBox?.Clear();
        }
    }

    public static void Dispose()
    {
        _logTimer?.Stop();
        _logTimer?.Dispose();
        _logTimer = null;
        _logTextBox = null;
    }

    private class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; } = LogLevel.Info;
        public string Message { get; set; } = "";
        public Exception? Exception { get; set; }
    }

    private class CustomUITarget : TargetWithLayout
    {
        public CustomUITarget(string name) : base()
        {
            Name = name;
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var message = RenderLogEvent(Layout, logEvent);

            lock (LogLock)
            {
                PendingLogs.Enqueue(new LogEntry
                {
                    Timestamp = logEvent.TimeStamp,
                    Level = logEvent.Level,
                    Message = message,
                    Exception = logEvent.Exception
                });
            }
        }
    }
}
