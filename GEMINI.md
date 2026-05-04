# umaEraArchive Project Overview

`umaEraArchive` is a modern story engine and editor framework built with **Godot 4** and **C#**. It is designed to facilitate the creation, management, and playback of visual novels (Galgame/AVG) through a data-driven, node-based architecture.

## Core Technology Stack
- **Engine:** Godot 4.x (C# Support)
- **Language:** C# 10/12 (.NET 6/8)
- **Serialization:** System.Text.Json (JSON-based story projects)
- **Target Platforms:** Desktop (Windows/macOS/Linux) and Mobile (Android/iOS)

## Architecture & Key Components

### 1. Core Systems (Autoloads)
- **`ResponsiveManager`**: Monitors screen size and orientation (Landscape/Portrait), triggering events for UI adaptation.
- **`SettingsManager`**: Manages application-wide settings like Dark Mode, Volume, and Window Embedding. Persists data to `user://settings.json`.
- **`ErrorNotifier`**: A global utility for displaying toast notifications and error messages.
- **`SettingsOverlay`**: A canvas-layer UI that provides an in-game settings menu.

### 2. Editor Framework
- **`EditorScreen`**: The main visual editor interface. It utilizes Godot's `GraphEdit` node to allow creators to build story flows using draggable nodes.
- **`StoryNodeManager`**: Handles the logic for saving, loading, and synchronizing data between the visual graph and the underlying JSON storage.
- **Node Data Models**: Located in `scripts/Editor/Nodes/`, these classes (inheriting from `BaseNodeData`) define the behavior and data for different story elements (Dialogue, Choice, Background, etc.).

### 3. Story Playback
- **`StoryPlayerScreen`**: The runtime engine that interprets JSON story data and renders the visual novel experience.

## Development Conventions

### 1. Namespace Ambiguity
To avoid conflicts between Godot and System.IO:
- **File Access**: Always use `Godot.FileAccess` for Godot-specific path operations (e.g., `res://`, `user://`). Use `System.IO.File` only for physical disk operations after calling `ProjectSettings.GlobalizePath()`.
- **Directory Access**: Use `Godot.DirAccess` for Godot's filesystem and `System.IO.Directory` for OS-level tasks.

### 2. Path Handling
- Always globalize Godot virtual paths (`res://`, `user://`) using `ProjectSettings.GlobalizePath(path)` before passing them to standard C# IO libraries.
- Use `System.IO.Path.Combine` or Godot's `PathJoin` for platform-agnostic path construction.

### 3. Responsive Layouts
- Prefer Godot **Containers** (VBox, HBox, Grid, Margin) and **Anchors** over fixed pixel positions.
- Listen to `ResponsiveManager.Instance.OnOrientationChanged` to toggle UI layouts between horizontal and vertical modes.

### 4. Data Synchronization
- Data classes should implement a `SyncFromView` pattern to capture state changes from the UI (GraphNodes) back into the data model before serialization.
- Round positional data (`PosX`, `PosY`) to 2 decimal places to keep JSON files clean.

### 5. Code Structure & Maintainability
- **Line Limit**: Single source files should not exceed **1000 lines**.
- **Logic Splitting**: If a class or file exceeds this limit, it MUST be refactored and split into smaller, logically independent components, partial classes, or helper utilities to ensure readability and maintainability.
- **Single Responsibility**: Each file should focus on a specific concern; excessive length is usually a sign of violated SRP.

## Building and Running

### Building
```powershell
# Build the C# solution
dotnet build
```

### Running
- Open the project in the **Godot Engine (C# version)**.
- The main entry scene is `uid://c6587dhy1q4a` (linked to `WelcomeScreen.tscn`).
- Press **F5** in the Godot Editor to run the project.

## Key Files
- `project.godot`: Main engine configuration.
- `scripts/Core/AppSettings.cs`: Definitions for application-wide configuration.
- `scripts/Editor/EditorScreen.cs`: Main logic for the node-based story editor
- `CODING_STANDARDS.md`: Official coding style and implementation rules.
