using GameProject.Characters;
using GameProject.Items;
using GameProject.StructuredText;
using GameProject.TextModel;
using GameProject.CLI;
using GameProject.Infrastructure;
using GameProject.Gameplay;
using System.Net;
using System.Text;

// Lightweight dependency-free automated test module. Run with: dotnet run --project GameProject.Tests
var tests = new (string Name, Action Run)[]
{
    ("CharacterStore adds and finds character by id/name", CharacterStoreAddAndFind),
    ("CharacterStore generates ids and rejects duplicates", CharacterStoreIdsAndDuplicates),
    ("CharacterStore stores items and abilities", CharacterStoreItemsAndAbilities),
    ("Character combat applies armor and clamps health", CombatAppliesArmor),
    ("Equipped item changes attack and armor", EquipmentChangesStats),
    ("Mage ability damages and heals", MageAbility),
    ("Warrior ability ignores armor", WarriorAbility),
    ("TextNode renders hierarchy and ids", TextNodeRendering),
    ("TextNode rejects children in leaf nodes", TextNodeRejectsLeafChildren),
    ("TextWorkspace adds, navigates and removes nodes", TextWorkspaceNavigation),
    ("TextWorkspace rejects invalid navigation and root removal", TextWorkspaceInvalidOperations),
    ("Structured text builder renders mixed content", StructuredTextRendering),
    ("Structured text validates headings and list state", StructuredTextValidation),
    ("Command parser supports quoted arguments", CommandParserQuotes),
    ("Command parser rejects unclosed quotes", CommandParserInvalidQuotes),
    ("Command registry executes known commands only", CommandRegistryExecution),
    ("Genshin HTTP client parses remote catalogue and character", GenshinApiParsing),
    ("Genshin character maps remote data into internal model", GenshinCharacterMapping),
    ("Game session starts, advances turns, and restores saved state", GameSessionSaveAndLoad)
};

var failed = 0;
Console.WriteLine("GameProject automated tests");
Console.WriteLine(new string('=', 48));
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine($"[PASS] {test.Name}"); }
    catch (Exception ex) { failed++; Console.WriteLine($"[FAIL] {test.Name}: {ex.Message}"); }
}
Console.WriteLine(new string('-', 48));
Console.WriteLine($"Total: {tests.Length}; Passed: {tests.Length - failed}; Failed: {failed}");
Environment.ExitCode = failed == 0 ? 0 : 1;

