using System;
using GameProject.Items;

namespace GameProject.Characters;

public abstract class Character
{
    private readonly string _name;
    private readonly int _maxHealth;
    private readonly int _baseArmor;
    private readonly int _baseAttack;
    private int _health;
    private IItem? _equippedItem;

    public string Name => _name;
    public int Health => _health;
    public int MaxHealth => _maxHealth;
    public int TotalAttack => _baseAttack + (_equippedItem?.AttackBonus ?? 0);
    public int TotalArmor => _baseArmor + (_equippedItem?.ArmorBonus ?? 0);

    public Character(string name, int maxHealth, int baseArmor, int baseAttack)
    {
        _name = name;
        _maxHealth = maxHealth;
        _health = maxHealth;
        _baseArmor = baseArmor;
        _baseAttack = baseAttack;
    }

    public void EquipItem(IItem item)
    {
        _equippedItem = item;
        Console.WriteLine($"{Name} equipped {item.Name}.");
    }

    public void Attack(Character target)
    {
        Console.WriteLine($"{Name} attacks {target.Name} for {TotalAttack} damage.");
        target.TakeDamage(TotalAttack);
    }

    public void TakeDamage(int damage)
    {
        int actualDamage = Math.Max(0, damage - TotalArmor);
        _health = Math.Max(0, _health - actualDamage);
        Console.WriteLine($"{Name} takes {actualDamage} damage. HP: {_health}/{_maxHealth}");
    }

    public void TakeDamageIgnoringArmor(int damage)
    {
        _health = Math.Max(0, _health - Math.Max(0, damage));
        Console.WriteLine($"{Name} takes {damage} damage. HP: {_health}/{_maxHealth}");
    }

    /// <summary>Restores a previously saved health value when loading a game session.</summary>
    public void RestoreHealth(int health)
    {
        if (health < 0 || health > _maxHealth) throw new ArgumentOutOfRangeException(nameof(health));
        _health = health;
    }

    public void Heal(int amount)
    {
        _health = Math.Min(_maxHealth, _health + amount);
        Console.WriteLine($"{Name} heals for {amount}. HP: {_health}/{_maxHealth}");
    }

    public abstract void UseAbility(Character target);
}
