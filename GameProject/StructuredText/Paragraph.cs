namespace GameProject.StructuredText;

/// <summary>
/// Звичайний абзац структурованого тексту.
/// </summary>
public sealed class Paragraph : StructuredTextElement
{
    public string Text { get; }

    public Paragraph(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Paragraph text cannot be empty.", nameof(text));

        Text = text;
    }

    public override string Render() => Text;
}
