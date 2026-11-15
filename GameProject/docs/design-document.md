# GameProject — Design Document

**Version:** 1.0  
**Platform:** .NET 8 / C#  
**Application type:** Interactive console application  
**Primary purpose:** Provide a command-line interface for a character catalogue, a turn-based text game, and the project's Genshin Impact data integration.

## 1. Scope and goals

This practical work completes the command-line interface (CLI) for the existing GameProject. The CLI is the presentation layer: it reads user commands, validates arguments, calls application/domain services, and displays results. It does not duplicate character or game rules inside the command handlers.

The application supports three functional areas:

1. **Character catalogue** — list and inspect characters, import catalogue entries from the remote Genshin API, create local characters through an interactive dialogue, and persist locally created data.
2. **Text game** — select a character, start a turn-based battle, perform actions, inspect the current state, save/resume a session, and review completed results.
3. **Project theme / Genshin integration** — synchronize remote character and weapon names, import a character, equip a weapon, and inspect imported character data.

## 2. Architecture

The solution keeps the existing project and separates responsibilities by namespace/folder.

| Module | Responsibility |
|---|---|
| `CLI` | Startup mode selection, command parsing, command registration, dialogs, output formatting |
| `Characters` | Character hierarchy, character state, abilities, catalogue records |
| `Gameplay` | Character catalogue persistence and turn-based game session lifecycle/save files |
| `Infrastructure` | HTTP communication and JSON mapping for the public Genshin API |
| `Items` | Equipment abstraction and item implementation |
| `TextModel` | Structured text workspace used by the existing text mode |
| `StructuredText` | Structured document elements and rendering |
| `Abstractions` | Interfaces used to decouple CLI/application code from implementations |

### 2.1 Main types

- `Application` — composition root; chooses a mode, creates services, registers commands, and runs the REPL loop.
- `CommandRegistry` — maps command names to handlers and help descriptions.
- `CommandLineParser` — parses quoted arguments, so names containing spaces can be passed as one argument.
- `CommandContext` — provides shared access to the character store, text workspace, and game session service.
- `Character` — abstract base class; `Warrior`, `Mage`, and `GenshinCharacter` provide concrete character types.
- `CharacterStore` — in-memory catalogue for the current process.
- `CharacterCatalogPersistence` — loads/saves user-created Warrior/Mage entries and API-imported Genshin character records in JSON.
- `GameSessionService` — controls battle turns, saves the current state, restores it, and appends completed results.
- `GenshinApiClient` — isolates HTTP requests and JSON parsing from command handlers.

## 3. Startup and navigation

The program can be started with a command-line switch:

```text
dotnet run --project GameProject -- --chars
dotnet run --project GameProject -- --game
dotnet run --project GameProject -- --text
```

If no switch is supplied, the application displays a mode-selection prompt:

```text
Choose interface mode: 1) Text  2) Characters  3) Game  0) Exit
Mode>
```

After selecting a mode, the application enters a read–eval–print loop (REPL). The prompt `>` is displayed, one command is read, parsed, dispatched through `CommandRegistry`, and the result is printed. The process remains active until `exit`, `quit`, or end-of-input. Exceptions from individual commands are caught at the REPL boundary and displayed as an error; they do not terminate the whole application.

Common commands:

| Command | Description |
|---|---|
| `help` | Displays commands registered in the current mode |
| `exit` / `quit` | Leaves the CLI process |

The parser accepts quoted values, for example `api create "Raiden Shogun"`.

## 4. Character catalogue CLI

Start this mode using `--chars` or choose option `2`.

| Command | Behaviour |
|---|---|
| `ls char` | Lists all characters currently in the local catalogue |
| `ls char --id <id/name>` | Displays one catalogue entry with current statistics, equipment, and abilities |
| `show <id/name>` | Displays detailed character information; for Genshin imports it also prints the available graphic URL |
| `create char` | Starts an interactive character-creation dialogue |
| `api sync` | Downloads the current remote character and weapon name lists |
| `api chars` | Prints the synchronized remote character names |
| `api weapons` | Prints the synchronized remote weapon names |
| `api create <name>` | Downloads a remote character and imports it into the local catalogue |
| `api equip <character> <weapon>` | Imports a remote weapon and equips it to the selected character |
| `api show <character>` | Prints the imported Genshin character profile |
| `create item` / `create ability` | Creates supporting catalogue objects through prompts |
| `add ...` / `act ...` | Associates objects with characters or performs character actions |

