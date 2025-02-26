using System.Runtime.CompilerServices;
using NLog;

namespace PKHeX.TemplateRegen;

public static class LogUtil
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public static void Log(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        Logger.Info(message);
    }
}
