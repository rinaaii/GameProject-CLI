using GameProject.Characters;
using GameProject.Items;

namespace GameProject.CLI;

internal static class CharacterCommands
{
    public static void Create(string[] args, CommandContext context)
    {
        var kind = CommandArgs.Required(args, 0, "create <char|item|ability>").ToLowerInvariant();
        switch (kind)
        {
            case "char": case "character":
            {
                var name = Ask("Character name"); var type = Ask("Character type (warrior/mage)").ToLowerInvariant();
                Character character = type switch { "warrior" => new Warrior(name), "mage" => new Mage(name), _ => throw new ArgumentException("Type must be warrior or mage.") };
                var requestedId = AskOptional("Identifier (leave blank for automatic)");
                var record = context.Characters.AddCharacter(requestedId, character);
                GameProject.Gameplay.CharacterCatalogPersistence.SaveUserCharacters(context.Characters);
                Console.WriteLine($"Created character [{record.Id}] {record.Character.Name} ({type})."); break;
            }
            case "item":
            {
                var name = Ask("Item name"); var armor = AskInt("Armor bonus"); var attack = AskInt("Attack bonus");
                var id = context.Characters.AddItem(AskOptional("Identifier (leave blank for automatic)"), new Item(name, armor, attack));
                Console.WriteLine($"Created item [{id.Id}] {id.Item.Name}."); break;
            }
            case "ability":
            {
                var name = Ask("Ability name"); var effectText = Ask("Effect (damage/heal)").ToLowerInvariant();
                var effect = effectText switch { "damage" => AbilityEffect.Damage, "heal" => AbilityEffect.Heal, _ => throw new ArgumentException("Effect must be damage or heal.") };
                var power = AskInt("Power"); var id = context.Characters.AddAbility(AskOptional("Identifier (leave blank for automatic)"), new Ability(name, effect, power));
                Console.WriteLine($"Created ability [{id.Id}] {id.Ability.Name}."); break;
            }
            default: throw new ArgumentException("Use create char, create item, or create ability.");
        }
    }

    public static void Add(string[] args, CommandContext context)
    {
        if (args.Length == 0) throw new ArgumentException("Usage: add <item|ability> <id/name> --char_id <id/name> [--id <ability-id>]");
        var charId = CommandArgs.Option(args, "--char_id") ?? throw new ArgumentException("Required option: --char_id <id/name>");
        var kind = args[0].ToLowerInvariant();
        var key = args.Length > 1 && !args[1].StartsWith("--", StringComparison.Ordinal) ? args[1] : CommandArgs.Option(args, "--object_id") ?? throw new ArgumentException("Specify the item/ability id or name.");
        var character = context.Characters.GetCharacter(charId);
        if (kind == "item")
        {
            var item = context.Characters.GetItem(key); character.Items.Add(item.Item); character.Character.EquipItem(item.Item);
            Console.WriteLine($"Added item [{item.Id}] to character [{character.Id}].");
        }
        else if (kind == "ability")
        {
            var ability = context.Characters.GetAbility(key); var chosenId = CommandArgs.Option(args, "--id") ?? ability.Id;
            if (character.Abilities.ContainsKey(chosenId)) throw new InvalidOperationException($"Character already has ability id '{chosenId}'.");
            character.Abilities.Add(chosenId, ability.Ability);
            Console.WriteLine($"Added ability [{ability.Id}] as [{chosenId}] to character [{character.Id}].");
        }
        else throw new ArgumentException("Use add item or add ability.");
    }

    public static void Act(string[] args, CommandContext context)
    {
        var action = CommandArgs.Required(args, 0, "act <attack|heal|ability> <actor> <target> [--id <ability-id>]").ToLowerInvariant();
        var actorKey = CommandArgs.Required(args, 1, "act <attack|heal|ability> <actor> <target>");
        var targetKey = CommandArgs.Required(args, 2, "act <attack|heal|ability> <actor> <target>");
        var actor = context.Characters.GetCharacter(actorKey); var target = context.Characters.GetCharacter(targetKey);
        switch (action)
        {
            case "attack": actor.Character.Attack(target.Character); break;
            case "heal": target.Character.Heal(AskInt("Healing amount")); break;
            case "ability":
                var id = CommandArgs.Option(args, "--id") ?? throw new ArgumentException("Use --id <ability-id> for an ability action.");
                if (!actor.Abilities.TryGetValue(id, out var ability)) throw new KeyNotFoundException($"Character '{actor.Id}' has no ability '{id}'.");
                ability.Execute(actor.Character, target.Character); break;
            default: throw new ArgumentException("Action must be attack, heal, or ability.");
        }
    }

