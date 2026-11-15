using GameProject.Gameplay;

namespace GameProject.CLI;

internal static class GameCommands
{
    public static void Execute(string[] args, CommandContext context)
    {
        if (args.Length == 0) throw new ArgumentException("Usage: game <characters|start|status|save|load|results>");
        switch (args[0].ToLowerInvariant())
        {
            case "characters":
                var characters = context.Characters.Characters.ToList();
                if (characters.Count == 0) { Console.WriteLine("Catalogue is empty. Use create char first."); return; }
                foreach (var c in characters) Console.WriteLine($"[{c.Id}] {c.Character.Name} ({(c.Character is GameProject.Characters.Mage ? "Mage" : "Warrior")}) HP {c.Character.Health}/{c.Character.MaxHealth}");
                break;
            case "start":
                if (args.Length < 2) throw new ArgumentException("Usage: game start <player-id/name> [opponent-id/name]");
                var player = context.Characters.GetCharacter(args[1]).Character;
                var enemy = args.Length >= 3 ? context.Characters.GetCharacter(args[2]).Character : null;
                context.Game.Start(player, enemy);
                Console.WriteLine($"Game started: {context.Game.Player.Name} vs {context.Game.Enemy.Name}.");
                Console.WriteLine("Turn commands: attack, ability, status, save, quit");
                RunTurnLoop(context.Game);
                break;
            case "status": context.Game.ShowStatus(); break;
            case "save": context.Game.Save(); Console.WriteLine("Game state saved."); break;
            case "load": context.Game.Load(); Console.WriteLine("Saved game loaded."); context.Game.ShowStatus(); RunTurnLoop(context.Game); break;
            case "results": context.Game.ShowResults(); break;
            default: throw new ArgumentException("Use game characters, start, status, save, load, or results.");
        }
    }
    private static void RunTurnLoop(GameSessionService game)
    {
        while (game.IsActive)
        {
            Console.Write("Game> "); var input = Console.ReadLine(); if (input is null) return;
            switch (input.Trim().ToLowerInvariant())
            {
                case "attack": game.PlayerAttack(); break;
                case "ability": game.PlayerAbility(); break;
                case "status": game.ShowStatus(); break;
                case "save": game.Save(); Console.WriteLine("Saved."); break;
                case "quit": Console.WriteLine("Game paused. Use 'game load' to continue."); return;
                default: Console.WriteLine("Commands: attack, ability, status, save, quit"); break;
            }
        }
    }
}
