using System;
using System.Threading.Tasks;

using NuGet.Common;

namespace NuGetPe
{
    public class ConsoleLogger : LoggerBase
    {
        public override void Log(ILogMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            WriteMessage(message);
        }

        public override Task LogAsync(ILogMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            WriteMessage(message);
            return Task.CompletedTask;
        }

        private static void WriteMessage(ILogMessage message)
        {
            Console.ForegroundColor = GetColor(message.Level);
            Console.WriteLine($@"[{message.Time:T} {message.Level}] {message.Message}");
            Console.ResetColor();
        }

        private static ConsoleColor GetColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => ConsoleColor.DarkGray,
                LogLevel.Verbose => ConsoleColor.DarkGray,
                LogLevel.Information => ConsoleColor.DarkGray,
                LogLevel.Minimal => ConsoleColor.DarkGray,
                LogLevel.Warning => ConsoleColor.DarkYellow,
                LogLevel.Error => ConsoleColor.DarkRed,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, $@"The value of argument '{nameof(level)}' ({level}) is invalid for enum type '{nameof(LogLevel)}'.")
            };
        }
    }
}
