# IsoMauiEngine — Copilot Instructions

## Big Picture
This repo is a .NET MAUI single-project app with the main code living under `IsoMauiEngine/`.
The solution file [ArsX.SolExplorer.sln](ArsX.SolExplorer.sln) now builds **only** `IsoMauiEngine`.

## Where Things Live
- Entry/app wiring: [IsoMauiEngine/MauiProgram.cs](IsoMauiEngine/MauiProgram.cs), [IsoMauiEngine/App.xaml](IsoMauiEngine/App.xaml), [IsoMauiEngine/AppShell.xaml](IsoMauiEngine/AppShell.xaml)
- Core engine-ish code: [IsoMauiEngine/Engine](IsoMauiEngine/Engine), [IsoMauiEngine/Iso](IsoMauiEngine/Iso), [IsoMauiEngine/Entities](IsoMauiEngine/Entities), [IsoMauiEngine/World](IsoMauiEngine/World)
- Rendering: [IsoMauiEngine/Rendering](IsoMauiEngine/Rendering)
- UI pages/views: [IsoMauiEngine/MainPage.xaml](IsoMauiEngine/MainPage.xaml), [IsoMauiEngine/Views](IsoMauiEngine/Views)

## Build/Run
- Build Windows target: `dotnet build IsoMauiEngine/IsoMauiEngine.csproj -c Debug -f net10.0-windows10.0.19041.0`
- Run Windows target: `dotnet run --project IsoMauiEngine/IsoMauiEngine.csproj -c Debug -f net10.0-windows10.0.19041.0`
- VS Code task: “Run IsoMauiEngine (Windows)” in [.vscode/tasks.json](.vscode/tasks.json)

## MAUI Resource Gotchas (Important)
- MAUI Resizetizer is strict about asset naming for `Resources/Images/*`:
  - filenames must be lowercase
  - start and end with a letter
  - only letters/numbers/underscores
- Don’t put non-image files (e.g. `.zip`, `.fbx`) in `Resources/Images/`. Use `Resources/Raw/` for arbitrary assets.

## Conventions
- Keep MAUI-specific UI in XAML pages; keep engine logic under `Engine/`, `Iso/`, `World/`.
- Avoid mixing WPF/WebView2 code into `IsoMauiEngine/` (this repo previously contained a separate WPF app).
