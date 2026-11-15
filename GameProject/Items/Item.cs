namespace GameProject.Items;

public class Item : IItem
{
    public string Name { get; }
    public int ArmorBonus { get; }
    public int AttackBonus { get; }

    public Item(string name, int armorBonus, int attackBonus)
    {
        Name = name;
        ArmorBonus = armorBonus;
        AttackBonus = attackBonus;
    }
}
