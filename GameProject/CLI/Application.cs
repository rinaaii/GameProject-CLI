using GameProject.Abstractions;
using GameProject.Characters;
using GameProject.Items;
using GameProject.TextModel;
using GameProject.Infrastructure;
using GameProject.Gameplay;

namespace GameProject.CLI;

public sealed class Application
{
    public int Run(string[] args)
    {
        var mode = SelectMode(args);
        if (mode is null) return 0;

        var characterStore = CreateCharacterStore();
        CharacterCatalogPersistence.LoadInto(characterStore);
        var context = new CommandContext(characterStore, CreateTextWorkspace(), new GameSessionService());
        using var api = new GenshinApiClient();
        var registry = new CommandRegistry();
        if (mode == InterfaceMode.Characters || mode == InterfaceMode.Game) RegisterCharacterCommands(registry, context, api);
        if (mode == InterfaceMode.Game) RegisterGameCommands(registry, context);
        if (mode == InterfaceMode.Text) RegisterTextCommands(registry, context);
        RegisterCommonCommands(registry);

        Console.WriteLine(mode switch { InterfaceMode.Characters => "Character catalogue console. Type 'help' to see commands, 'exit' to quit.", InterfaceMode.Game => "Text game console. Type 'help' to see commands, 'exit' to quit.", _ => "Structured text console. Type 'help' to see commands, 'exit' to quit." });
        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var parts = CommandLineParser.Parse(line);
                if (parts.Count == 0) continue;
                if (parts[0].Equals("exit", StringComparison.OrdinalIgnoreCase) || parts[0].Equals("quit", StringComparison.OrdinalIgnoreCase)) break;
                if (!registry.TryExecute(parts[0], parts.Skip(1).ToArray(), context))
                    Console.WriteLine($"Unknown command '{parts[0]}'. Type 'help'.");
            }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        }
        return 0;
    }


    // Composition root: swap these factories to use another storage/workspace implementation.
    private static ICharacterStore CreateCharacterStore() => new CharacterStore();
    private static ITextWorkspace CreateTextWorkspace() => new TextWorkspace();

    private static InterfaceMode? SelectMode(string[] args)
    {
        var option = args.FirstOrDefault(a => a.StartsWith("--", StringComparison.Ordinal));
        if (option is not null)
        {
            if (option.Equals("--text", StringComparison.OrdinalIgnoreCase)) return InterfaceMode.Text;
            if (option.Equals("--chars", StringComparison.OrdinalIgnoreCase) || option.Equals("--characters", StringComparison.OrdinalIgnoreCase)) return InterfaceMode.Characters;
            if (option.Equals("--game", StringComparison.OrdinalIgnoreCase)) return InterfaceMode.Game;
            Console.WriteLine($"Unknown startup option '{option}'. Use --text, --chars, or --game.");
            return null;
        }
        while (true)
        {
            Console.WriteLine("Choose interface mode: 1) Text  2) Characters  3) Game  0) Exit");
            Console.Write("Mode> ");
            var choice = Console.ReadLine()?.Trim();
            if (choice is "1" || choice?.Equals("text", StringComparison.OrdinalIgnoreCase) == true) return InterfaceMode.Text;
            if (choice is "2" || choice?.Equals("chars", StringComparison.OrdinalIgnoreCase) == true || choice?.Equals("characters", StringComparison.OrdinalIgnoreCase) == true) return InterfaceMode.Characters;
            if (choice is "3" || choice?.Equals("game", StringComparison.OrdinalIgnoreCase) == true) return InterfaceMode.Game;
            if (choice is "0" or null) return null;
            Console.WriteLine("Please enter 1, 2, 3, or 0.");
        }
    }

    private static void RegisterCommonCommands(CommandRegistry r)
    {
        r.Add("help", "help", (_, _) => r.PrintHelp());
    }

    private static void RegisterCharacterCommands(CommandRegistry r, CommandContext c, GenshinApiClient api)
    {
        new GenshinCommands(api).Register(r, c);
        r.Add("create", "create <char|item|ability> — create an object through a dialog", (a, _) => CharacterCommands.Create(a, c));
        r.Add("add", "add <item|ability> <id/name> --char_id <id/name> [--id <ability-id>]", (a, _) => CharacterCommands.Add(a, c));
        r.Add("act", "act <attack|heal|ability> <actor> <target> [--id <ability-id>]", (a, _) => CharacterCommands.Act(a, c));
        r.Add("ls", "ls <char|item|ability> [--id <id/name>]", (a, _) => CharacterCommands.List(a, c));
        r.Add("show", "show <character-id/name> — detailed character information and graphic link", (a, _) => CharacterCommands.Show(a, c));
    }

    private static void RegisterGameCommands(CommandRegistry r, CommandContext c)
    {
        r.Add("game", "game <characters|start|status|history|save|load|results> — text game operations", (a, _) => GameCommands.Execute(a, c));
    }

    private static void RegisterTextCommands(CommandRegistry r, CommandContext c)
    {
        r.Add("pwd", "pwd — show current document path", (_, _) => Console.WriteLine(c.Text.CurrentPath));
        r.Add("print", "print [--whole] [--id] — render current element/document", (a, _) => TextCommands.Print(a, c));
        r.Add("add", "add <container|leaf> <heading|paragraph|list> — create an element", (a, _) => TextCommands.Add(a, c));
        r.Add("rm", "rm [<element-name>] — remove child or current element", (a, _) => TextCommands.Remove(a, c));
        r.Add("up", "up — move to parent container", (_, _) => c.Text.MoveUp());
        r.Add("cd", "cd <path> | cd --id <container-id> — navigate", (a, _) => TextCommands.ChangeDirectory(a, c));
    }
}

public enum InterfaceMode { Text, Characters, Game }
