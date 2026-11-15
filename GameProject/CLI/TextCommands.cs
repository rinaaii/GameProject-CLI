using GameProject.TextModel;
using GameProject.Abstractions;

namespace GameProject.CLI;

internal static class TextCommands
{
    public static void Print(string[] args, CommandContext context)
    {
        var ids = CommandArgs.Has(args, "--id");
        if (CommandArgs.Has(args, "--whole")) context.Text.PrintWhole(ids);
        else context.Text.PrintCurrent(ids);
    }

    public static void Add(string[] args, CommandContext context)
    {
        var category = CommandArgs.Required(args, 0, "add <container|leaf> <heading|paragraph|list>").ToLowerInvariant();
        var type = CommandArgs.Required(args, 1, "add <container|leaf> <heading|paragraph|list>").ToLowerInvariant();
        var isContainer = category == "container";
        if (!isContainer && category != "leaf") throw new ArgumentException("First argument must be container or leaf.");
        TextNodeKind kind = type switch
        {
            "heading" when isContainer => TextNodeKind.Heading,
            "list" when isContainer => TextNodeKind.BulletList,
            "paragraph" when !isContainer => TextNodeKind.Paragraph,
            _ => throw new ArgumentException("Supported combinations: add container heading, add container list, add leaf paragraph.")
        };
        Console.Write("Element name: "); var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Element name cannot be empty.");
        var content = "";
        if (kind == TextNodeKind.Paragraph) { Console.Write("Paragraph text: "); content = Console.ReadLine() ?? ""; if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Paragraph text cannot be empty."); }
        var node = context.Text.Add(kind, name, content);
        Console.WriteLine($"Created {kind} [{node.Id}] '{node.Name}' in {context.Text.CurrentPath}.");
    }

    public static void Remove(string[] args, CommandContext context)
    {
        if (context.Text.Current == context.Text.Root) throw new InvalidOperationException("Cannot remove document root.");
        if (args.Length > 0)
        {
            var name = args[0];
            if (context.Text.Current.IsContainer) context.Text.RemoveChild(name);
            else throw new InvalidOperationException("Current element has no children; use rm without a name to remove it.");
        }
        else if (Confirm($"Delete current element '{context.Text.Current.Name}' [{context.Text.Current.Id}]?")) context.Text.RemoveCurrent();
        else Console.WriteLine("Cancelled.");
    }

    private static bool Confirm(string prompt)
    {
        Console.Write($"{prompt} [y/N] ");
        return Console.ReadLine()?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true;
    }

    public static void ChangeDirectory(string[] args, CommandContext context)
    {
        var id = CommandArgs.Option(args, "--id");
        if (id is not null) { context.Text.ChangeDirectoryById(id); return; }
        var path = CommandArgs.Required(args, 0, "cd <path> | cd --id <container-id>");
        context.Text.ChangeDirectory(path);
    }
}
