using GameProject.Items;
using GameProject.Characters;

namespace GameProject.Abstractions;

/// <summary>
/// Contract for character/item/ability persistence. CLI and application logic
/// depend on this abstraction instead of a concrete in-memory implementation.
/// </summary>
public interface ICharacterStore
{
    IEnumerable<CharacterRecord> Characters { get; }
    IEnumerable<ItemRecord> Items { get; }
    IEnumerable<AbilityRecord> Abilities { get; }

    CharacterRecord AddCharacter(string? id, Character character);
    ItemRecord AddItem(string? id, IItem item);
    AbilityRecord AddAbility(string? id, Ability ability);
    CharacterRecord GetCharacter(string key);
    ItemRecord GetItem(string key);
    AbilityRecord GetAbility(string key);
    bool TryGetCharacter(string key, out CharacterRecord record);
}
