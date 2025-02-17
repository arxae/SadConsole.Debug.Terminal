using Serilog.Configuration;

namespace Serilog.Sinks.SadConsole;

public static class TerminalSinkExtension
{
    public static LoggerConfiguration SadConsoleTerminal(
        this LoggerSinkConfiguration loggerConfiguration,
        IFormatProvider formatProvider = null)
    {
        return loggerConfiguration.Sink(new TerminalSink(formatProvider));
    }
}