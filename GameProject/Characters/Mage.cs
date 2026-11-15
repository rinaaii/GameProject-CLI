using System;

namespace GameProject.Characters;

public class Mage : Character
{
    public Mage(string name) : base(name, 80, 5, 35) { }

    public override Character CreateFreshCopy()
    {
        var copy = new Mage(Name);
        if (EquippedItem is { } item) copy.EquipItem(item);
        return copy;
    }

    public override void UseAbility(Character target)
    {
        int damage = TotalAttack;
        Console.WriteLine($"{Name} uses Soul Drain on {target.Name} for {damage} damage.");
        target.TakeDamage(damage);
        Heal(damage / 2);
    }
}
