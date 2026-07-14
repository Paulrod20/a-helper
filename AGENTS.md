# A-Helper — Project Development Guidelines

## What this project is

A-Helper is a native Windows 11 desktop app (WinForms, C#, .NET 10) that replaces Alienware
Command Center. It reads and controls hardware settings on an Alienware x16 R1
(performance modes, fan speeds/curves, GPU switching, RGB/AlienFX) via WMI.

Primary design inspiration: G-Helper (github.com/seerge/g-helper) — a lightweight,
clean, single-purpose utility for Asus laptops. We are NOT copying their code;
we are following the UX philosophy: lightweight, fast, no bloat, and a clean
sectioned UI with card-style selectable buttons.

The earlier PredatorHelper prototype is the practical implementation reference for
WinForms structure, popup layout, tray lifecycle, settings persistence, telemetry
timing, and interaction flow. Adapt those patterns for Alienware; never carry over
Acer WMI classes, command values, sensor IDs, profile IDs, or fan-control assumptions.

## Tech stack

- .NET 10, WinForms (`net10.0-windows`)
- `System.Management` for Alienware WMI communication
- Compact, DPI-aware tray-popup UI inspired by G-Helper

## Project structure (do not restructure without asking)

```
src/AHelper/
├── Models/       Plain data classes only. No logic, no UI references.
├── Services/     Hardware communication (WMI calls, system reads/writes).
│                 One class per hardware domain (e.g. FanService, PowerModeService).
└── UI/
    ├── Forms/    WinForms windows and their presentation/event wiring.
    └── Controls/ Reusable custom controls as the UI grows.
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

## Development approach

Current priorities:
- Researching WMI namespaces/classes/methods for Alienware hardware control
  (equivalents to Asus's `AcerGamingFunction`-style WMI interfaces)
- Refactoring existing code for clarity once logic is working
- Error handling and edge cases once a feature's core logic is understood and validated
- Investigating community projects/forums for reverse-engineered Alienware WMI docs

Working principles:
- Build features incrementally so each WinForms and WMI component is understood and
  verified before expanding it.
- Silently restructuring the folder/architecture above.
- Introducing new NuGet dependencies without flagging them first.

## Testing

- Unit tests belong in a sibling `tests/AHelper.Tests/` project (xUnit).
- Services should be designed to be testable in isolation
  (constructor-inject dependencies, avoid static hardware calls where possible).

## Current status

Early development. Working: dark WinForms tray-popup shell with card-style placeholder
mode selection and persisted UI preferences. Next: a read-only Alienware thermal query
service that enumerates supported profiles, fans, sensors, and current readings.