### 4.1 Character creation dialogue

The user enters `create char`. The CLI asks for:

1. Character name.
2. Character type (`warrior` or `mage`).
3. Optional identifier (blank means the store generates one).

The command validates required values, creates the appropriate concrete class, adds it to `CharacterStore`, and invokes `CharacterCatalogPersistence.SaveUserCharacters`. User-created characters are written to `data/characters.json` under the application's output directory. API-imported Genshin characters are persisted there as well, including their parsed remote profile and equipped item bonuses. On the next start, `LoadInto` restores those entries into the catalogue without requiring the remote API to be online.

### 4.2 Character information and graphics

`ls char` is the concise catalogue view. `show <id/name>` is the detailed view and prints identity, type, health, attack, armor, equipment, and abilities. For a `GenshinCharacter`, it additionally prints element, weapon type, rarity, description, and a graphic URL. Locally-created characters do not have a remote graphic URL, so the CLI explicitly reports that no graphic was supplied.

## 5. Text game CLI

Start this mode using `--game` or choose option `3`.

| Command | Behaviour |
|---|---|
| `game characters` | Lists the characters available for selection |
| `game start <player-id/name> [opponent-id/name]` | Starts a new battle; if the opponent is omitted, a training dummy is created |
| `game status` | Displays turn number and both characters' health |
| `game history` | Displays the chronological action log for the current or loaded battle |
| `game save` | Saves the current game state |
| `game load` | Restores the last saved game and resumes the turn loop |
| `game results` | Prints the completed-game results history |

### 5.1 Main game loop

After `game start`, the CLI enters a nested `Game>` prompt. The player can issue:

- `attack` — the selected character attacks the opponent; if the opponent survives, it performs a counterattack.
- `ability` — invokes the selected character's special ability; the opponent counterattacks if still alive.
- `status` — displays current health and turn count without advancing the turn.
- `history` — prints every recorded event in chronological order, including the start, player action, opponent counterattack, and final result.
- `save` — writes the current state to disk.
- `quit` — pauses the interaction and returns to the outer CLI; the user can later call `game load`.

The loop ends when either character reaches zero health. The winner and turn count are appended to the results file. Each active turn also updates the save file, reducing the chance of losing progress.

### 5.2 Saved data

- `data/game-save.json` — player/opponent names and types, health values, turn count, and session status.
- `data/game-results.json` — history of completed games, including completion time, participants, outcome, and number of turns.
- The current save also contains the battle action log, imported Genshin character data, and equipped item bonuses so the loaded battle can continue with the same combat values.

The game service stores a lightweight snapshot and reconstructs the supported built-in character types when loading. It is a simple practical-work implementation, not a general-purpose save format for arbitrary plug-ins or equipment graphs.

## 6. Combat rules and mathematics

The battle uses deterministic integer arithmetic (there are no random rolls or critical hits in this iteration). All values are calculated using the character's current attack and armor, including equipment bonuses.

### 6.1 Base character statistics

| Character | Maximum HP | Base armor | Base attack |
|---|---:|---:|---:|
| Warrior | 120 | 15 | 25 |
| Mage | 80 | 5 | 35 |
| Imported Genshin character | `70 + 10 × rarity` | `2 × rarity` | `20 + 5 × rarity` |

For imported characters, rarity is clamped to the range 1–5 before the formula is applied. Equipment modifies the values as follows:

- `TotalAttack = BaseAttack + EquippedItem.AttackBonus`
- `TotalArmor = BaseArmor + EquippedItem.ArmorBonus`

### 6.2 Normal attack and damage

For a normal attack, the raw damage equals the attacker's `TotalAttack`. Damage reduction is applied by the target's armor:

`ActualDamage = max(0, Attacker.TotalAttack - Target.TotalArmor)`

The target's resulting health is clamped at zero:

`NewHP = max(0, CurrentHP - ActualDamage)`

A character cannot have negative health. An attack that would deal less than or equal to zero damage deals zero.

### 6.3 Special abilities

