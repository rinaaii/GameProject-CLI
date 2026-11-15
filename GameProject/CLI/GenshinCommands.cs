using GameProject.Characters;
using GameProject.Infrastructure;
using GameProject.Items;
using GameProject.StructuredText;

namespace GameProject.CLI;

/// <summary>CLI adapter for browsing the remote catalogue and importing game objects.</summary>
internal sealed class GenshinCommands(GenshinApiClient api)
{
    private IReadOnlyList<string> _characters = Array.Empty<string>();
    private IReadOnlyList<string> _weapons = Array.Empty<string>();

    public void Register(CommandRegistry registry, CommandContext context)
    {
        registry.Add("api", "api <sync|chars|weapons|create <character>|equip <char-id> <weapon>|show <char-id>> — remote Genshin catalogue", (args, _) => Execute(args, context));
    }

    private void Execute(string[] args, CommandContext context)
    {
        var action = CommandArgs.Required(args, 0, "api sync|chars|weapons|create <character>|equip <char-id> <weapon>|show <char-id>").ToLowerInvariant();
        switch (action)
        {
            case "sync":
                _characters = api.GetCharacterNamesAsync().GetAwaiter().GetResult();
                _weapons = api.GetWeaponNamesAsync().GetAwaiter().GetResult();
                Console.WriteLine($"Remote catalogue loaded: {_characters.Count} characters, {_weapons.Count} weapons.");
                break;
            case "chars": EnsureLoaded(); foreach (var name in _characters) Console.WriteLine(name); break;
            case "weapons": EnsureLoaded(); foreach (var name in _weapons) Console.WriteLine(name); break;
            case "create":
            {
                EnsureLoaded(); var requested = string.Join(" ", args.Skip(1));
                if (string.IsNullOrWhiteSpace(requested)) throw new ArgumentException("Usage: api create <character name>");
                var name = Resolve(_characters, requested);
                var remote = api.GetCharacterAsync(name).GetAwaiter().GetResult();
                var character = new GenshinCharacter(remote);
                var record = context.Characters.AddCharacter(null, character);
                GameProject.Gameplay.CharacterCatalogPersistence.SaveUserCharacters(context.Characters);
                Console.WriteLine($"Imported [{record.Id}] {remote.Name} ({remote.Element}, {remote.Rarity}★).");
                Console.WriteLine(BuildCharacterDocument(character).Render());
                break;
            }
            case "equip":
            {
                EnsureLoaded(); if (args.Length < 3) throw new ArgumentException("Usage: api equip <character-id/name> <weapon name>");
                var character = context.Characters.GetCharacter(args[1]);
                var weaponName = Resolve(_weapons, string.Join(" ", args.Skip(2)));
                var remote = api.GetWeaponAsync(weaponName).GetAwaiter().GetResult();
                // API base attack is converted into an equipment bonus relative to a neutral baseline.
                var item = new Item(remote.Name, 0, Math.Max(0, remote.BaseAttack - 40) + Math.Max(0, remote.Rarity - 1) * 2);
                var itemRecord = context.Characters.AddItem($"genshin-{Slug(remote.Name)}", item);
                character.Items.Add(item); character.Character.EquipItem(item);
                GameProject.Gameplay.CharacterCatalogPersistence.SaveUserCharacters(context.Characters);
                Console.WriteLine($"Imported and equipped [{itemRecord.Id}] {remote.Name} (base ATK {remote.BaseAttack}, rarity {remote.Rarity}★).");
                break;
            }
            case "show":
            {
                var record = context.Characters.GetCharacter(CommandArgs.Required(args, 1, "api show <character-id/name>"));
                if (record.Character is not GenshinCharacter genshin) throw new ArgumentException("This character was not imported from the remote Genshin API.");
                Console.WriteLine(BuildCharacterDocument(genshin, record.Character).Render());
                break;
            }
            default: throw new ArgumentException("Use api sync, api chars, api weapons, api create, api equip, or api show.");
        }
    }

    private void EnsureLoaded() { if (_characters.Count == 0 || _weapons.Count == 0) throw new InvalidOperationException("Run 'api sync' first to download the remote catalogue."); }
    private static string Resolve(IReadOnlyList<string> values, string input) => values.FirstOrDefault(x => x.Equals(input, StringComparison.OrdinalIgnoreCase) || x.Replace('-', ' ').Equals(input, StringComparison.OrdinalIgnoreCase)) ?? throw new KeyNotFoundException($"'{input}' was not found in the remote catalogue.");
    private static string Slug(string value) => value.ToLowerInvariant().Replace(' ', '-').Replace("'", "");

    private static StructuredTextDocument BuildCharacterDocument(GenshinCharacter c, Character? state = null)
    {
        var current = state ?? c;
        var builder = new StructuredTextBuilder().AddHeading(current.Name)
            .AddParagraph(c.Description)
            .AddParagraph($"Element: {c.Element} | Weapon: {c.WeaponType} | Rarity: {c.Rarity}★")
            .AddHeading("Character state", 2)
            .StartBulletList()
            .AddListItem($"HP: {current.Health}/{current.MaxHealth}")
            .AddListItem($"Attack: {current.TotalAttack}")
            .AddListItem($"Armor: {current.TotalArmor}");
        builder.AddHeading("Abilities", 2).StartBulletList();
        if (c.RemoteAbilities.Count == 0) builder.AddListItem("No ability descriptions were supplied by the API.");
        else foreach (var a in c.RemoteAbilities) builder.AddListItem($"{a.Name}: {a.Description}");
        return builder.Build();
    }
}
