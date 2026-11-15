using GameProject.Items;
using GameProject.Abstractions;

namespace GameProject.Characters;

public sealed class CharacterRecord(string id, Character character)
{
    public string Id { get; } = id;
    public Character Character { get; } = character;
    public Dictionary<string, Ability> Abilities { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<IItem> Items { get; } = new();
}

public sealed record ItemRecord(string Id, IItem Item);
public sealed record AbilityRecord(string Id, Ability Ability);

public sealed class CharacterStore : ICharacterStore
{
    private readonly Dictionary<string, CharacterRecord> _characters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ItemRecord> _items = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AbilityRecord> _abilities = new(StringComparer.OrdinalIgnoreCase);
    private int _nextId = 1;
    public IEnumerable<CharacterRecord> Characters => _characters.Values;
    public IEnumerable<ItemRecord> Items => _items.Values;
    public IEnumerable<AbilityRecord> Abilities => _abilities.Values;

    public string NewId(string prefix) => $"{prefix}{_nextId++}";
    public CharacterRecord AddCharacter(string? id, Character character)
    {
        id = string.IsNullOrWhiteSpace(id) ? NewId("char") : id;
        if (_characters.ContainsKey(id)) throw new InvalidOperationException($"Character id '{id}' is already used.");
        var record = new CharacterRecord(id, character); _characters.Add(id, record); return record;
    }
    public ItemRecord AddItem(string? id, IItem item)
    {
        id = string.IsNullOrWhiteSpace(id) ? NewId("item") : id;
        if (_items.ContainsKey(id)) throw new InvalidOperationException($"Item id '{id}' is already used.");
        var record = new ItemRecord(id, item); _items.Add(id, record); return record;
    }
    public AbilityRecord AddAbility(string? id, Ability ability)
    {
        id = string.IsNullOrWhiteSpace(id) ? NewId("ability") : id;
        if (_abilities.ContainsKey(id)) throw new InvalidOperationException($"Ability id '{id}' is already used.");
        var record = new AbilityRecord(id, ability); _abilities.Add(id, record); return record;
    }
    public CharacterRecord GetCharacter(string key) => _characters.TryGetValue(key, out var byId) ? byId : _characters.Values.FirstOrDefault(x => x.Character.Name.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? throw new KeyNotFoundException($"Character '{key}' was not found.");
    public ItemRecord GetItem(string key) => _items.TryGetValue(key, out var byId) ? byId : _items.Values.FirstOrDefault(x => x.Item.Name.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? throw new KeyNotFoundException($"Item '{key}' was not found.");
    public AbilityRecord GetAbility(string key) => _abilities.TryGetValue(key, out var byId) ? byId : _abilities.Values.FirstOrDefault(x => x.Ability.Name.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? throw new KeyNotFoundException($"Ability '{key}' was not found.");
    public bool TryGetCharacter(string key, out CharacterRecord record)
    {
        try { record = GetCharacter(key); return true; } catch (KeyNotFoundException) { record = null!; return false; }
    }
}
