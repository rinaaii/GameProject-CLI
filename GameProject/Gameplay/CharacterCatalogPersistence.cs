using System.Text.Json;
using GameProject.Abstractions;
using GameProject.Characters;
using GameProject.Infrastructure;
using GameProject.Items;

namespace GameProject.Gameplay;

/// <summary>Persists locally created and API-imported characters as JSON.</summary>
public static class CharacterCatalogPersistence
{
    private sealed record SavedItem(string Name, int ArmorBonus, int AttackBonus);
    private sealed record SavedCharacter(string Id, string Name, string Type, RemoteCharacter? Remote = null, SavedItem? EquippedItem = null);
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "data", "characters.json");

    public static void LoadInto(ICharacterStore store)
    {
        if (!File.Exists(FilePath)) return;
        var entries = JsonSerializer.Deserialize<List<SavedCharacter>>(File.ReadAllText(FilePath)) ?? new();
        foreach (var item in entries)
        {
            if (store.TryGetCharacter(item.Id, out _)) continue;
            Character character = item.Type.ToLowerInvariant() switch
            {
                "mage" => new Mage(item.Name),
                "genshin" when item.Remote is not null => new GenshinCharacter(item.Remote),
                _ => new Warrior(item.Name)
            };
            if (item.EquippedItem is not null)
                character.EquipItem(new Item(item.EquippedItem.Name, item.EquippedItem.ArmorBonus, item.EquippedItem.AttackBonus));
            store.AddCharacter(item.Id, character);
        }
    }

    public static void SaveUserCharacters(ICharacterStore store)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var entries = store.Characters
            .Where(x => x.Character is Warrior or Mage or GenshinCharacter)
            .Select(x => new SavedCharacter(
                x.Id,
                x.Character.Name,
                x.Character switch { Mage => "mage", GenshinCharacter => "genshin", _ => "warrior" },
                x.Character is GenshinCharacter g
                    ? new RemoteCharacter(g.Name, g.Description, g.Rarity, g.Element, g.WeaponType, g.RemoteAbilities)
                    : null,
                x.Character.EquippedItem is { } equipped
                    ? new SavedItem(equipped.Name, equipped.ArmorBonus, equipped.AttackBonus)
                    : null))
            .ToList();
        File.WriteAllText(FilePath, JsonSerializer.Serialize(entries, Options));
    }
}
