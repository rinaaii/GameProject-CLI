using GameProject.Infrastructure;
namespace GameProject.Characters;

/// <summary>Internal game character mapped from a remote Genshin character record.</summary>
public sealed class GenshinCharacter : Character
{
    public string Description { get; }
    public string Element { get; }
    public string WeaponType { get; }
    public int Rarity { get; }
    public IReadOnlyList<RemoteAbility> RemoteAbilities { get; }
    public GenshinCharacter(RemoteCharacter source) : base(source.Name,70+Math.Clamp(source.Rarity,1,5)*10,Math.Clamp(source.Rarity,1,5)*2,20+Math.Clamp(source.Rarity,1,5)*5)
    { Description=source.Description; Element=source.Element; WeaponType=source.WeaponType; Rarity=Math.Clamp(source.Rarity,1,5); RemoteAbilities=source.Abilities; }
    public override void UseAbility(Character target)
    { var ability=RemoteAbilities.FirstOrDefault(); Console.WriteLine($"{Name} uses {ability?.Name??"Basic skill"} on {target.Name}."); target.TakeDamage(TotalAttack); }
}
