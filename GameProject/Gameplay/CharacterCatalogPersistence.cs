using System.Text.Json;
using GameProject.Abstractions;
using GameProject.Characters;

namespace GameProject.Gameplay;

/// <summary>Persists user-created Warrior and Mage entries as JSON in the application data folder.</summary>
public static class CharacterCatalogPersistence
{
    private sealed record SavedCharacter(string Id, string Name, string Type);
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "data", "characters.json");

    public static void LoadInto(ICharacterStore store)
    {
        if (!File.Exists(FilePath)) return;
        var entries = JsonSerializer.Deserialize<List<SavedCharacter>>(File.ReadAllText(FilePath)) ?? new();
        foreach (var item in entries)
        {
            if (store.TryGetCharacter(item.Id, out _)) continue;
            Character character = item.Type.Equals("mage", StringComparison.OrdinalIgnoreCase) ? new Mage(item.Name) : new Warrior(item.Name);
            store.AddCharacter(item.Id, character);
        }
    }

    public static void SaveUserCharacters(ICharacterStore store)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var entries = store.Characters.Where(x => x.Character is Warrior or Mage)
            .Select(x => new SavedCharacter(x.Id, x.Character.Name, x.Character is Mage ? "mage" : "warrior")).ToList();
        File.WriteAllText(FilePath, JsonSerializer.Serialize(entries, Options));
    }
}
