using System;
using GameProject.Characters;
using GameProject.Items;
using GameProject.StructuredText;

namespace GameProject;

public class Game
{
    public void Start()
    {
        Console.WriteLine("=== GAME START ===");

        Character arthur = new Warrior("Arthur the Knight");
        Character merlin = new Mage("Merlin the Wizard");

        IItem sword = new Item("Knight's Sword", 0, 10);
        IItem armor = new Item("Heavy Armor", 15, 0);
        IItem staff = new Item("Wizard Staff", 0, 8);

        arthur.EquipItem(sword);
        arthur.EquipItem(armor);
        merlin.EquipItem(staff);

        arthur.Attack(merlin);
        merlin.UseAbility(arthur);

        var report = new StructuredTextBuilder()
            .AddHeading("Battle Report")
            .AddParagraph("The battle between Arthur and Merlin has finished.")
            .StartBulletList()
            .AddListItem($"Arthur HP: {arthur.Health}/{arthur.MaxHealth}")
            .AddListItem($"Merlin HP: {merlin.Health}/{merlin.MaxHealth}")
            .Build();

        Console.WriteLine();
        Console.WriteLine(report.Render());
        Console.WriteLine("=== GAME END ===");
    }
}
