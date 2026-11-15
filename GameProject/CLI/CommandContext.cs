using GameProject.Abstractions;
using GameProject.Gameplay;

namespace GameProject.CLI;

public sealed class CommandContext(ICharacterStore characters, ITextWorkspace text, GameSessionService? game = null)
{
    public ICharacterStore Characters { get; } = characters;
    public ITextWorkspace Text { get; } = text;
    public GameSessionService Game { get; } = game ?? new GameSessionService();
}

public static class CommandLineParser
{
    public static List<string> Parse(string line)
    {
        var result = new List<string>(); var current = new System.Text.StringBuilder();
        char quote = '\0'; bool escaped = false; bool started = false;
        foreach (var ch in line)
        {
            if (escaped) { current.Append(ch); escaped = false; started = true; continue; }
            if (ch == '\\' && quote != '\'') { escaped = true; continue; }
            if (quote != '\0') { if (ch == quote) quote = '\0'; else current.Append(ch); started = true; continue; }
            if (ch is '\'' or '"') { quote = ch; started = true; continue; }
            if (char.IsWhiteSpace(ch)) { if (started) { result.Add(current.ToString()); current.Clear(); started = false; } continue; }
            current.Append(ch); started = true;
        }
        if (quote != '\0') throw new FormatException("Unclosed quote in command.");
        if (escaped) current.Append('\\');
        if (started) result.Add(current.ToString());
        return result;
    }
}

internal static class CommandArgs
{
    public static string? Option(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length) throw new ArgumentException($"Option {name} needs a value.");
                return args[i + 1];
            }
        }
        return null;
    }
    public static bool Has(string[] args, string name) => args.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
    public static string Required(string[] args, int index, string usage)
    {
        if (index >= args.Length) throw new ArgumentException($"Usage: {usage}");
        return args[index];
    }
}
