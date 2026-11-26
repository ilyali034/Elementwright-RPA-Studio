# Elementwright-RPA-Studio

Desktop studio (WPF, .NET 10) for building and running automations on top of
[Elementwright-RPA-Framework](https://github.com/ilyali034/Elementwright-RPA-Framework).

## Framework dependency

Studio uses the framework as a library (`Elementwright.Core`).

**Development (default)** — clone both repos side by side:

```
projects/
├─ Elementwright-RPA-Framework/
└─ Elementwright-RPA-Studio/
```

The csproj picks up the sibling project automatically.

**Without a sibling clone** — the csproj falls back to the `Elementwright.Core` NuGet package.

## Run

```
dotnet run --project src/Elementwright.Studio
```
