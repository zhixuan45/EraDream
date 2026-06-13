# EraDream Internal Modding API Guide

This guide is intended for internal developers and mod creators working with the `EraDream` extension system. It details how to correctly use C# DLLs and JSON behavior packs within the engine.

## 1. Extension Loading Overview

Extensions (`.umaext` files) are managed by the `ExtensionManager`.
When an extension is activated, its `manifest.json` is checked.
- If the type is `character`, no code injection is allowed.
- If the type is `gameplay`, the engine will attempt to load custom logic.

### Gameplay Logic Structure
A gameplay pack's logic is typically located in the `Logic/` folder and consists of:
- `ModEntry.dll` (A compiled C# library implementing `IUmaPlugin`)
- `behavior.json` (A JSON file defining behavior rules, hooks, and item properties)

## 2. Using C# DLLs (`ModEntry.dll`)

### Loading Mechanism
The engine uses `ModLoader` (which extends `AssemblyLoadContext`) to load mod DLLs into isolated contexts. This isolation helps prevent conflicts between different mods and allows for unloading.

### The `IUmaPlugin` Interface
To inject logic, your `ModEntry.dll` must contain a public class that implements the `IUmaPlugin` interface. The engine will scan the assembly, instantiate the first class it finds implementing this interface, and call its `OnLoad` method.

```csharp
using EraDream.Core.Mods;

namespace MyCustomMod
{
    public class MyPlugin : IUmaPlugin
    {
        public void OnLoad()
        {
            // Initialization logic here
            // e.g., Registering custom stats or hooks
        }

        public void OnUnload()
        {
            // Cleanup logic when the mod is unloaded
        }
    }
}
```

### ⚠️ Security Warnings for C# DLLs
Loading custom DLLs is inherently risky because it allows arbitrary code execution.
- **Untrusted Code**: Never load DLLs from unknown or untrusted sources. Malicious code could access the user's file system, network, or compromise the game.
- **System.IO Abuse**: Avoid using raw `System.IO` classes for reading/writing files outside of the designated `user://` extension directories. Always use Godot's safe paths (`ProjectSettings.GlobalizePath`) and sanitize inputs (e.g., prevent `..` directory traversal).
- **Engine State**: Be careful not to hold strong references to engine nodes (`Node`) that might be freed, which could cause crashes or memory leaks.

## 3. Using JSON Data Packs (`behavior.json`)

### Loading Mechanism
The `BehaviorRegistry` is responsible for parsing `behavior.json` files and registering the defined items and behavior rules.

### `behavior.json` Structure
The file defines `rules` and `items`. Rules trigger specific `actions` when a `hook` is fired and certain `conditions` are met.

```json
{
  "rules": [
    {
      "id": "my_mod_rule_1",
      "hook": "OnTurnStart",
      "conditions": [
        {
          "property": "Player.Money",
          "operator": ">=",
          "value": "100"
        }
      ],
      "probability": 0.5,
      "action": {
        "type": "BriefStory",
        "path": "Triggered custom story!"
      }
    }
  ],
  "items": [
    {
      "id": "my_mod:custom_item",
      "name": "Custom Item",
      "type": "Consumable"
    }
  ]
}
```

### Supported Properties and Actions
- **Properties**: `Player.Money`, `Player.Stamina`, `Uma.Mood`, `Uma.Speed`, etc. Custom variables can be accessed using the prefix `Variable:`.
- **Actions**: `BriefStory` (shows a toast notification), `DetailedStory` (changes the scene to a custom path), `ChangeStat` (modifies a stat by the value defined in `value_change`).

## 4. Best Practices
1. **Namespacing**: Always prefix your custom items, rules, and variables with your unique mod ID (e.g., `my_mod:item_id`) to prevent collisions with other mods.
2. **Error Handling**: When writing your `ModEntry.dll`, wrap your logic in `try-catch` blocks and use `GD.PrintErr` to report issues rather than crashing the game.
