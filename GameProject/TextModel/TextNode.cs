namespace GameProject.TextModel;

public enum TextNodeKind { Root, Heading, Paragraph, BulletList }

public sealed class TextNode
{
    public string Id { get; }
    public string Name { get; set; }
    public TextNodeKind Kind { get; }
    public string Content { get; set; }
    public TextNode? Parent { get; private set; }
    public List<TextNode> Children { get; } = new();
    public bool IsContainer => Kind is TextNodeKind.Root or TextNodeKind.Heading or TextNodeKind.BulletList;

    public TextNode(string id, string name, TextNodeKind kind, string content = "")
    { Id = id; Name = name; Kind = kind; Content = content; }

    public void Add(TextNode child)
    {
        if (!IsContainer) throw new InvalidOperationException("A leaf cannot contain children.");
        if (Children.Any(c => c.Name.Equals(child.Name, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException($"An element named '{child.Name}' already exists here.");
        child.Parent = this; Children.Add(child);
    }
    public bool Remove(TextNode child) { if (!Children.Remove(child)) return false; child.Parent = null; return true; }
    public string Render(bool includeIds = false)
    {
        var label = includeIds ? $" [id:{Id}]" : "";
        var own = Kind switch
        {
            TextNodeKind.Root => "",
            TextNodeKind.Heading => $"# {Name}{label}",
            TextNodeKind.Paragraph => (includeIds ? $"[id:{Id}] " : "") + Content,
            TextNodeKind.BulletList => $"{Name}{label}",
            _ => Content
        };
        var descendants = Children.Select(c => c.Kind == TextNodeKind.Paragraph ? "- " + c.Render(includeIds) : c.Render(includeIds));
        return string.Join(Environment.NewLine, new[] { own }.Where(s => !string.IsNullOrEmpty(s)).Concat(descendants));
    }
}
