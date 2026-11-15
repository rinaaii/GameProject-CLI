namespace GameProject.StructuredText;

/// <summary>
/// Єдина точка входу для створення структурованого тексту.
/// Інкапсулює порядок створення елементів та приховує деталі композиції документа.
/// </summary>
public sealed class StructuredTextBuilder
{
    private readonly List<StructuredTextElement> _elements = new();
    private BulletList? _currentList;

    public StructuredTextBuilder AddHeading(string text, int level = 1)
    {
        CloseCurrentList();
        _elements.Add(new Heading(text, level));
        return this;
    }

    public StructuredTextBuilder AddParagraph(string text)
    {
        CloseCurrentList();
        _elements.Add(new Paragraph(text));
        return this;
    }

    public StructuredTextBuilder StartBulletList()
    {
        CloseCurrentList();
        _currentList = new BulletList();
        _elements.Add(_currentList);
        return this;
    }

    public StructuredTextBuilder AddListItem(string text)
    {
        if (_currentList is null)
            throw new InvalidOperationException("Start a bullet list before adding list items.");

        _currentList.AddItem(text);
        return this;
    }

    public StructuredTextDocument Build()
    {
        CloseCurrentList();
        return new StructuredTextDocument(_elements);
    }

    private void CloseCurrentList() => _currentList = null;
}
