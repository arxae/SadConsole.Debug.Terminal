using SadConsole.Configuration;
using SadConsole.Debug.DebugTerminal;

namespace SadConsole.Debug;

public static class TerminalExtensions
{
    public static Builder AddDebugTerminal(this Builder builder,
    TerminalConfiguration? configuration = null)
    {
        Debugger.Opened += (opened) =>
        {
            if(opened == false) return;
            Debugger.GuiComponents.Add(new TerminalView());
            
            if(configuration != null)
            {
                Terminal.Configuration = configuration;
                return;
            }

            Terminal.Configuration = new TerminalConfiguration();
            Terminal.Configuration.LoadConfiguration();
        };

        Debugger.Closed += () => Terminal.Configuration.SaveConfiguration();

        return builder;
    }
}
