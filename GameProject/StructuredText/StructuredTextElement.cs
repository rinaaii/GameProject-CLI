namespace GameProject.StructuredText;

/// <summary>
/// Абстрактний елемент структурованого тексту.
/// Визначає спільний API для всіх типів елементів і використовує поліморфний Render().
/// </summary>
public abstract class StructuredTextElement
{
    public abstract string Render();
}
