# Build fix

Исправлены две ошибки компиляции в CLI:

- `CommandContext` теперь принимает необязательный `GameSessionService` и корректно создаёт его, если зависимость не передана.
- Исправлено использование nullable-ссылок в ходе боя через локальные ссылки `Player` и `Enemy` после проверки активной сессии.

Проверка на Windows:

```powershell
dotnet build GameProject/GameProject.csproj
dotnet run --project GameProject -- --game
```

Среда, в которой был подготовлен архив, не содержит .NET SDK, поэтому успешная компиляция здесь не подтверждалась.
