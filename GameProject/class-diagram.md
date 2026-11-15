# Діаграма класів — модульний GameProject

```mermaid
classDiagram
    class Program
    class Application {
      +Run(args) int
      -CreateCharacterStore() ICharacterStore
      -CreateTextWorkspace() ITextWorkspace
    }
    class CommandRegistry {
      +Add(name, help, handler)
      +TryExecute(name, args, context) bool
    }
    class CommandContext {
      +ICharacterStore Characters
      +ITextWorkspace Text
    }
    class ICharacterStore {
      <<interface>>
      +AddCharacter(id, character)
      +AddItem(id, item)
      +AddAbility(id, ability)
      +GetCharacter(key)
      +GetItem(key)
      +GetAbility(key)
    }
    class CharacterStore
    class ITextWorkspace {
      <<interface>>
      +Add(kind, name, content)
      +MoveUp()
      +ChangeDirectory(path)
      +ChangeDirectoryById(id)
      +PrintWhole(includeIds)
    }
    class TextWorkspace
    class CharacterCommands
    class TextCommands
    class CharacterRecord
    class Character {
      <<abstract>>
      +Attack(target)
      +Heal(amount)
    }
    class Warrior
    class Mage
    class Ability
    class IItem {
      <<interface>>
    }
    class Item
    class TextNode
    class StructuredTextBuilder
    class StructuredTextDocument
    class StructuredTextElement {
      <<abstract>>
      +Render() string
    }
    class Heading
    class Paragraph
    class BulletList

    Program --> Application
    Application --> CommandRegistry : configures
    Application --> CommandContext : composes
    CommandRegistry --> CommandContext : dispatches
    CommandContext --> ICharacterStore
    CommandContext --> ITextWorkspace
    ICharacterStore <|.. CharacterStore
    ITextWorkspace <|.. TextWorkspace
    CharacterCommands ..> ICharacterStore : through context
    TextCommands ..> ITextWorkspace : through context
    CharacterStore o-- CharacterRecord
    CharacterRecord --> Character
    CharacterRecord o-- Ability
    CharacterRecord o-- IItem
    Character <|-- Warrior
    Character <|-- Mage
    IItem <|.. Item
    TextWorkspace *-- TextNode
    TextNode o-- TextNode : children
    StructuredTextDocument o-- StructuredTextElement
    StructuredTextElement <|-- Heading
    StructuredTextElement <|-- Paragraph
    StructuredTextElement <|-- BulletList
    StructuredTextBuilder ..> StructuredTextDocument : builds
```

## Пояснення

- `Program` лише передає аргументи запуску до `Application`.
- `Application` — точка композиції: обирає режим, створює реалізації інтерфейсів і реєструє команди.
- `ICharacterStore` та `ITextWorkspace` — межі між командним інтерфейсом і моделями. Конкретні реалізації можна замінити без переписування команд.
- `CharacterStore` відповідає за колекції персонажів, предметів, здібностей та пошук за ID/назвою.
- `Character` задає спільну поведінку; `Warrior` і `Mage` реалізують спеціалізовані типи персонажів.
- `TextWorkspace` керує навігацією та змінами дерева `TextNode`.
- `StructuredTextBuilder` дає fluent API для створення `StructuredTextDocument`, а `StructuredTextElement` забезпечує поліморфне рендерення.
- `CommandRegistry`, `CharacterCommands` і `TextCommands` розділяють маршрутизацію команд та логіку двох підсистем.
