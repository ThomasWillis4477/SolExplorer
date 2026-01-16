# ArsX OneView — Copilot Build Specification

Goal: Build a minimal modern Chromium-based browser using WebView2 in WPF (.NET 9).
UI style: Discord-style left sidebar for quick-site icons; simple toolbar; main WebView2 content.

Tech Stack:
- .NET 9
- WPF
- C#
- WebView2 (Chromium)
- Project name: ArsX.OneView
- Namespace: ArsX.OneView

Core Features:
- Left sidebar with icon buttons (Discord, Facebook, X, Gmail, Yahoo Mail)
- Top bar: Back, Forward, Reload, URL bar
- Main panel: WebView2 browser engine
- Basic navigation logic
- Basic session restore (in future)
- Simple vault for usernames (passwords later via DPAPI)
