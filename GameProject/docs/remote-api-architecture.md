# Remote API integration

```text
CLI (GenshinCommands)
        |
        v
Infrastructure (GenshinApiClient / HttpClient)
        |
        v
https://genshin.jmp.blue (JSON)
        |
        v
Remote DTO records -> GenshinCharacter / Item -> CharacterStore
                                      |
                                      v
                         StructuredTextBuilder -> Console
```

`GenshinApiClient` is the infrastructure adapter. It performs HTTP requests and parses JSON into transport records (`RemoteCharacter`, `RemoteWeapon`, `RemoteAbility`). The CLI maps these records to the domain model: `GenshinCharacter` extends the existing `Character`, and API weapons become the existing `Item`. The public API's character descriptions and skill descriptions are retained. Since this API does not provide the complete combat stat block, HP, attack, and armor are generated deterministically from rarity; weapon base attack is converted to an equipment attack bonus. The HTTP client can receive an injected `HttpClient` and base URL, allowing deterministic tests with a fake handler.
