# Elementwright-RPA-Studio

Windows desktop application (WPF, .NET 10) for inspecting and automating applications through [Elementwright-RPA-Framework](https://github.com/ilyali034/Elementwright-RPA-Framework).

## Framework dependency

Studio consumes the `Elementwright.Core` library.

**Development (recommended):** clone both repositories side by side:

```text
projects/
├── Elementwright-RPA-Framework/
└── Elementwright-RPA-Studio/
```

The project file automatically uses a sibling Framework clone through `ProjectReference`. If it is not present, it falls back to the published `Elementwright.Core` NuGet package.

## Run

On Windows with the .NET 10 SDK installed:

```powershell
dotnet restore .\src\Elementwright.Studio\Elementwright.Studio.csproj
dotnet build .\src\Elementwright.Studio\Elementwright.Studio.csproj
dotnet run --project .\src\Elementwright.Studio\Elementwright.Studio.csproj
```

## Inspector workflow

1. Press **Refresh applications** to enumerate top-level application windows.
2. Select a window and choose **Connect to target**.
3. Expand the UI Automation tree and select an element.
4. Inspect its metadata, bounds, current value and supported UI Automation patterns.
5. Use **Refresh tree** after the target application's UI changes.
6. Use **Test selector** to verify that the selected element can be resolved again.
7. Use **Copy selector** to copy the selector as JSON (parse it with `ElementSelector.Parse`).
8. Use the available actions when their buttons are enabled:
   - **Invoke element** — UIA Invoke pattern (commonly buttons and menu items).
   - **Set value** — UIA Value pattern (commonly editable fields).
   - **Toggle** — UIA Toggle pattern (commonly checkboxes).
   - **Select** — UIA SelectionItem pattern (commonly list and tab items).

Actions are pattern-based. A disabled action means the selected element does not advertise that pattern or is disabled; Studio does not silently fall back to mouse coordinates.

## Known limitations

- Windows desktop session required.
- UI Automation support depends on the target application's accessibility provider.
- The tree is a bounded snapshot, not a live-updating accessibility tree. Refresh it after navigation or major UI changes.
- Selector resolution currently uses the first matching element. Duplicate-ID disambiguation, hover highlighting, mouse picking and a visual workflow designer are not implemented yet.
