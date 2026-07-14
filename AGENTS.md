# A-Helper — Project Conventions for Codex

## What this project is

A-Helper is a native Windows 11 desktop app (WPF, C#, .NET 10) that replaces Alienware
Command Center. It reads and controls hardware settings on an Alienware x16 R1
(performance modes, fan speeds/curves, GPU switching, RGB/AlienFX) via WMI.

Primary design inspiration: G-Helper (github.com/seerge/g-helper) — a lightweight,
clean, single-purpose utility for Asus laptops. We are NOT copying their code
(they're WinForms; we're WPF) — only the UX philosophy: lightweight, fast, no bloat,
clean sectioned UI with card-style selectable buttons.

## Tech stack

- .NET 10, WPF (`net10.0-windows`)
- WPF-UI 4.3.0 — Fluent Design controls, dark theme, Mica backdrop, built-in Fluent icons
- MVVM architecture (no code-behind logic beyond wiring — see structure below)

## Project structure (do not restructure without asking)

```
src/AHelper/
├── Models/       Plain data classes only. No logic, no UI references.
├── Services/     Hardware communication (WMI calls, system reads/writes).
│                 One class per hardware domain (e.g. FanService, PowerModeService).
├── ViewModels/   Binds Models to Views. Holds observable state + commands.
│                 Never references XAML/UI types directly.
├── Views/        XAML windows/pages + minimal code-behind (wiring only).
└── Themes/       Resource dictionaries — colors, fonts, control styles.
```

## Coding conventions

- Match the code organization quality of a professional codebase: clear separation
  of concerns, one responsibility per class, no god-objects.
- Prefer clean, modern C# idioms (LINQ, pattern matching) over verbose manual loops
  when it improves readability — but clarity always wins over cleverness.
- Every public method/class gets a short XML doc comment explaining *why*, not just what.
- No magic numbers/strings — use named constants for WMI class names, method names,
  registry keys, etc.
- Naming: PascalCase for classes/methods/properties, camelCase for locals/params,
  `_camelCase` for private fields.

## How Codex should help on this project

Good uses:
- Researching WMI namespaces/classes/methods for Alienware hardware control
  (equivalents to Asus's `AcerGamingFunction`-style WMI interfaces)
- Refactoring existing code for clarity once logic is working
- Error handling and edge cases once a feature's core logic is understood by Paul
- Investigating community projects/forums for reverse-engineered Alienware WMI docs

Avoid:
- Writing entire features end-to-end unprompted — Paul is learning WPF/MVVM deeply
  and wants to understand every piece, not receive finished code to paste in.
- Silently restructuring the folder/architecture above.
- Introducing new NuGet dependencies without flagging them first.

## Testing

- Unit tests belong in a sibling `tests/AHelper.Tests/` project (xUnit).
- ViewModels and Services should be designed to be testable in isolation
  (constructor-inject dependencies, avoid static hardware calls where possible).

## Current status

Early development. Working: dark-themed FluentWindow shell with custom title bar.
Next: card-style selectable button group (mirrors G-Helper's mode-selection UI),
then WMI research for Alienware performance mode + fan control equivalents.