- **Warrior — Crushing Blow:** `RawDamage = TotalAttack + 10`. This ability ignores the target's armor, so `ActualDamage = RawDamage`.
- **Mage — Soul Drain:** `RawDamage = TotalAttack`; the target receives the normal armor reduction formula. The mage then heals for integer `floor(RawDamage / 2)`. Healing is capped at maximum health: `NewHP = min(MaxHP, CurrentHP + HealAmount)`. The healing amount is based on raw ability damage, not the post-armor damage.
- **Imported Genshin character:** the first API-provided skill is used as the ability label; the current prototype resolves its combat effect as a normal attack using `TotalAttack` and target armor. The API supplies descriptive skill data, not a full combat simulation.

### 6.4 Turn order and battle termination

One player turn consists of the selected player action followed by one opponent counterattack, provided the opponent is still alive. The turn counter increases once per player action. If either character reaches `0 HP`, the battle ends immediately and no further counterattack is performed. The surviving character is recorded as the winner.

### 6.5 History and persistence

Every battle records a timestamped start event, each player action, each opponent counterattack, and the final winner. The `history` command displays these entries in order. The save snapshot includes the participants, their current HP, turn number, action history, imported Genshin character data, and equipped item bonuses. Older saves without a history field remain loadable; they receive a note that the previous save did not contain a turn log.

## 7. Project theme: Genshin API integration

The project-specific third theme is the existing Genshin API integration. It is exposed through the `api` command family in character mode and game mode. `GenshinApiClient` owns HTTP communication and parses JSON responses from `/characters`, `/characters/{name}`, `/weapons`, and `/weapons/{name}` into `RemoteCharacter`, `RemoteAbility`, and `RemoteWeapon` records. `GenshinCommands` translates user commands into API calls and maps returned data to project models. Imported characters and their equipment are saved in the local JSON catalogue. The CLI reports network/API errors without closing the REPL.

The remote service is external and may be unavailable. Local catalogue and game commands remain usable without a successful API synchronization. Remote names must be synchronized before `api create` or `api equip` can resolve a name.

## 8. Error handling and validation

- Missing arguments produce a usage message.
- Unknown commands are reported with a hint to run `help`.
- Invalid character types, empty required names, invalid numbers, duplicate identifiers, and unknown catalogue entries are rejected.
- API and file-system exceptions are caught by the outer command loop and shown as `Error: ...`.
- An unavailable API does not prevent the user from using local characters or the text game.

## 9. OOP and design decisions

- **Encapsulation:** character health and session state are changed through methods, not directly by command handlers.
- **Abstraction:** `ICharacterStore` and `ITextWorkspace` separate consumers from concrete implementations; `IItem` abstracts equipment.
- **Inheritance / polymorphism:** the `Character` hierarchy allows the game to call `Attack`/`UseAbility` through the base type while concrete classes provide different ability behaviour.
- **Command pattern (lightweight):** `CommandRegistry` stores a handler for each command, separating command dispatch from the REPL loop.
- **Separation of concerns:** API access, persistence, game rules, and console interaction live in separate classes.

## 10. Build and run

Requirements: .NET 8 SDK.

```bash
dotnet restore GameProject/GameProject.csproj
dotnet build GameProject/GameProject.csproj
dotnet run --project GameProject -- --chars
dotnet run --project GameProject -- --game
dotnet run --project GameProject -- --text
dotnet run --project GameProject.Tests
```

The test project in this repository is a dependency-free executable test harness. It is run with `dotnet run --project GameProject.Tests` rather than `dotnet test`.

## 11. Suggested acceptance demonstration

1. Start `--chars`; run `help`, `create char`, `ls char`, and `show <id>`.
2. Exit and restart `--chars`; verify the created character is still listed.
3. Run `api sync` and `api chars` when the remote service is reachable; optionally import one character and show its graphic URL.
4. Restart `--chars`; verify that an API-imported Genshin character remains in the local catalogue even if the API is unavailable.
5. Start `--game`; run `game characters`, `game start <player>`, `status`, `attack` or `ability`, and `history`.
6. Pause with `quit`, run `game load`, and verify the saved state and history resume.
7. Complete a battle and run `game results`.
8. Run the test harness and include its console output in the submission evidence.
