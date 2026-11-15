using System;

namespace GameProject.Characters;

public class Warrior : Character
{
    public Warrior(string name) : base(name, 120, 15, 25) { }

    public override Character CreateFreshCopy()
    {
        var copy = new Warrior(Name);
        if (EquippedItem is { } item) copy.EquipItem(item);
        return copy;
    }

    public override void UseAbility(Character target)
    {
        int damage = TotalAttack + 10;
        Console.WriteLine($"{Name} uses Crushing Blow on {target.Name} for {damage} damage.");
        target.TakeDamageIgnoringArmor(damage);
    }
}
