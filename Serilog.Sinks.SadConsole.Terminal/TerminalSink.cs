using Serilog.Core;
using Serilog.Events;
using SadConsole.Debug;

namespace Serilog.Sinks.SadConsole;

public class TerminalSink : ILogEventSink
{
    readonly IFormatProvider formatProvider;

    public TerminalSink(IFormatProvider formatProvider)
    {
        this.formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage(formatProvider);
        Terminal.AddMessage(message, MapSerilogLevelToTerminal(logEvent.Level),
            exception: logEvent.Exception,
            context: logEvent.Properties);
    }

    LogLevel MapSerilogLevelToTerminal(LogEventLevel termLevel)
    {
        return termLevel switch
        {
            LogEventLevel.Verbose => LogLevel.Verbose,
            LogEventLevel.Debug => LogLevel.Debug,
            LogEventLevel.Information => LogLevel.Information,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Error => LogLevel.Error,
            _ => LogLevel.Information
        };
    }
}
