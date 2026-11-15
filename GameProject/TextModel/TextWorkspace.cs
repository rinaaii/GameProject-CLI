using GameProject.Abstractions;

namespace GameProject.TextModel;

public sealed class TextWorkspace : ITextWorkspace
{
    private int _nextId = 1;
    public TextNode Root { get; } = new("root", "document", TextNodeKind.Root);
    public TextNode Current { get; private set; }
    public string CurrentPath
    {
        get
        {
            var parts = new Stack<string>(); var node = Current;
            while (node != Root) { parts.Push(node.Name); node = node.Parent ?? Root; }
            return "/" + string.Join("/", parts);
        }
    }
    public TextWorkspace() => Current = Root;
    public string NewId() => $"text{_nextId++}";
    public TextNode Add(TextNodeKind kind, string name, string content = "")
    {
        if (!Current.IsContainer) throw new InvalidOperationException("Current element is not a container.");
        var node = new TextNode(NewId(), name, kind, content); Current.Add(node); return node;
    }
    public void MoveUp()
    {
        if (Current.Parent is null) { Console.WriteLine("Already at document root."); return; }
        Current = Current.Parent; Console.WriteLine(CurrentPath);
    }
    public void ChangeDirectory(string path)
    {
        var target = path.StartsWith('/') ? Root : Current;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment == ".") continue;
            if (segment == "..") { target = target.Parent ?? Root; continue; }
            target = target.Children.FirstOrDefault(c => c.Name.Equals(segment, StringComparison.OrdinalIgnoreCase))
                ?? throw new KeyNotFoundException($"Path segment '{segment}' not found.");
        }
        if (!target.IsContainer) throw new InvalidOperationException("Can only navigate to a container.");
        Current = target; Console.WriteLine(CurrentPath);
    }
    public void ChangeDirectoryById(string id)
    {
        var target = FindById(Root, id) ?? throw new KeyNotFoundException($"Container id '{id}' not found.");
        if (!target.IsContainer) throw new InvalidOperationException("The selected element is not a container.");
        Current = target; Console.WriteLine(CurrentPath);
    }
    public TextNode? FindById(TextNode node, string id)
    {
        if (node.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) return node;
        foreach (var child in node.Children) { var found = FindById(child, id); if (found is not null) return found; }
        return null;
    }
    public void PrintCurrent(bool includeIds = false)
    {
        if (Current == Root) Console.WriteLine(Root.Render(includeIds));
        else Console.WriteLine(Current.Render(includeIds));
    }
    public void PrintWhole(bool includeIds = false) => Console.WriteLine(Root.Render(includeIds));
    public void RemoveCurrent()
    {
        if (Current == Root) throw new InvalidOperationException("Cannot remove document root.");
        var parent = Current.Parent!; parent.Remove(Current); Current = parent; Console.WriteLine($"Removed element. Current path: {CurrentPath}");
    }
    public void RemoveChild(string name)
    {
        var child = Current.Children.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Element '{name}' not found in current container.");
        if (Confirm($"Delete '{child.Name}' [{child.Id}]?")) { Current.Remove(child); Console.WriteLine("Removed."); } else Console.WriteLine("Cancelled.");
    }
    public static bool Confirm(string prompt)
    {
        Console.Write($"{prompt} [y/N] "); return Console.ReadLine()?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true;
    }
}
