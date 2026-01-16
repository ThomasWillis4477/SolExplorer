# ArsX One-View — Copilot Master Spec (Stage 2)

You are helping build **ArsX One-View**, a minimal Windows browser.

## Environment

- OS: Windows 11
- SDK: .NET 9.0.307
- Project type: WPF (.NET 9)
- Language: C#
- Project name: `ArsX.OneView`
- Root namespace: `ArsX.OneView`
- Package already planned/added: `Microsoft.Web.WebView2` (for Chromium-based WebView2 control)

## Goal for Stage 2

Implement the **main window UI and core browser wiring**:

- A **Discord-style left sidebar** with icon buttons for favourite sites
- A **top toolbar** with:
  - Back, Forward, Reload buttons
  - URL/address bar
- A **main content area** hosting a `WebView2` that loads real modern websites
- Basic navigation logic:
  - Address bar navigation (Enter)
  - Sidebar buttons navigate to their `Tag` URL
  - Back/Forward/Reload wired to WebView2
  - Update window title + address bar after navigation

## UI Requirements (MainWindow.xaml)

Create a layout with:

- A root `Grid` with 2 columns:
  - Column 0: fixed width ~70 for the sidebar
  - Column 1: star `*` width for the main area
- **Left sidebar**:
  - `StackPanel` named `Sidebar`
  - Dark background (e.g. `#202225`)
  - Top “AX” label as the ArsX mark
  - Buttons for quick sites with `Tag` set to the URL:
    - Discord – `https://discord.com/app` (label “D”)
    - Facebook – `https://www.facebook.com` (label “f”)
    - X – `https://x.com` (label “X”)
    - Yahoo Mail – `https://mail.yahoo.com` (label “Y!”)
    - Gmail – `https://mail.google.com` (label “G`)
- **Main area**:
  - A `Grid` with 2 rows:
    - Row 0 – top toolbar (fixed height)
    - Row 1 – WebView2 (fills remaining space)
  - **Toolbar**:
    - `DockPanel` with:
      - StackPanel on left containing:
        - `BackButton`
        - `ForwardButton`
        - `ReloadButton`
      - `TextBox` named `AddressBar` filling remaining space
  - **WebView**:
    - `wv2:WebView2` named `BrowserView` in row 1

Also ensure `MainWindow.xaml` includes the namespace:

```xml
xmlns:wv2="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf"
