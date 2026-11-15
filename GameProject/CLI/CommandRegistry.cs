namespace GameProject.CLI;

public sealed class CommandRegistry
{
    private readonly Dictionary<string, (string Help, Action<string[], CommandContext> Handler)> _commands = new(StringComparer.OrdinalIgnoreCase);
    public void Add(string name, string help, Action<string[], CommandContext> handler)
    {
        if (!_commands.TryAdd(name, (help, handler))) throw new InvalidOperationException($"Command '{name}' is already registered.");
    }
    public bool TryExecute(string name, string[] args, CommandContext context)
    {
        if (!_commands.TryGetValue(name, out var command)) return false;
        command.Handler(args, context);
        return true;
    }
    public void PrintHelp()
    {
        foreach (var item in _commands.Values) Console.WriteLine($"  {item.Help}");
        Console.WriteLine("  exit — leave the interpreter");
    }
}
