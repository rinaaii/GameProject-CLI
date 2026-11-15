using System.Text.Json;
using GameProject.Characters;
using GameProject.Infrastructure;
using GameProject.Items;

namespace GameProject.Gameplay;

public sealed class GameSessionService
{
    private sealed record SessionSave(string PlayerName, string PlayerType, int PlayerHp, string EnemyName, string EnemyType, int EnemyHp, int Turn, string Status, string? Winner, List<string>? History = null, RemoteCharacter? PlayerRemote = null, RemoteCharacter? EnemyRemote = null, SavedItem? PlayerItem = null, SavedItem? EnemyItem = null);
    private sealed record GameResult(DateTimeOffset FinishedAt, string Player, string Opponent, string Outcome, int Turns);
    private sealed record SavedItem(string Name, int ArmorBonus, int AttackBonus);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly List<string> _history = new();
    private Character? _player;
    private Character? _enemy;

    public int Turn { get; private set; }
    public IReadOnlyList<string> History => _history.AsReadOnly();
    public bool IsActive => _player is not null && _enemy is not null && _player.Health > 0 && _enemy.Health > 0;
    public Character Player => _player ?? throw new InvalidOperationException("Start or load a game first.");
    public Character Enemy => _enemy ?? throw new InvalidOperationException("Start or load a game first.");
    public string SavePath => Path.Combine(AppContext.BaseDirectory, "data", "game-save.json");
    public string ResultsPath => Path.Combine(AppContext.BaseDirectory, "data", "game-results.json");

    public void Start(Character player, Character? enemy = null)
    {
        _player = CloneFresh(player);
        _enemy = enemy is null ? new Warrior("Training Dummy") : CloneFresh(enemy);
        Turn = 0;
        _history.Clear();
        AddHistory($"Battle started: {_player.Name} vs {_enemy.Name}.");
        Save();
    }

    public void PlayerAttack() => ExecutePlayerAction(useAbility: false);
    public void PlayerAbility() => ExecutePlayerAction(useAbility: true);

    private void ExecutePlayerAction(bool useAbility)
    {
        EnsureActive();
        Turn++;
        var player = Player;
        var enemy = Enemy;
        var enemyBefore = enemy.Health;
        if (useAbility) player.UseAbility(enemy); else player.Attack(enemy);
        AddHistory($"Turn {Turn}: {player.Name} {(useAbility ? "used a special ability on" : "attacked")} {enemy.Name}. {enemy.Name}: {enemyBefore} -> {enemy.Health} HP.");

        if (enemy.Health == 0)
        {
            Finish(player.Name);
            return;
        }

        var playerBefore = player.Health;
        enemy.Attack(player);
        AddHistory($"Turn {Turn}: {enemy.Name} counterattacked {player.Name}. {player.Name}: {playerBefore} -> {player.Health} HP.");
        if (player.Health == 0)
        {
            Finish(enemy.Name);
            return;
        }
        Save();
    }

    public void Save()
    {
        if (_player is null || _enemy is null) throw new InvalidOperationException("There is no game to save.");
        Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
        var save = new SessionSave(_player.Name, TypeOf(_player), _player.Health, _enemy.Name, TypeOf(_enemy), _enemy.Health, Turn, IsActive ? "active" : "finished", null, _history.ToList(), RemoteOf(_player), RemoteOf(_enemy), ItemOf(_player), ItemOf(_enemy));
        File.WriteAllText(SavePath, JsonSerializer.Serialize(save, JsonOptions));
    }

    public void Load()
    {
        if (!File.Exists(SavePath)) throw new FileNotFoundException("No saved game was found.", SavePath);
        var data = JsonSerializer.Deserialize<SessionSave>(File.ReadAllText(SavePath)) ?? throw new InvalidDataException("Saved game is empty.");
        _player = Create(data.PlayerName, data.PlayerType, data.PlayerRemote);
        if (data.PlayerItem is not null) _player.EquipItem(new Item(data.PlayerItem.Name, data.PlayerItem.ArmorBonus, data.PlayerItem.AttackBonus));
        _player.RestoreHealth(data.PlayerHp);
        _enemy = Create(data.EnemyName, data.EnemyType, data.EnemyRemote);
        if (data.EnemyItem is not null) _enemy.EquipItem(new Item(data.EnemyItem.Name, data.EnemyItem.ArmorBonus, data.EnemyItem.AttackBonus));
        _enemy.RestoreHealth(data.EnemyHp);
        Turn = data.Turn;
        _history.Clear();
        if (data.History is not null) _history.AddRange(data.History);
        if (_history.Count == 0) AddHistory("Previous save did not contain turn history.");
    }

    public void ShowStatus()
    {
        EnsureLoaded();
        Console.WriteLine($"Turn: {Turn}; {Player.Name}: {Player.Health}/{Player.MaxHealth} HP; {Enemy.Name}: {Enemy.Health}/{Enemy.MaxHealth} HP; status: {(IsActive ? "active" : "finished")}");
    }

    public void ShowHistory()
    {
        EnsureLoaded();
        if (_history.Count == 0) { Console.WriteLine("No actions recorded yet."); return; }
        Console.WriteLine("Battle history:");
        foreach (var entry in _history) Console.WriteLine($"- {entry}");
    }

    public void ShowResults()
    {
        if (!File.Exists(ResultsPath)) { Console.WriteLine("No completed games yet."); return; }
        Console.WriteLine(File.ReadAllText(ResultsPath));
    }

    private void Finish(string winner)
    {
        AddHistory($"Battle finished. Winner: {winner}.");
        Directory.CreateDirectory(Path.GetDirectoryName(ResultsPath)!);
        var list = File.Exists(ResultsPath) ? JsonSerializer.Deserialize<List<GameResult>>(File.ReadAllText(ResultsPath)) ?? new() : new();
        list.Add(new(DateTimeOffset.UtcNow, Player.Name, Enemy.Name, $"Winner: {winner}", Turn));
        File.WriteAllText(ResultsPath, JsonSerializer.Serialize(list, JsonOptions));
        Save();
        Console.WriteLine($"Game over. Winner: {winner}.");
    }

    private void AddHistory(string entry) => _history.Add($"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] {entry}");
    private void EnsureLoaded() { if (_player is null || _enemy is null) throw new InvalidOperationException("Start or load a game first."); }
    private void EnsureActive() { EnsureLoaded(); if (!IsActive) throw new InvalidOperationException("The game has finished. Start a new game."); }
    private static string TypeOf(Character c) => c switch { Mage => "mage", GenshinCharacter => "genshin", _ => "warrior" };
    private static SavedItem? ItemOf(Character c) => c.EquippedItem is { } item ? new SavedItem(item.Name, item.ArmorBonus, item.AttackBonus) : null;
    private static RemoteCharacter? RemoteOf(Character c) => c is GenshinCharacter g
        ? new RemoteCharacter(g.Name, g.Description, g.Rarity, g.Element, g.WeaponType, g.RemoteAbilities)
        : null;
    private static Character Create(string name, string type, RemoteCharacter? remote) => type switch
    {
        "mage" => new Mage(name),
        "genshin" when remote is not null => new GenshinCharacter(remote),
        _ => new Warrior(name)
    };
    private static Character CloneFresh(Character c) => c.CreateFreshCopy();
}