static void Assert(bool condition, string message = "Assertion failed") { if (!condition) throw new Exception(message); }
static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected '{expected}', got '{actual}'."); }
static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception($"Expected exception {typeof(T).Name}."); }
static void CharacterStoreAddAndFind()
{
    var store = new CharacterStore(); var record = store.AddCharacter("hero", new Warrior("Artem"));
    Equal("hero", store.GetCharacter("HERO").Id); Equal("Artem", store.GetCharacter("artem").Character.Name);
    Assert(store.TryGetCharacter("hero", out var found)); Equal(record.Id, found.Id);
    Assert(!store.TryGetCharacter("missing", out _));
}
static void CharacterStoreIdsAndDuplicates()
{
    var store = new CharacterStore(); Equal("char1", store.AddCharacter(null, new Mage("M")).Id);
    Throws<InvalidOperationException>(() => store.AddCharacter("char1", new Warrior("W")));
}
static void CharacterStoreItemsAndAbilities()
{
    var store = new CharacterStore(); store.AddItem(null, new Item("Staff", 1, 7));
    store.AddAbility(null, new Ability("Spark", AbilityEffect.Damage, 9));
    Equal("Staff", store.GetItem("item1").Item.Name); Equal(9, store.GetAbility("SPARK").Ability.Power);
}
static void CombatAppliesArmor()
{
    var target = new Warrior("Tank"); target.TakeDamage(20); Equal(115, target.Health);
    target.TakeDamage(1000); Equal(0, target.Health);
}
static void EquipmentChangesStats()
{
    var hero = new Warrior("Hero"); hero.EquipItem(new Item("Axe", 4, 12));
    Equal(37, hero.TotalAttack); Equal(19, hero.TotalArmor);
}
static void MageAbility()
{
    var mage = new Mage("Mage"); var target = new Warrior("Target"); mage.TakeDamage(20);
    mage.UseAbility(target); Equal(80, mage.Health); Equal(100, target.Health);
}
static void WarriorAbility()
{
    var warrior = new Warrior("Warrior"); var target = new Warrior("Target");
    warrior.UseAbility(target); Equal(85, target.Health);
}
static void TextNodeRendering()
{
    var root = new TextNode("r", "root", TextNodeKind.Root); var heading = new TextNode("h", "Intro", TextNodeKind.Heading);
    heading.Add(new TextNode("p", "paragraph", TextNodeKind.Paragraph, "Hello")); root.Add(heading);
    Assert(root.Render().Contains("# Intro") && root.Render().Contains("- Hello")); Assert(root.Render(true).Contains("[id:h]"));
}
static void TextNodeRejectsLeafChildren()
{
    var leaf = new TextNode("p", "P", TextNodeKind.Paragraph, "text");
    Throws<InvalidOperationException>(() => leaf.Add(new TextNode("x", "X", TextNodeKind.Paragraph)));
}
static void TextWorkspaceNavigation()
{
    var ws = new TextWorkspace(); var heading = ws.Add(TextNodeKind.Heading, "Intro");
    ws.ChangeDirectoryById(heading.Id); Equal("/Intro", ws.CurrentPath);
    ws.Add(TextNodeKind.Paragraph, "p", "Hello"); ws.MoveUp(); Equal("/", ws.CurrentPath);
    ws.ChangeDirectory("/Intro"); ws.RemoveCurrent(); Equal("/", ws.CurrentPath); Equal(0, ws.Root.Children.Count);
}
static void TextWorkspaceInvalidOperations()
{
    var ws = new TextWorkspace(); Throws<KeyNotFoundException>(() => ws.ChangeDirectory("/missing"));
    Throws<InvalidOperationException>(() => ws.RemoveCurrent());
}
static void StructuredTextRendering()
{
    var doc = new StructuredTextBuilder().AddHeading("Title", 2).AddParagraph("Body")
        .StartBulletList().AddListItem("One").AddListItem("Two").Build();
    Assert(doc.Render().Contains("## Title")); Assert(doc.Render().Contains("Body"));
    Assert(doc.Render().Contains("- One") && doc.Render().Contains("- Two")); Equal(3, doc.Elements.Count);
}
static void StructuredTextValidation()
{
    Throws<ArgumentOutOfRangeException>(() => new Heading("Bad", 7));
    Throws<ArgumentException>(() => new Paragraph(" "));
    Throws<InvalidOperationException>(() => new StructuredTextBuilder().AddListItem("orphan"));
    Throws<ArgumentException>(() => new BulletList().AddItem(""));
}
static void CommandParserQuotes()
{
    var parsed = CommandLineParser.Parse("add heading \"hello world\" 'two words'");
    Equal(4, parsed.Count); Equal("hello world", parsed[2]); Equal("two words", parsed[3]);
}
static void CommandParserInvalidQuotes() => Throws<FormatException>(() => CommandLineParser.Parse("add \"unfinished"));
static void CommandRegistryExecution()
{
    var store = new CharacterStore(); var text = new TextWorkspace(); var context = new CommandContext(store, text);
    var registry = new CommandRegistry(); var called = false;
    registry.Add("ping", "ping", (_, _) => called = true);
    Assert(registry.TryExecute("PING", Array.Empty<string>(), context)); Assert(called);
    Assert(!registry.TryExecute("unknown", Array.Empty<string>(), context));
    Throws<InvalidOperationException>(() => registry.Add("ping", "duplicate", (_, _) => { }));
}

static void GenshinApiParsing()
{
    using var http = new HttpClient(new FakeApiHandler());
    using var api = new GenshinApiClient(http, "https://fake.test");
    var names = api.GetCharacterNamesAsync().GetAwaiter().GetResult();
    Equal("amber", names[0]);
    var character = api.GetCharacterAsync("Amber").GetAwaiter().GetResult();
    Equal("Amber", character.Name); Equal(4, character.Rarity); Equal("Pyro", character.Element);
    Equal("Sharpshooter", character.Abilities[0].Name);
    var weapon = api.GetWeaponAsync("Dull Blade").GetAwaiter().GetResult();
    Equal(39, weapon.BaseAttack); Equal("Sword", weapon.Type);
}
static void GenshinCharacterMapping()
{
    var source = new RemoteCharacter("Amber", "A scout of Mondstadt.", 4, "Pyro", "Bow", new[] { new RemoteAbility("Sharpshooter", "A precise shot.") });
    var mapped = new GenshinCharacter(source);
    Equal("Amber", mapped.Name); Equal("Pyro", mapped.Element); Equal(110, mapped.MaxHealth);
    Assert(mapped.RemoteAbilities.Count == 1);
}

sealed class FakeApiHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        var json = path switch
        {
            "/characters" => "[\"amber\",\"kaeya\"]",
            "/characters/amber" => "{\"name\":\"Amber\",\"description\":\"A scout of Mondstadt.\",\"rarity\":4,\"vision\":\"Pyro\",\"weapon\":\"Bow\",\"skillTalents\":[{\"name\":\"Sharpshooter\",\"description\":\"A precise shot.\"}]}",
            "/weapons/dull-blade" => "{\"name\":\"Dull Blade\",\"type\":\"Sword\",\"rarity\":1,\"baseAttack\":39}",
            _ => "{}"
        };
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}

static void GameSessionSaveAndLoad()
{
    var first = new GameSessionService();
    first.Start(new Warrior("Test Hero"));
    first.PlayerAttack();
    Equal(1, first.Turn);
    var second = new GameSessionService();
    second.Load();
    Equal(1, second.Turn);
    Equal("Test Hero", second.Player.Name);
    Equal("Training Dummy", second.Enemy.Name);
    Assert(second.Player.Health < second.Player.MaxHealth);
}
