using Godot;
using System;
using EraDream.Core;
using EraDream.Game;

public partial class SaveSlotScreen : Control
{
    // 静态状态标识传递 (C#注释，最多两行)
    public static bool IsSaveMode { get; set; } = false;
    public static string BackScenePath { get; set; } = "res://scenes/MainMenuScreen.tscn";

    private Label _titleLabel;
    private GridContainer _gridContainer;
    private Button _btnBack;

    public struct SaveSlotMetadata
    {
        public bool HasData;
        public string PlayerName;
        public string ActiveUmaId;
        public int CurrentTurn;
        public int MaxTurns;
        public string SaveTimeString;
    }

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("MarginContainer/MainVBox/TitleLabel");
        _gridContainer = GetNode<GridContainer>("MarginContainer/MainVBox/ScrollContainer/GridContainer");
        _btnBack = GetNode<Button>("MarginContainer/MainVBox/CenterContainer/BtnBack");

        _btnBack.Pressed += OnBackPressed;

        // 根据模式初始化标题与选项 (C#注释，最多两行)
        _titleLabel.Text = IsSaveMode ? "保存游戏 (Save Game)" : "读取游戏 (Load Game)";

        RefreshSlots();

        // 响应式布局绑定 (C#注释，最多两行)
        if (ResponsiveManager.Instance != null)
        {
            ResponsiveManager.Instance.OnOrientationChanged += AdjustLayout;
            AdjustLayout(ResponsiveManager.Instance.CurrentOrientation == ScreenOrientation.Landscape);
        }
    }

    private void AdjustLayout(bool isLandscape)
    {
        // 动态调整网格的列数 (C#注释，最多两行)
        if (_gridContainer != null)
        {
            _gridContainer.Columns = isLandscape ? 2 : 1;
        }
    }

    private void RefreshSlots()
    {
        // 清理现存槽位节点 (C#注释，最多两行)
        foreach (var child in _gridContainer.GetChildren())
        {
            child.QueueFree();
        }

        // 动态渲染 6 个槽位面板 (C#注释，最多两行)
        for (int i = 1; i <= 6; i++)
        {
            int slotIndex = i;
            var meta = GetSlotMetadata(slotIndex);
            var panel = CreateSlotPanel(slotIndex, meta);
            _gridContainer.AddChild(panel);
        }
    }

    private SaveSlotMetadata GetSlotMetadata(int slotIndex)
    {
        string path = $"user://save_slot_{slotIndex}.sav";
        if (!FileAccess.FileExists(path))
        {
            return new SaveSlotMetadata { HasData = false };
        }

        try
        {
            // 快速解压反序列化提取元数据 (C#注释，最多两行)
            var state = FileIOManager.LoadBinary<GameState>(path);
            if (state != null)
            {
                ulong modTime = FileAccess.GetModifiedTime(path);
                string timeStr = Time.GetDatetimeStringFromUnixTime((long)modTime).Replace("T", " ");
                return new SaveSlotMetadata
                {
                    HasData = true,
                    PlayerName = state.Player?.PlayerName ?? "未命名",
                    ActiveUmaId = state.ActiveUmaId,
                    CurrentTurn = state.CurrentTurn,
                    MaxTurns = state.MaxTurns,
                    SaveTimeString = timeStr
                };
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SaveSlotScreen] Failed to read slot {slotIndex} metadata: {ex.Message}");
        }

        return new SaveSlotMetadata { HasData = false };
    }

    private Control CreateSlotPanel(int slotIndex, SaveSlotMetadata metadata)
    {
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(400, 140);
        
        // 绘制卡片背景与发光描边风格 (C#注释，最多两行)
        var style = new StyleBoxFlat {
            BgColor = new Color(0.12f, 0.12f, 0.15f, 0.9f),
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            BorderColor = metadata.HasData ? new Color(0.35f, 0.5f, 0.7f, 0.6f) : new Color(0.2f, 0.2f, 0.22f, 0.4f),
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 12,
            ContentMarginBottom = 12
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var margin = new MarginContainer();
        panel.AddChild(margin);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 20);
        margin.AddChild(hbox);

        // 槽位大序号标记 (C#注释，最多两行)
        var numLabel = new Label {
            Text = $"{slotIndex:D2}",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        numLabel.AddThemeFontSizeOverride("font_size", 32);
        numLabel.AddThemeColorOverride("font_color", metadata.HasData ? new Color(0.4f, 0.7f, 1.0f) : new Color(0.4f, 0.4f, 0.4f));
        hbox.AddChild(numLabel);

        // 信息区 (C#注释，最多两行)
        var vboxInfo = new VBoxContainer {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        vboxInfo.AddThemeConstantOverride("separation", 4);
        hbox.AddChild(vboxInfo);

        if (metadata.HasData)
        {
            var playerLabel = new Label { Text = $"训练员: {metadata.PlayerName}" };
            playerLabel.AddThemeFontSizeOverride("font_size", 15);
            vboxInfo.AddChild(playerLabel);

            string umaName = "无";
            if (!string.IsNullOrEmpty(metadata.ActiveUmaId))
            {
                var actor = CharacterManager.GetActor(metadata.ActiveUmaId);
                umaName = actor?.DisplayName ?? metadata.ActiveUmaId;
            }
            var umaLabel = new Label { Text = $"签约马娘: {umaName}" };
            umaLabel.AddThemeFontSizeOverride("font_size", 14);
            umaLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.6f));
            vboxInfo.AddChild(umaLabel);

            var progressLabel = new Label { Text = $"进度: {metadata.CurrentTurn} / {metadata.MaxTurns} 回合" };
            progressLabel.AddThemeFontSizeOverride("font_size", 12);
            progressLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
            vboxInfo.AddChild(progressLabel);

            var timeLabel = new Label { Text = $"时间: {metadata.SaveTimeString}" };
            timeLabel.AddThemeFontSizeOverride("font_size", 11);
            timeLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            vboxInfo.AddChild(timeLabel);
        }
        else
        {
            var emptyLabel = new Label {
                Text = "—— 空存栏 ——",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            emptyLabel.AddThemeColorOverride("font_color", new Color(0.35f, 0.35f, 0.38f));
            vboxInfo.AddChild(emptyLabel);
        }

        // 操作区 (C#注释，最多两行)
        var vboxActions = new VBoxContainer {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        vboxActions.AddThemeConstantOverride("separation", 10);
        hbox.AddChild(vboxActions);

        var btnAction = new Button {
            Text = IsSaveMode ? "保存" : "读取",
            CustomMinimumSize = new Vector2(90, 36)
        };
        if (!IsSaveMode && !metadata.HasData)
        {
            btnAction.Disabled = true;
        }
        btnAction.Pressed += () => OnSlotActionPressed(slotIndex);
        vboxActions.AddChild(btnAction);

        var btnDelete = new Button {
            Text = "删除",
            CustomMinimumSize = new Vector2(90, 30)
        };
        btnDelete.AddThemeColorOverride("font_color", new Color(0.95f, 0.4f, 0.4f));
        if (!metadata.HasData)
        {
            btnDelete.Disabled = true;
        }
        btnDelete.Pressed += () => OnSlotDeletePressed(slotIndex);
        vboxActions.AddChild(btnDelete);

        return panel;
    }

    private void OnSlotActionPressed(int slotIndex)
    {
        string slotPath = $"user://save_slot_{slotIndex}.sav";
        var notifier = GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier");

        if (IsSaveMode)
        {
            // 保存当前游戏状态并更新界面 (C#注释，最多两行)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveGame(slotPath);
                notifier?.ShowToast($"[成功] 游戏已保存至槽位 {slotIndex}。");
                RefreshSlots();
            }
        }
        else
        {
            // 读取选择槽位状态并重新加载进入养成 (C#注释，最多两行)
            if (GameManager.Instance != null && FileAccess.FileExists(slotPath))
            {
                GameManager.Instance.LoadGame(slotPath);
                notifier?.ShowToast($"[成功] 成功读取槽位 {slotIndex} 游戏进度。");
                LoadingScreen.TargetScene = "res://scenes/SimulationMainScreen.tscn";
                GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
            }
        }
    }

    private void OnSlotDeletePressed(int slotIndex)
    {
        string slotPath = $"user://save_slot_{slotIndex}.sav";
        var notifier = GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier");

        // 安全校验并物理删除对应文件 (C#注释，最多两行)
        if (FileAccess.FileExists(slotPath))
        {
            DirAccess.RemoveAbsolute(slotPath);
            notifier?.ShowToast($"[系统] 已成功删除槽位 {slotIndex} 的存档。");
            RefreshSlots();
        }
    }

    private void OnBackPressed()
    {
        // 动态回退到指定的跳转源场景 (C#注释，最多两行)
        LoadingScreen.TargetScene = BackScenePath;
        GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
    }

    public override void _ExitTree()
    {
        if (ResponsiveManager.Instance != null)
        {
            ResponsiveManager.Instance.OnOrientationChanged -= AdjustLayout;
        }
    }
}