    public static void List(string[] args, CommandContext context)
    {
        var kind = CommandArgs.Required(args, 0, "ls <char|item|ability> [--id <id/name>]").ToLowerInvariant();
        var key = CommandArgs.Option(args, "--id");
        switch (kind)
        {
            case "char": case "character":
                IEnumerable<CharacterRecord> chars = key is null ? context.Characters.Characters : new[] { context.Characters.GetCharacter(key) };
                foreach (var c in chars) Console.WriteLine(key is null ? $"[{c.Id}] {c.Character.Name} HP {c.Character.Health}/{c.Character.MaxHealth}; items: {string.Join(", ", c.Items.Select(i => i.Name).DefaultIfEmpty("none"))}; abilities: {string.Join(", ", c.Abilities.Select(a => $"{a.Key}:{a.Value.Name}").DefaultIfEmpty("none"))}" : $"[{c.Id}] {c.Character.Name}; HP {c.Character.Health}/{c.Character.MaxHealth}; attack {c.Character.TotalAttack}; armor {c.Character.TotalArmor}; items: {string.Join(", ", c.Items.Select(i => i.Name))}; abilities: {string.Join(", ", c.Abilities.Select(a => $"{a.Key}:{a.Value.Name} ({a.Value.Effect}, {a.Value.Power})"))}");
                break;
            case "item":
                IEnumerable<ItemRecord> items = key is null ? context.Characters.Items : new[] { context.Characters.GetItem(key) };
                foreach (var i in items) { var users = context.Characters.Characters.Where(c => c.Items.Contains(i.Item)).Select(c => c.Character.Name); Console.WriteLine(key is null ? $"[{i.Id}] {i.Item.Name} (users: {string.Join(", ", users.DefaultIfEmpty("none"))})" : $"[{i.Id}] {i.Item.Name}; attack +{i.Item.AttackBonus}; armor +{i.Item.ArmorBonus}; users: {string.Join(", ", users.DefaultIfEmpty("none"))}"); }
                break;
            case "ability":
                IEnumerable<AbilityRecord> abilities = key is null ? context.Characters.Abilities : new[] { context.Characters.GetAbility(key) };
                foreach (var a in abilities) { var users = context.Characters.Characters.Where(c => c.Abilities.Values.Contains(a.Ability)).Select(c => c.Character.Name); Console.WriteLine(key is null ? $"[{a.Id}] {a.Ability.Name} ({a.Ability.Effect}, {a.Ability.Power}; users: {string.Join(", ", users.DefaultIfEmpty("none"))})" : $"[{a.Id}] {a.Ability.Name}; effect {a.Ability.Effect}; power {a.Ability.Power}; users: {string.Join(", ", users.DefaultIfEmpty("none"))}"); }
                break;
            default: throw new ArgumentException("Use ls char, ls item, or ls ability.");
        }
    }

    public static void Show(string[] args, CommandContext context)
    {
        var key = CommandArgs.Required(args, 0, "show <character-id/name>");
        var record = context.Characters.GetCharacter(key); var c = record.Character;
        Console.WriteLine($"Character: {c.Name} [{record.Id}]");
        Console.WriteLine($"Type: {(c is Mage ? "Mage" : c is Warrior ? "Warrior" : c.GetType().Name)}");
        Console.WriteLine($"Health: {c.Health}/{c.MaxHealth}; attack: {c.TotalAttack}; armor: {c.TotalArmor}");
        if (c is GameProject.Characters.GenshinCharacter g)
        {
            Console.WriteLine($"Element: {g.Element}; weapon: {g.WeaponType}; rarity: {g.Rarity}★");
            Console.WriteLine($"Description: {g.Description}");
            Console.WriteLine($"Graphic: https://genshin.jmp.blue/characters/{g.Name.ToLowerInvariant().Replace(" ", "-")}/icon");
        }
        else Console.WriteLine("Graphic: not supplied for this locally created character.");
        Console.WriteLine($"Items: {string.Join(", ", record.Items.Select(x => x.Name).DefaultIfEmpty("none"))}");
        Console.WriteLine($"Abilities: {string.Join(", ", record.Abilities.Select(x => x.Value.Name).DefaultIfEmpty("none"))}");
    }

    private static string Ask(string prompt) { Console.Write($"{prompt}: "); return Console.ReadLine() is { Length: > 0 } value ? value.Trim() : throw new ArgumentException($"{prompt} cannot be empty."); }
    private static string? AskOptional(string prompt) { Console.Write($"{prompt}: "); return Console.ReadLine()?.Trim() is { Length: > 0 } value ? value : null; }
    private static int AskInt(string prompt) { while (true) { var value = Ask(prompt); if (int.TryParse(value, out var number) && number >= 0) return number; Console.WriteLine("Enter a non-negative integer."); } }
}
