namespace GameProject.StructuredText;

/// <summary>
/// Маркований список. Його елементи також є StructuredTextElement,
/// тому документ працює з усіма елементами через єдиний абстрактний тип.
/// </summary>
public sealed class BulletList : StructuredTextElement
{
    private readonly List<string> _items = new();

    public IReadOnlyList<string> Items => _items;

    public void AddItem(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("List item cannot be empty.", nameof(text));

        _items.Add(text);
    }

    public override string Render() => string.Join(Environment.NewLine, _items.Select(item => $"- {item}"));
}
