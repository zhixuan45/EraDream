using Godot;
using System;
using System.Linq;
using UmaEraArchive.Core;

namespace umaEraArchive.Game.UI;

/// <summary>
/// 内置调试控制台，允许通过命令修改游戏状态 (类似命令方块)
/// </summary>
public partial class DebugConsole : CanvasLayer
{
    private LineEdit _inputField;
    private RichTextLabel _outputLog;
    private Control _consolePanel;

    public override void _Ready()
    {
        // 查找或创建 UI 节点
        _consolePanel = GetNodeOrNull<Control>("Panel");
        _inputField = GetNodeOrNull<LineEdit>("Panel/LineEdit");
        _outputLog = GetNodeOrNull<RichTextLabel>("Panel/RichTextLabel");

        if (_inputField != null)
        {
            _inputField.TextSubmitted += OnCommandSubmitted;
        }

        // 默认隐藏
        Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        // 使用 ~ 键切换控制台显示 (QuoteLeft)
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Quoteleft)
        {
            Visible = !Visible;
            if (Visible && _inputField != null)
            {
                _inputField.GrabFocus();
            }
        }
    }

    private void OnCommandSubmitted(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        Log($"> {text}");
        ExecuteCommand(text.Trim().ToLower());
        _inputField.Clear();
    }

    private void ExecuteCommand(string rawCommand)
    {
        var parts = rawCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string cmd = parts[0];
        var state = GameManager.Instance.CurrentState;

        if (state == null)
        {
            Log("[Error] GameState is null. Start a game first.");
            return;
        }

        try
        {
            switch (cmd)
            {
                case "help":
                    Log("Available commands: set money [v], set speed [v], set stamina [v], set turn [v], save, load");
                    break;
                case "set":
                    HandleSetCommand(parts, state);
                    break;
                case "save":
                    GameManager.Instance.SaveGame(GameManager.AutoSavePath);
                    Log($"[System] Game saved to {GameManager.AutoSavePath}");
                    break;
                case "load":
                    GameManager.Instance.LoadGame(GameManager.AutoSavePath);
                    Log($"[System] Game loaded from {GameManager.AutoSavePath}");
                    break;
                default:
                    Log($"[Error] Unknown command: {cmd}. Type 'help' for list.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Log($"[Exception] {ex.Message}");
        }
    }

    private void HandleSetCommand(string[] parts, GameState state)
    {
        if (parts.Length < 3)
        {
            Log("[Usage] set [property] [value]");
            return;
        }

        string prop = parts[1];
        if (!int.TryParse(parts[2], out int val))
        {
            Log("[Error] Value must be an integer.");
            return;
        }

        switch (prop)
        {
            case "money":
                state.Player.Money = val;
                Log($"[Success] Player money set to {val}");
                break;
            case "speed":
                state.Uma.Speed = val;
                Log($"[Success] Uma speed set to {val}");
                break;
            case "stamina":
                state.Uma.Stamina = val;
                Log($"[Success] Uma stamina set to {val}");
                break;
            case "turn":
                state.CurrentTurn = val;
                Log($"[Success] Current turn set to {val}");
                break;
            default:
                Log($"[Error] Unsupported property: {prop}");
                break;
        }
    }

    private void Log(string message)
    {
        GD.Print($"[Console] {message}");
        if (_outputLog != null)
        {
            _outputLog.AppendText(message + "\n");
        }
    }
}
