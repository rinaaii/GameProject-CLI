namespace GameProject.Characters;

public enum AbilityEffect { Damage, Heal }

public sealed class Ability(string name, AbilityEffect effect, int power)
{
    public string Name { get; } = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Ability name is required.") : name;
    public AbilityEffect Effect { get; } = effect;
    public int Power { get; } = power < 0 ? throw new ArgumentOutOfRangeException(nameof(power)) : power;

    public void Execute(Character actor, Character target)
    {
        if (Effect == AbilityEffect.Heal) target.Heal(Power);
        else target.TakeDamage(Power);
        Console.WriteLine($"{actor.Name} uses {Name} on {target.Name} ({Effect}, power {Power}).");
    }
}
