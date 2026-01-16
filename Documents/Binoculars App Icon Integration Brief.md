# ArsX One-View — Binoculars App Icon Integration Brief

## 1. Context

You are integrating a new **binoculars icon** as the primary application icon for **ArsX One-View**, a .NET 9 WPF browser.  
The source artwork already exists as a PNG (flat white binoculars on black) and will be manually converted into a multi-size `.ico` file.

**Assumption for this brief:**
- Thomas will create `OneView.ico` from the existing PNG (using an external icon tool) and place it at:
  - `assets/OneView.ico` (relative to the project file).
- Copilot only needs to:
  - Ensure the project references this icon correctly.
  - Make sure the main window uses the same icon.
  - Keep everything consistent for EXE, taskbar, and window chrome.

Do **not** attempt to generate the `.ico` file in code. Assume it already exists in `assets/OneView.ico`.

---

## 2. Goals

1. **Use the binoculars icon as the application icon**:
   - Windows EXE should show it in Explorer, taskbar, and Alt-Tab.
2. **Use the same icon for the main WPF window**:
   - Window title bar should display the binoculars.
3. **Keep project structure clean**:
   - Icon stored under `assets/OneView.ico`.
  - `assets/OneView.ico` (relative to the project file).
   - Icon stored under `assets/OneView.ico`.
1. Confirm there is an `assets` folder in the project.
2. Add an item entry for the icon file if not already present.
---

## 3. Tasks

  <Resource Include="assets\OneView.ico" />

**File:** `arsX.SolExplorer.csproj` (or whatever the main project file is named)

1. Confirm there is a `Resources` folder in the project.
2. Add an item entry for the icon file if not already present.

Add or update the relevant `<ItemGroup>` to include the icon:

```xml
<ItemGroup>
  <Resource Include="assets\OneView.ico" />
</ItemGroup>
