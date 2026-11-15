namespace GameProject.Items;

public interface IItem
{
    string Name { get; }
    int ArmorBonus { get; }
    int AttackBonus { get; }
}
