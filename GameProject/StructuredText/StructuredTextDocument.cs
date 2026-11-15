namespace GameProject.StructuredText;

/// <summary>
/// Документ — композиція структурованих елементів.
/// </summary>
public sealed class StructuredTextDocument
{
    private readonly IReadOnlyList<StructuredTextElement> _elements;

    internal StructuredTextDocument(IEnumerable<StructuredTextElement> elements)
    {
        _elements = elements.ToList().AsReadOnly();
    }

    public IReadOnlyList<StructuredTextElement> Elements => _elements;

    public string Render() => string.Join(Environment.NewLine + Environment.NewLine, _elements.Select(e => e.Render()));
}
