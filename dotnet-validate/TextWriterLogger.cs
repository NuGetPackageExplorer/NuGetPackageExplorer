using System;
using System.IO;
using System.Threading.Tasks;

using NuGet.Common;

namespace NuGetPe
{
    public class TextWriterLogger : LoggerBase
    {
        private readonly TextWriter _textWriter;

        public TextWriterLogger(TextWriter textWriter)
        {
            _textWriter = textWriter ?? throw new ArgumentNullException(nameof(textWriter));
        }

        public override void Log(ILogMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            _textWriter.WriteLine(FormatMessage(message));
        }

        public override async Task LogAsync(ILogMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            await _textWriter.WriteLineAsync(FormatMessage(message)).ConfigureAwait(false);
        }

        private static string FormatMessage(ILogMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            return $@"[{message.Time:T} {message.Level}] {message.Message}";
        }
    }
}
