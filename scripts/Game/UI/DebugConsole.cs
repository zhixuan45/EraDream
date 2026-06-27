using Godot;
using System;
using System.Linq;
using EraDream.Core;

namespace EraDream.Game.UI;

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
        // 优化按键检测逻辑：支持物理键、修饰键，并在处理后消耗事件防止冲突
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            // 切换键：波浪号或反引号，检查 Keycode 和 PhysicalKeycode
            bool isToggleKey = keyEvent.Keycode == Key.Quoteleft || 
                               keyEvent.Keycode == Key.Asciitilde || 
                               keyEvent.PhysicalKeycode == Key.Quoteleft ||
                               keyEvent.PhysicalKeycode == Key.Asciitilde;

            // 关闭键：当控制台可见时，允许按 Esc 键关闭
            bool isCloseKey = Visible && keyEvent.Keycode == Key.Escape;

            if (isToggleKey || isCloseKey)
            {
                Visible = !Visible;
                GD.Print($"[DebugConsole] Toggle Visible to: {Visible} via {keyEvent.Keycode}");
                
                // 标记事件为已处理，防止穿透到输入框或其他UI组件
                GetViewport().SetInputAsHandled();

                if (Visible)
                {
                    _inputField?.GrabFocus();
                }
                else
                {
                    _inputField?.ReleaseFocus();
                }
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
                    Log("Available commands: set money [v], set speed [v], set stamina [v], set turn [v], scout nominate [id], scout list, save, load");
                    break;
                case "scout":
                    HandleScoutCommand(parts, state);
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

    /// <summary>
    /// 处理控制台马娘签约池相关的调试命令
    /// </summary>
    private void HandleScoutCommand(string[] parts, GameState state)
    {
        if (parts.Length < 2)
        {
            Log("[Usage] scout nominate [uma_id]  OR  scout list");
            return;
        }

        string sub = parts[1];
        if (sub == "list")
        {
            Log("[Scout List] Available characters in manager:");
            foreach (var charData in CharacterManager.Characters) Log($" - {charData.ActorId} ({charData.DisplayName})");
        }
        else if (sub == "nominate")
        {
            if (parts.Length < 3)
            {
                Log("[Usage] scout nominate [uma_id]");
                return;
            }
            string targetId = parts[2];
            var actor = CharacterManager.GetActor(targetId);
            if (actor == null && targetId != "test.manual_uma") Log($"[Warning] Character '{targetId}' is not loaded.");

            state.CurrentScoutPool.Clear();
            state.CurrentScoutPool.Add(targetId);
            Log($"[Success] Scout pool nominated with {targetId}");
        }
        else
        {
            Log($"[Error] Unknown scout subcommand: {sub}");
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
