# GameProject.Tests

Run from the `GameProject_refactored` folder:

```bash
dotnet run --project GameProject.Tests/GameProject.Tests.csproj
```

The test suite includes unit checks for character combat, items, structured text, workspace and CLI parsing, plus HTTP-client parsing tests using a fake `HttpMessageHandler`. The API tests do not require internet access.
