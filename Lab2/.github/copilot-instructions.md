Project: Lab2 (ConsoleApp2)

Summary
-------
This repository is a small C# console app (labs/demos) focused on event handling and property-change validation. The core demo code lives in `ConsoleApp2/Program.cs` and contains examples of:
- a custom Event<T> class with operator overloading (+= / -=) for subscribers
- PropertyChanging / PropertyChanged flows and a validator that can cancel changes
- Small domain examples: UserProfile and SensorDevice

Quick commands
--------------
- Build solution: `dotnet build Lab2.sln` or `dotnet build ./ConsoleApp2/ConsoleApp1.csproj`
- Run project: `dotnet run --project ./ConsoleApp2/ConsoleApp1.csproj`

Important notes & gotchas
------------------------
- The `Program.cs` file contains demonstration code but does not declare a `Main` in the shown snippet — check for top-level statements or other files if `dotnet run` fails.
- Folder name vs project path: the folder is `ConsoleApp2` but the solution references `ConsoleApp1\ConsoleApp1.csproj` in the `.sln`. Prefer building the project file directly (`--project`) to avoid path mismatches.
- Target framework: `net9.0` (see `ConsoleApp2/ConsoleApp1.csproj`). Nullable and implicit usings are enabled.

Patterns to follow (use when editing)
-----------------------------------
- Custom events: the project uses a hand-rolled `Event<TEventArgs>` container rather than C# `event`. It stores handlers in a List, exposes `Add/Remove`, and overloads `+`/`-` to support `instance += handler` syntax.
  Example from `Program.cs`: `public Event<PropertyChangingEventArgs> PropertyChanging = new Event<PropertyChangingEventArgs>();`
- Validation flow: changes go through `PropertyChangingEventArgs` (has `CanChange` flag) before mutation; `PropertyChangeValidator` may set `CanChange = false` to cancel. If you modify property logic, maintain this check.
- Handlers must not throw: Event.Invoke catches exceptions per handler to keep the chain alive — don't rely on exception bubbling for control flow.

Where to look for examples
--------------------------
- `ConsoleApp2/Program.cs` — all examples, handlers, `UserProfile` and `SensorDevice` demos.

When you create or modify demos
------------------------------
- Keep the demo outputs deterministic and readable (Console.WriteLine already used heavily).
- Prefer creating small methods (e.g., `DemoEvents`) and, if needed, add a single `Main` or call the demo from an existing entry point.

If you are an AI assistant working on this repo
---------------------------------------------
- Focus on small, local changes: this project is educational and compact.
- When adding tests or runnable demos, ensure `dotnet run` works with the chosen entry point and prefer `--project` to avoid solution path mismatches.

Questions / next steps
---------------------
If anything in `Program.cs` looks like it should be split into multiple files or the solution needs aligning, ask before renaming project folders or changing the solution file.
