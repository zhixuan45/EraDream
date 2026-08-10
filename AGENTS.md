# Repository Guidelines

## Project Structure & Module Organization

EraDream is a Godot 4.6.1 .NET project targeting .NET 8 and C#. Core runtime services and shared models live in `scripts/Core/`; gameplay systems and models are under `scripts/Game/`. Editor and story-authoring code is in `scripts/ExtensionEditor/` and `scripts/StoryEditor/`. Test scripts are in `scripts/Tests/`, with their runner scene in `scenes/TestRunner.tscn`. Scenes are stored in `scenes/`, while reusable content is organized in `resources/`, `audio/`, `Shaders/`, and `translations/`. Keep `.godot/`, Android build output, and exported artifacts out of commits.

## Build, Test, and Development Commands

Open the repository in the Godot 4.6 .NET editor and press F6 to run the current scene or F5 to run the configured main scene. To build the C# project from a terminal, run:

```powershell
dotnet build EraDream.csproj
```

Run `godot --path . --editor` to start the project in the editor when the Godot executable is on `PATH`. Use the project's test runner scene for automated or regression checks:

```powershell
godot --path . --headless --scene res://scenes/TestRunner.tscn
```

Use `deploy_test_pack.ps1` when validating the packaged test data or extension workflow.

## Coding Style & Naming Conventions

Use UTF-8, four-space indentation, and C# PascalCase for types and public members; use camelCase for local variables and parameters. Follow the existing partial-class organization for UI-heavy nodes and keep each file below 1000 lines. Prefer explicit API qualification where Godot and .NET names collide (`Godot.FileAccess`, `System.IO.Directory`). Convert `res://` and `user://` paths with `ProjectSettings.GlobalizePath()` before passing them to `System.IO`, and join paths with `Path.Combine()`.

## Testing Guidelines

Add focused regression tests under `scripts/Tests/` and name them after the unit or behavior under test, for example `InventoryTest.cs`. Exercise editor, extension, and gameplay changes through `TestRunner.tscn`; include a reproducible manual Godot scenario when a UI or scene change cannot be covered by the runner.

## Commit & Pull Request Guidelines

Recent history uses short imperative Chinese messages alongside Conventional Commit prefixes such as `feat:` and `chore:`. Keep commits focused and use either concise Chinese descriptions or the established Conventional Commit form. PRs should explain the behavior change, identify affected scenes/scripts, describe tests run, link the relevant issue when available, and attach screenshots or a short recording for visible UI changes.

## Security & Configuration Tips

Treat extension manifests and imported data as untrusted input; preserve the existing validation and security-scanning paths in `scripts/Core/Extensions/`. Do not commit credentials, local absolute paths, `.godot/` state, or generated Android/build output.
