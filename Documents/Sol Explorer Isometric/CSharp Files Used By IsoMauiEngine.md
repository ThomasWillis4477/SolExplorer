# C# Files Used by `IsoMauiEngine`

This is an inventory of the C# source files that are currently included in the `IsoMauiEngine` MAUI project.

- Project file: `IsoMauiEngine/IsoMauiEngine.csproj`
- As of: 2026-01-16
- Counting rule: SDK-style projects include `**/*.cs` under the project folder by default (unless explicitly excluded). This list reflects the current `IsoMauiEngine/**/*.cs` files (excluding build outputs like `bin/` and `obj/`).

## Root
- `IsoMauiEngine/App.xaml.cs`
- `IsoMauiEngine/AppShell.xaml.cs`
- `IsoMauiEngine/MainPage.xaml.cs`
- `IsoMauiEngine/MauiProgram.cs`

## Diagnostics
- `IsoMauiEngine/Diagnostics/GameInputRouter.cs`
- `IsoMauiEngine/Diagnostics/KeyboardEventLogger.cs`
- `IsoMauiEngine/Diagnostics/PointerEventLogger.cs`
- `IsoMauiEngine/Diagnostics/RouteDebugLogger.cs`
- `IsoMauiEngine/Diagnostics/RouteDebugState.cs`

## Engine
- `IsoMauiEngine/Engine/Camera2D.cs`
- `IsoMauiEngine/Engine/GameClock.cs`
- `IsoMauiEngine/Engine/GameHost.cs`
- `IsoMauiEngine/Engine/InputState.cs`

## Entities
- `IsoMauiEngine/Entities/Entity.cs`
- `IsoMauiEngine/Entities/Player.cs`

## Iso
- `IsoMauiEngine/Iso/IsoMath.cs`

## Platforms
### Android
- `IsoMauiEngine/Platforms/Android/MainActivity.cs`
- `IsoMauiEngine/Platforms/Android/MainApplication.cs`

### iOS
- `IsoMauiEngine/Platforms/iOS/AppDelegate.cs`
- `IsoMauiEngine/Platforms/iOS/Program.cs`

### MacCatalyst
- `IsoMauiEngine/Platforms/MacCatalyst/AppDelegate.cs`
- `IsoMauiEngine/Platforms/MacCatalyst/Program.cs`

### Windows
- `IsoMauiEngine/Platforms/Windows/App.xaml.cs`

## Rendering
- `IsoMauiEngine/Rendering/DrawItem.cs`
- `IsoMauiEngine/Rendering/IsoDrawable.cs`
- `IsoMauiEngine/Rendering/Renderer2D.cs`
- `IsoMauiEngine/Rendering/SpriteAssets.cs`
- `IsoMauiEngine/Rendering/SpriteSheet.cs`

## Views
- `IsoMauiEngine/Views/AnimationTestPage.cs`
- `IsoMauiEngine/Views/ShellInfoPages.cs`

## World
- `IsoMauiEngine/World/GameWorld.cs`
- `IsoMauiEngine/World/TileMap.cs`

---
If you want, I can also generate this list automatically (script + task) so it stays up-to-date as files are added/removed.
