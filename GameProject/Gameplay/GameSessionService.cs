using System.Text.Json;
using GameProject.Characters;

namespace GameProject.Gameplay;

public sealed class GameSessionService
{
    private sealed record SessionSave(string PlayerName, string PlayerType, int PlayerHp, string EnemyName, string EnemyType, int EnemyHp, int Turn, string Status, string? Winner);
    private sealed record GameResult(DateTimeOffset FinishedAt, string Player, string Opponent, string Outcome, int Turns);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private Character? _player; private Character? _enemy;
    public int Turn { get; private set; }
    public bool IsActive => _player is not null && _enemy is not null && _player.Health > 0 && _enemy.Health > 0;
    public Character Player => _player ?? throw new InvalidOperationException("Start or load a game first.");
    public Character Enemy => _enemy ?? throw new InvalidOperationException("Start or load a game first.");
    public string SavePath => Path.Combine(AppContext.BaseDirectory, "data", "game-save.json");
    public string ResultsPath => Path.Combine(AppContext.BaseDirectory, "data", "game-results.json");

    public void Start(Character player, Character? enemy = null)
    {
        _player = CloneFresh(player); _enemy = enemy is null ? new Warrior("Training Dummy") : CloneFresh(enemy); Turn = 0;
        Save();
    }
    public void PlayerAttack()
    {
        EnsureActive();
        var player = Player;
        var enemy = Enemy;
        player.Attack(enemy);
        Turn++;
        if (enemy.Health == 0) Finish(player.Name);
        else
        {
            enemy.Attack(player);
            if (player.Health == 0) Finish(enemy.Name);
        }
        if (IsActive) Save();
    }

    public void PlayerAbility()
    {
        EnsureActive();
        var player = Player;
        var enemy = Enemy;
        player.UseAbility(enemy);
        Turn++;
        if (enemy.Health == 0) Finish(player.Name);
        else
        {
            enemy.Attack(player);
            if (player.Health == 0) Finish(enemy.Name);
        }
        if (IsActive) Save();
    }
    public void Save()
    {
        if (_player is null || _enemy is null) throw new InvalidOperationException("There is no game to save.");
        Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
        var save = new SessionSave(_player.Name, TypeOf(_player), _player.Health, _enemy.Name, TypeOf(_enemy), _enemy.Health, Turn, IsActive ? "active" : "finished", null);
        File.WriteAllText(SavePath, JsonSerializer.Serialize(save, JsonOptions));
    }
    public void Load()
    {
        if (!File.Exists(SavePath)) throw new FileNotFoundException("No saved game was found.", SavePath);
        var data = JsonSerializer.Deserialize<SessionSave>(File.ReadAllText(SavePath)) ?? throw new InvalidDataException("Saved game is empty.");
        _player = Create(data.PlayerName, data.PlayerType); _player.RestoreHealth(data.PlayerHp);
        _enemy = Create(data.EnemyName, data.EnemyType); _enemy.RestoreHealth(data.EnemyHp); Turn = data.Turn;
    }
    public void ShowStatus()
    {
        EnsureLoaded(); Console.WriteLine($"Turn: {Turn}; {Player.Name}: {Player.Health}/{Player.MaxHealth} HP; {Enemy.Name}: {Enemy.Health}/{Enemy.MaxHealth} HP; status: {(IsActive ? "active" : "finished")}");
    }
    public void ShowResults()
    {
        if (!File.Exists(ResultsPath)) { Console.WriteLine("No completed games yet."); return; }
        Console.WriteLine(File.ReadAllText(ResultsPath));
    }
    private void Finish(string winner)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ResultsPath)!);
        var list = File.Exists(ResultsPath) ? JsonSerializer.Deserialize<List<GameResult>>(File.ReadAllText(ResultsPath)) ?? new() : new();
        list.Add(new(DateTimeOffset.UtcNow, Player.Name, Enemy.Name, $"Winner: {winner}", Turn));
        File.WriteAllText(ResultsPath, JsonSerializer.Serialize(list, JsonOptions)); Save();
        Console.WriteLine($"Game over. Winner: {winner}.");
    }
    private void EnsureLoaded() { if (_player is null || _enemy is null) throw new InvalidOperationException("Start or load a game first."); }
    private void EnsureActive() { EnsureLoaded(); if (!IsActive) throw new InvalidOperationException("The game has finished. Start a new game."); }
    private static string TypeOf(Character c) => c is Mage ? "mage" : "warrior";
    private static Character Create(string name, string type) => type == "mage" ? new Mage(name) : new Warrior(name);
    private static Character CloneFresh(Character c) => Create(c.Name, TypeOf(c));
}
