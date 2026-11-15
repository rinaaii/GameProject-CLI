# GameProject — OOP та командний інтерфейс

Консольний застосунок на .NET 8 із двома режимами: керування структурованим текстом та персонажами.

## Запуск

```bash
dotnet run -- --text
dotnet run -- --chars
```

Якщо аргумент режиму не переданий, програма показує меню вибору. У будь-якому режимі введіть `help`, щоб побачити доступні команди; `exit` завершує інтерпретатор.

Командний парсер підтримує одинарні та подвійні лапки для значень із пробілами.

## Режим Characters (`--chars`)

- `create char` — діалог створення воїна або мага, з автоматичним чи власним ID.
- `create item` — діалог створення предмета з бонусами атаки й захисту.
- `create ability` — діалог створення здібності з ефектом `damage` або `heal` і силою.
- `add item <item-id-or-name> --char_id <character-id-or-name>` — призначити предмет персонажу.
- `add ability <ability-id-or-name> --char_id <character-id-or-name> [--id <ability-id>]` — додати здібність персонажу; `--id` задає її локальний ID, якщо він вільний у цього персонажа.
- `act attack <actor> <target>` — атака.
- `act heal <actor> <target>` — діалог із кількістю лікування, застосованого до цілі.
- `act ability <actor> <target> --id <ability-id>` — виконати здібність, призначену персонажу-актору.
- `ls char|item|ability [--id <id-or-name>]` — короткий список або повний стан обраного об'єкта, включно з користувачами предметів/здібностей.

Приклад:

```text
> create char
Character name: Arthur
Character type (warrior/mage): warrior
Identifier (leave blank for automatic): arthur
> create ability
Ability name: Heavy strike
Effect (damage/heal): damage
Power: 30
Identifier (leave blank for automatic): strike
> add ability strike --char_id arthur --id special
> act ability arthur merlin --id special
> ls char
```

## Режим Text (`--text`)

- `pwd` — показати шлях поточного контейнера.
- `print [--whole] [--id]` — показати поточний елемент або весь документ; `--id` показує ідентифікатори.
- `add container heading|list` — створити контейнер заголовка або списку в діалоговому режимі.
- `add leaf paragraph` — створити абзац у діалоговому режимі.
- `rm [element-name]` — видалити дочірній елемент або поточний елемент; видалення потребує підтвердження.
- `up` — перейти до батьківського контейнера.
- `cd <path>` — перейти за шляхом, наприклад `cd /Introduction/Overview` або `cd ..`.
- `cd --id <container-id>` — перейти до контейнера за ID.

Приклад:

```text
> add container heading
Element name: Introduction
> add leaf paragraph
Element name: Welcome
Paragraph text: This is the first paragraph.
> pwd
/Introduction
> print --whole --id
```

## Архітектура та SOLID

- `Application` визначає режим за аргументами запуску та підключає набір команд.
- `CommandRegistry` реєструє команди окремо від їх реалізації, тому нові команди можна додавати без розширення центрального `switch`.
- `CommandLineParser` відповідає тільки за розбір введеного рядка.
- `CharacterCommands` і `TextCommands` розділяють відповідальність за дві предметні області.
- `CharacterStore` централізує створення, унікальність ID та пошук за ID або назвою.
- `TextWorkspace` керує навігацією по дереву, а `TextNode` представляє окремий вузол документа.
- `Character` лишається абстрактним класом із поліморфними реалізаціями `Warrior` і `Mage`.
- `StructuredTextBuilder` з попередньої роботи збережено як API програмного створення документа.

Діаграма класів і пояснення: [`class-diagram.md`](class-diagram.md).

## Модульна структура (оновлено)

- `Abstractions/ICharacterStore.cs` і `Abstractions/ITextWorkspace.cs` задають контракти для підсистем персонажів і структурованого тексту.
- `CommandContext` залежить від цих інтерфейсів, а не від конкретних класів.
- `Application` є composition root і створює поточні реалізації (`CharacterStore`, `TextWorkspace`) у фабричних методах. Для заміни на інше сховище/робочий простір можна реалізувати відповідний інтерфейс і змінити фабрику.
- `Scenarios/` містить два сценарії ручної перевірки програми.
- `docs/architecture.md` описує модулі, замінність і застосування принципів SOLID.

Повна діаграма класів: [`class-diagram.md`](class-diagram.md). Опис архітектури: [`docs/architecture.md`](docs/architecture.md).

## Автоматизоване тестування

До рішення додано окремий проєкт `GameProject.Tests`, який посилається на основний `GameProject` і перевіряє основні сценарії роботи сховища, персонажів і бою, текстової моделі, структурованого тексту та CLI. Тести не потребують сторонніх пакетів.

Запуск із каталогу `GameProject_refactored`:

```bash
dotnet run --project GameProject.Tests/GameProject.Tests.csproj
```

Також доступні скрипти `run-tests.bat` (Windows) та `run-tests.sh` (Linux/macOS).

## Remote Genshin API integration

The `Infrastructure/GenshinApiClient` module uses the standard .NET `HttpClient` and JSON parser to call the public API at `https://genshin.jmp.blue`. In character mode (`dotnet run --project GameProject -- --chars`), run `api sync` to download catalogue names, `api chars` / `api weapons` to browse them, `api create <name>` to import a character, `api equip <character-id> <weapon-name>` to import and equip a weapon, and `api show <character-id>` to render the current character state with the structured-text subsystem. The API provides descriptions and abilities but not full combat stats, so the internal model derives HP/attack/armor from rarity; weapon base attack is converted into an equipment attack bonus. Network/API errors are reported by the CLI and can be retried.

See `Scenarios/genshin-api-mode.txt` for a walkthrough. The HTTP client accepts an injected `HttpClient` and base URL so it can be tested with a fake HTTP handler without network access.


## Extended CLI (current practical work)

Startup modes: `dotnet run --project GameProject -- --chars`, `--text`, or `--game`. Without an option, the application asks the user to select a mode.

Character catalogue: `ls char`, `show <id/name>`, `create char`, `api sync`, `api chars`, `api create <name>`. Locally created Warrior/Mage characters are stored in `data/characters.json`. Genshin character graphic links are printed by `show`.

Text game: `game characters`, `game start <player-id/name> [opponent-id/name]`, `game status`, `game save`, `game load`, `game results`. During the turn loop use `attack`, `ability`, `status`, `save`, or `quit`. Game state and completed results are stored in `data/game-save.json` and `data/game-results.json`.
