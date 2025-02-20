namespace SadConsole.Debug;

public class TerminalCommand
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public virtual Action? Action { get; set; }
}

public struct TerminalCommandParameterHint
{
    public string Name;
    public string Description;
    public string Type;
    public bool Optional;
}

public class TerminalCommandArgs : TerminalCommand
{
    public required List<TerminalCommandParameterHint> ParameterHint { get; set; }
    public required new Action<string[]> Action { get; set; }
}