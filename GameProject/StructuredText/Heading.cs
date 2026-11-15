namespace GameProject.StructuredText;

/// <summary>
/// Заголовок документа.
/// </summary>
public sealed class Heading : StructuredTextElement
{
    public string Text { get; }
    public int Level { get; }

    public Heading(string text, int level = 1)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Heading text cannot be empty.", nameof(text));

        if (level is < 1 or > 6)
            throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be from 1 to 6.");

        Text = text;
        Level = level;
    }

    public override string Render() => $"{new string('#', Level)} {Text}";
}
