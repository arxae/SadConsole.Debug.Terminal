namespace SadConsole.Debug;

public static class Terminal
{
    public static TerminalConfiguration Configuration { get; set; } = new TerminalConfiguration();

    internal static List<TerminalMessage> Messages { get; set; } = [];

    // Built-in commands
    internal static List<TerminalCommand> Commands { get; set; } =
    [
        new TerminalCommand
        {
            Name = "help",
            Description = "Displays a list of available commands",
            Action = Help
        },
        new TerminalCommand
        {
            Name = "clear",
            Description = "Clears the terminal window",
            Action = Clear
        },
        new TerminalCommandArgs
        {
            Name = "verbose",
            Description = "Logs a message with the verbose level",
            ParameterHint = [
                new TerminalCommandParameterHint { Name = "message", Description = "The message to log", Type = "string" }
            ],
            Action = (args) => Verbose(string.Join(" ", args))
        },
    ];

    // Logging
    public static void AddMessage(string message, LogLevel level, object? context = null, Exception? exception = null, bool displayFlat = false)
    {
        Messages.Add(new TerminalMessage() {
            Message = message,
            Timestamp = DateTime.Now.TimeOfDay,
            Level = level,
            Context = context,
            Exception = exception,
            DisplayFlat = displayFlat
        });
    }

    // Logging shorthands
    public static void Verbose(string message) => AddMessage(message, LogLevel.Verbose);
    public static void Verbose(string message, object context) => AddMessage(message, LogLevel.Verbose, context);
    public static void Debug(string message) => AddMessage(message, LogLevel.Debug);
    public static void Debug(string message, object context) => AddMessage(message, LogLevel.Debug, context);
    public static void Information(string message) => AddMessage(message, LogLevel.Information);
    public static void Information(string message, object context) => AddMessage(message, LogLevel.Information, context);
    public static void Warning(string message) => AddMessage(message, LogLevel.Warning);
    public static void Warning(string message, object context) => AddMessage(message, LogLevel.Warning, context);
    public static void Warning(string message, Exception exception) => AddMessage(message, LogLevel.Warning, exception, exception);
    public static void Error(string message) => AddMessage(message, LogLevel.Error);
    public static void Error(string message, object context) => AddMessage(message, LogLevel.Error, context);
    public static void Error(string message, Exception exception) => AddMessage(message, LogLevel.Error, exception, exception);
    public static void Fatal(string message) => AddMessage(message, LogLevel.Fatal);
    public static void Fatal(string message, object context) => AddMessage(message, LogLevel.Fatal, context);
    public static void Fatal(string message, Exception exception) => AddMessage(message, LogLevel.Fatal, exception, exception);

    // Terminal methods
    public static void Clear() => Messages.Clear();

    /// <summary>
    /// Dumps all messages to a file
    /// </summary>
    /// <param name="path">The path where to save the file to</param>
    /// <param name="messagesOnly">If set to true, only save the messages. If set to false, include context and exception if present</param>
    public static void DumpLogToFile(string path, bool messagesOnly = false, bool onlyFiltered = false)
    {
        // Reuse the imgui text filter for consistency
        Hexa.NET.ImGui.ImGuiTextFilter filter = new();

        using StreamWriter writer = new(path);
        foreach (TerminalMessage message in Messages)
        {
            if (onlyFiltered && filter.PassFilter(message.Message + message.Timestamp.ToString() + message.Level.ToString()) == false) continue;

            writer.WriteLine($"{message.Timestamp.ToString(Configuration.TimestampFormat)} [{message.Level}] {message.Message}");

            if (messagesOnly == false)
            {
                if (message.Context != null)
                {
                    writer.WriteLine($"\tContext: {message.Context}");
                }

                if (message.Exception != null)
                {
                    writer.WriteLine($"\tException: {message.Exception}");
                }

                writer.WriteLine();
            }
        }

        AddMessage($"Log dumped to {path}", LogLevel.Information, displayFlat: true);
    }

    // Commands
    public static void RegisterCommand(TerminalCommand command)
    {
        if(command.Action == null)
        {
            AddMessage($"Command {command.Name} does not have an action assigned to it", LogLevel.Error, displayFlat: true);
            return;
        }

        Commands.Add(command);
        AddMessage($"Command Registered: {command.Name}", LogLevel.Verbose, displayFlat: true);
    }

    static void Help()
    {
        foreach (var command in Commands)
        {
            AddMessage($"{command.Name} - {command.Description}", LogLevel.Information, displayFlat: true);

            if (command is TerminalCommandArgs argsCommand)
            {
                foreach (TerminalCommandParameterHint hint in argsCommand.ParameterHint)
                {
                    AddMessage($"\t\t{hint.Name} - {hint.Description} ({hint.Type})", LogLevel.Information, displayFlat: true);
                }
            }
        }
    }
}

public struct TerminalMessage
{
    public string Message;
    public TimeSpan Timestamp;
    public LogLevel Level;
    public bool DisplayFlat;
    public object? Context;
    public Exception? Exception;
}

public enum LogLevel
{
    Verbose,
    Debug,
    Information,
    Warning,
    Error,
    Fatal
}