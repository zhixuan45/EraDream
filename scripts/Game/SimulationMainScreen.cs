using Godot;
using System;
using System.Linq;
using EraDream.Core;
using EraDream.Core.Extensions;

namespace EraDream.Game;

public partial class SimulationMainScreen : Control
{
    // UI 节点绑定
    private BoxContainer _topSection;
    private Label _turnInfo;
    private Label _playerInfo;
    private Control _portraitContainer;
    private CharacterSprite _umaSprite;
    private Label _barkLabel;
    
    // 马娘五维属性 Label
    private Label _speedLabel;
    private Label _staminaLabel;
    private Label _powerLabel;
    private Label _gutsLabel;
    private Label _intLabel;
    private Label _affectionLabel;

    // 马娘资源进度条
    private ProgressBar _umaStaminaBar;
    private Label _umaStaminaText;
    private ProgressBar _umaEnergyBar;
    private Label _umaEnergyText;

    // 按钮
    private Button _btnTrain;
    private Button _btnWork;
    private Button _btnOuting;
    private Button _btnShop;
    private Button _btnInventory;
    private Button _btnRest;
    private Button _btnNextWeek;
    private Button _btnSystem;
    private Button _btnVisitPlayground; // 动态运动场签约按钮

    private UI.InventoryUI _inventoryUI;

    // 弹出菜单
    private PopupMenu _shopMenu;

    private PackedScene _trainingMenuScene = GD.Load<PackedScene>("res://scenes/TrainingMenuUI.tscn");

    public override void _Ready()
    {
        // 绑定节点
        _topSection = GetNode<BoxContainer>("SafeArea/MainVBox/TopSection");
        _turnInfo = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/TurnInfo");
        _playerInfo = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/PlayerInfo");
        _portraitContainer = GetNode<Control>("SafeArea/MainVBox/TopSection/PortraitContainer");

        // 动态创建立绘并拉伸以填充容器大小
        _umaSprite = new CharacterSprite { Name = "UmaSprite" };
        _umaSprite.SetAnchorsPreset(LayoutPreset.FullRect);
        _portraitContainer.AddChild(_umaSprite);
        _portraitContainer.GetNodeOrNull<Control>("Placeholder")?.Hide();

        // 动态创建精美悬浮对话气泡 (Bark Bubble)，悬浮在立绘框上方
        var bubblePanel = new PanelContainer {
            Name = "BarkBubble",
            CustomMinimumSize = new Vector2(280, 70),
            Position = new Vector2(10, -30)
        };
        
        var styleBox = new StyleBoxFlat {
            BgColor = new Color(0.12f, 0.12f, 0.14f, 0.85f),
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 14,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0.35f, 0.35f, 0.4f, 0.45f),
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 12,
            ContentMarginBottom = 12
        };
        bubblePanel.AddThemeStyleboxOverride("panel", styleBox);

        _barkLabel = new Label {
            Name = "BarkLabel",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _barkLabel.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.6f));
        _barkLabel.AddThemeConstantOverride("outline_size", 3);

        bubblePanel.AddChild(_barkLabel);
        _portraitContainer.AddChild(bubblePanel);

        _speedLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/SpeedLabel");
        _staminaLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/StaminaLabel");
        _powerLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/PowerLabel");
        _gutsLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/GutsLabel");
        _intLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/IntLabel");
        _affectionLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/AffectionLabel");

        _umaStaminaBar = GetNode<ProgressBar>("SafeArea/MainVBox/UmaResourcesVBox/StaminaHBox/StaminaBar");
        _umaStaminaText = GetNode<Label>("SafeArea/MainVBox/UmaResourcesVBox/StaminaHBox/StaminaBar/StaminaText");
        _umaEnergyBar = GetNode<ProgressBar>("SafeArea/MainVBox/UmaResourcesVBox/EnergyHBox/EnergyBar");
        _umaEnergyText = GetNode<Label>("SafeArea/MainVBox/UmaResourcesVBox/EnergyHBox/EnergyBar/EnergyText");

        _btnTrain = GetNode<Button>("SafeArea/MainVBox/BottomActions/BtnTrain");
        _btnWork = GetNode<Button>("SafeArea/MainVBox/BottomActions/BtnWork");
        _btnOuting = GetNode<Button>("SafeArea/MainVBox/BottomActions/BtnOuting");
        _btnShop = GetNode<Button>("SafeArea/MainVBox/BottomActions/BtnShop");
        _btnInventory = GetNode<Button>("SafeArea/MainVBox/BottomActions/BtnInventory");
        _btnRest = GetNode<Button>("SafeArea/MainVBox/BottomActions/BtnRest");
        _btnNextWeek = GetNode<Button>("SafeArea/MainVBox/BottomActions/BtnNextWeek");
        _btnSystem = GetNode<Button>("SafeArea/MainVBox/BottomActions/BtnSystem");

        // 绑定事件
        _btnTrain.Pressed += OnTrainPressed;
        _btnWork.Pressed += OnWorkPressed;
        _btnOuting.Pressed += OnOutingPressed;
        _btnShop.Pressed += OnShopPressed;
        _btnInventory.Pressed += OnInventoryPressed;
        _btnRest.Pressed += OnRestPressed;
        _btnNextWeek.Pressed += OnNextWeekPressed;
        _btnSystem.Pressed += OnSystemPressed;

        // 动态实例化运动场按钮
        _btnVisitPlayground = new Button {
            Text = "去运动场看看 (Playground)",
            Name = "BtnVisitPlayground"
        };
        _btnVisitPlayground.Pressed += OnVisitPlaygroundPressed;
        GetNode<Control>("SafeArea/MainVBox/BottomActions").AddChild(_btnVisitPlayground);

        _shopMenu = new PopupMenu();
        _shopMenu.AddItem("体力药水 (500)", 0);
        _shopMenu.AddItem("小蛋糕 (300)", 1);
        _shopMenu.IdPressed += OnShopMenuSelected;
        AddChild(_shopMenu);

        // 响应式布局注册
        if (ResponsiveManager.Instance != null)
        {
            ResponsiveManager.Instance.OnOrientationChanged += AdjustLayout;
            AdjustLayout(ResponsiveManager.Instance.CurrentOrientation == ScreenOrientation.Landscape);
        }

        UpdateUI();
        InitializeUma();

        // 检查并触发回合开始剧情
        GameManager.Instance?.HandleTurnStart();
    }

    private void InitializeUma()
    {
        if (GameManager.Instance?.CurrentState == null) return;
        string activeId = GameManager.Instance.CurrentState.ActiveUmaId;
        if (!string.IsNullOrEmpty(activeId))
        {
            _umaSprite.Show();
            _umaSprite.UpdateCharacter(activeId);
            TriggerBark();
        }
        else
        {
            _umaSprite.Hide();
            _barkLabel.Text = "（当前没有签约的马娘，请前往运动场签约）";
        }
    }

    private void TriggerBark()
    {
        if (GameManager.Instance?.CurrentState == null) return;
        string activeId = GameManager.Instance.CurrentState.ActiveUmaId;
        var bark = CharacterManager.GetBestBark(activeId, GameManager.Instance.CurrentState);
        
        if (bark != null)
        {
            _barkLabel.Text = bark.Text;
            _umaSprite.UpdateCharacter(activeId, bark.Expression);
            
            // 语音处理
            string voicePath = bark.Voice;
            var actor = CharacterManager.GetActor(activeId);
            if (!string.IsNullOrEmpty(voicePath) && actor?.Audio?.Voices != null && actor.Audio.Voices.TryGetValue(voicePath, out string mappedPath))
            {
                voicePath = mappedPath;
            }

            if (string.IsNullOrEmpty(voicePath))
            {
                if (actor?.Audio?.FallbackVoices?.Count > 0)
                {
                    voicePath = actor.Audio.FallbackVoices[(int)(GD.Randi() % actor.Audio.FallbackVoices.Count)];
                }
            }

            if (!string.IsNullOrEmpty(voicePath))
            {
                var stream = ResourceProxy.LoadAudioFromAbsPath(ProjectSettings.GlobalizePath(voicePath), voicePath);
                if (stream != null)
                {
                    var ap = new AudioStreamPlayer { Stream = stream };
                    AddChild(ap);
                    ap.Play();
                    ap.Finished += () => ap.QueueFree();
                }
            }
        }
    }

    public override void _ExitTree()
    {
        if (ResponsiveManager.Instance != null)
        {
            ResponsiveManager.Instance.OnOrientationChanged -= AdjustLayout;
        }
    }

    private void AdjustLayout(bool isLandscape)
    {
        _topSection.Vertical = !isLandscape;
        _topSection.AddThemeConstantOverride("separation", isLandscape ? 30 : 10);
        
        var actions = GetNode<GridContainer>("SafeArea/MainVBox/BottomActions");
        if (actions != null)
        {
            actions.Columns = isLandscape ? 4 : 2;
        }
    }

    private void UpdateUI()
    {
        if (GameManager.Instance?.CurrentState != null)
        {
            var state = GameManager.Instance.CurrentState;
            _turnInfo.Text = $"当前回合: {state.CurrentTurn} / {state.MaxTurns}";
            _playerInfo.Text = $"{state.Player.PlayerName} 体力: {state.Player.Stamina}/{state.Player.MaxStamina} | 精力: {state.Player.Energy} | 金钱: {state.Player.Money}";

            bool hasUma = !string.IsNullOrEmpty(state.ActiveUmaId);

            // 控制马娘专属行动按钮的可见性
            _btnTrain.Visible = hasUma;
            _btnOuting.Visible = hasUma;
            _btnShop.Visible = hasUma;
            _btnInventory.Visible = hasUma;
            _btnVisitPlayground.Visible = !hasUma;

            if (hasUma)
            {
                var uma = state.Uma;
                _speedLabel.Text = $"速度: {uma.Speed}";
                _staminaLabel.Text = $"耐力: {uma.Stamina}";
                _powerLabel.Text = $"力量: {uma.Power}";
                _gutsLabel.Text = $"根性: {uma.Guts}";
                _intLabel.Text = $"智力: {uma.Intelligence}";
                _affectionLabel.Text = $"好感度: {uma.Affection} | 心情: {uma.CurrentMoodStage}";

                // 显示马娘资源条
                _umaStaminaBar.Visible = true;
                _umaStaminaBar.MaxValue = uma.MaxActionStamina;
                _umaStaminaBar.Value = uma.ActionStamina;
                _umaStaminaText.Text = $"{uma.ActionStamina} / {uma.MaxActionStamina}";

                _umaEnergyBar.Visible = true;
                _umaEnergyBar.MaxValue = uma.MaxEnergy;
                _umaEnergyBar.Value = uma.Energy;
                _umaEnergyText.Text = $"{uma.Energy} / {uma.MaxEnergy}";
            }
            else
            {
                _speedLabel.Text = "速度: -";
                _staminaLabel.Text = "耐力: -";
                _powerLabel.Text = "力量: -";
                _gutsLabel.Text = "根性: -";
                _intLabel.Text = "智力: -";
                _affectionLabel.Text = "好感度: - | 心情: -";

                // 隐藏未签约马娘的资源条
                _umaStaminaBar.Visible = false;
                _umaEnergyBar.Visible = false;
            }
        }
    }

    private void OnTrainPressed()
    {
        var menu = _trainingMenuScene.Instantiate<EraDream.Game.UI.TrainingMenuUI>();
        AddChild(menu);
        menu.TrainingSelected += (type) => OnTrainingSelected((long)type);
        menu.CustomTrainingSelected += (trainingId) => OnCustomTrainingSelected(trainingId);
        menu.DynamicOptionSelected += OnDynamicOptionSelected;
        menu.CloseRequested += () => menu.Close();
    }

    private void OnDynamicOptionSelected(string menuId, string optionId)
    {
        if (BehaviorRegistry.Instance != null && GameManager.Instance?.CurrentState != null)
        {
            BehaviorRegistry.Instance.ExecuteOptionAction(menuId, optionId, GameManager.Instance.CurrentState);
            GameManager.Instance.MarkSaveDirty("动态行为选项");
            UpdateUI();
            TriggerBark();
        }
    }

    private void OnCustomTrainingSelected(string trainingId)
    {
        if (GameManager.Instance != null && !GameManager.Instance.CurrentState.IsGameOver)
        {
            var result = GameManager.Instance.Training.ExecuteTraining(GameManager.Instance.CurrentState, trainingId);
            if (result == TrainingResult.Success)
            {
                GameManager.Instance.MarkSaveDirty("自定义训练");
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("训练成功！属性获得了提升。");
                UpdateUI();
                TriggerBark();
            }
            else if (result == TrainingResult.Failed)
            {
                GameManager.Instance.MarkSaveDirty("自定义训练失败结果");
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("训练失败！马娘的心情变差了。");
                UpdateUI();
            }
            else if (result == TrainingResult.InsufficientStamina)
            {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("马娘行动力不足，无法进行训练！");
            }
            else if (result == TrainingResult.InsufficientTrainerEnergy)
            {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("训练员体力或精力不足！");
            }
        }
    }

    private void OnTrainingSelected(long id)
    {
        TrainingType type = (TrainingType)id;
        if (GameManager.Instance != null && !GameManager.Instance.CurrentState.IsGameOver)
        {
            var result = GameManager.Instance.Training.ExecuteTraining(GameManager.Instance.CurrentState, type);
            if (result == TrainingResult.Success)
            {
                GameManager.Instance.MarkSaveDirty("训练");
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("训练成功！属性获得了提升。");
                UpdateUI();
                TriggerBark();
            }
            else if (result == TrainingResult.Failed)
            {
                GameManager.Instance.MarkSaveDirty("训练失败结果");
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("训练失败！马娘的心情变差了。");
                UpdateUI();
            }
            else if (result == TrainingResult.InsufficientStamina)
            {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("马娘行动力不足，无法进行训练！");
            }
            else if (result == TrainingResult.InsufficientTrainerEnergy)
            {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("训练员体力或精力不足！");
            }
        }
    }

    private void OnWorkPressed()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CurrentState.IsGameOver)
        {
            if (GameManager.Instance.Work.ExecuteWork(GameManager.Instance.CurrentState))
            {
                GameManager.Instance.MarkSaveDirty("打工");
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("打工成功，获得了金钱！");
                UpdateUI();
            }
            else
            {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("训练员体力或精力不足！");
            }
        }
    }

    private void OnOutingPressed()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState.IsGameOver) return;

        // 检查是否有动态的外出选项
        var options = BehaviorRegistry.Instance?.GetValidOptions("Outing", GameManager.Instance.CurrentState);
        if (options != null && options.Count > 0)
        {
            // 复用 TrainingMenuUI 展现动态菜单
            var menu = _trainingMenuScene.Instantiate<EraDream.Game.UI.TrainingMenuUI>();
            AddChild(menu);
            // 重新设置标题（可选，TrainingMenuUI 目前没有公开设置标题的方法，暂且由于其通用布局直接使用）
            menu.TrainingSelected += (type) => OnOutingSelected(); // 兼容旧逻辑
            menu.DynamicOptionSelected += OnDynamicOptionSelected;
            menu.CloseRequested += () => menu.Close();
        }
        else
        {
            // 走默认逻辑
            OnOutingSelected();
        }
    }

    private void OnOutingSelected()
    {
        if (GameManager.Instance.Outing.ExecuteOuting(GameManager.Instance.CurrentState))
        {
            GameManager.Instance.MarkSaveDirty("外出");
            UpdateUI();
            TriggerBark();
        }
        else
        {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("训练员体力或精力不足！");
        }
    }

    private void OnShopPressed()
    {
        UIUtils.ShowMenuAtControl(_shopMenu, _btnShop, new Vector2I(0, -100));
    }

    private void OnShopMenuSelected(long id)
    {
        string itemId = id switch
        {
            0 => "stamina_potion",
            1 => "cupcake",
            _ => ""
        };

        if (string.IsNullOrEmpty(itemId)) return;

        var state = GameManager.Instance.CurrentState;
        if (GameManager.Instance.Shop.BuyItem(state, itemId))
        {
            // 简单逻辑：买完直接用
            GameManager.Instance.Shop.UseItem(state, itemId);
            GameManager.Instance.MarkSaveDirty("商店购买与使用物品");
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"购买并使用了物品 {itemId}");
            UpdateUI();
            TriggerBark();
        }
        else
        {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("金钱不足或物品未定义！");
        }
    }

    private void OnInventoryPressed()
    {
        if (_inventoryUI == null)
        {
            var scene = GD.Load<PackedScene>("res://scenes/InventoryUI.tscn");
            _inventoryUI = scene.Instantiate<UI.InventoryUI>();
            AddChild(_inventoryUI);
        }

        _inventoryUI.Visible = true;
        _inventoryUI.RefreshUI();
    }

    private void OnRestPressed()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CurrentState.IsGameOver)
        {
            GameManager.Instance.Rest.ExecuteRest(GameManager.Instance.CurrentState);
            GameManager.Instance.MarkSaveDirty("休息");
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("进行了休息，体力已恢复。");
            UpdateUI();
        }
    }

    private void OnNextWeekPressed()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CurrentState.IsGameOver)
        {
            GameManager.Instance.AdvanceTurn();
            UpdateUI();
            TriggerBark();
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("新的一周开始了！");
        }
    }

    private Control _systemOverlay;
    private LineEdit _cmdInput;

    private void OnSystemPressed()
    {
        // 如果已经显示，则不重复创建 (C#注释，最多两行)
        if (_systemOverlay != null && IsInstanceValid(_systemOverlay)) return;

        // 1. 创建全屏半透明遮罩背景 (C#注释，最多两行)
        _systemOverlay = new ColorRect {
            Color = new Color(0, 0, 0, 0.6f),
            Name = "SystemOverlay"
        };
        _systemOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_systemOverlay);

        // 2. 创建居中系统菜单面板 (C#注释，最多两行)
        var panel = new PanelContainer {
            CustomMinimumSize = new Vector2(400, 480)
        };
        
        var panelStyle = new StyleBoxFlat {
            BgColor = new Color(0.15f, 0.15f, 0.18f, 0.95f),
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            BorderColor = new Color(0.3f, 0.35f, 0.45f, 0.8f),
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 20,
            ContentMarginBottom = 20
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        _systemOverlay.AddChild(panel);
        panel.SetAnchorsPreset(LayoutPreset.Center);

        var margin = new MarginContainer();
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 15);
        margin.AddChild(vbox);

        // 标题 (C#注释，最多两行)
        var titleLabel = new Label {
            Text = "系统控制台",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 22);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.8f, 1.0f));
        vbox.AddChild(titleLabel);

        // 按钮列表 (C#注释，最多两行)
        var btnSave = new Button { Text = "保存游戏 (Save Game)", CustomMinimumSize = new Vector2(0, 45) };
        btnSave.Pressed += OnSystemMenuSavePressed;
        vbox.AddChild(btnSave);

        var btnLoad = new Button { Text = "读取游戏 (Load Game)", CustomMinimumSize = new Vector2(0, 45) };
        btnLoad.Pressed += OnSystemMenuLoadPressed;
        vbox.AddChild(btnLoad);

        var btnMainMenu = new Button { Text = "返回主菜单 (Return to Menu)", CustomMinimumSize = new Vector2(0, 45) };
        btnMainMenu.Pressed += OnSystemMenuReturnPressed;
        vbox.AddChild(btnMainMenu);

        var btnCancel = new Button { Text = "返回游戏 (Cancel)", CustomMinimumSize = new Vector2(0, 45) };
        btnCancel.Pressed += CloseSystemOverlay;
        vbox.AddChild(btnCancel);

        // 分割线 (C#注释，最多两行)
        var separator = new HSeparator();
        vbox.AddChild(separator);

        // 调试指令区域标题 (C#注释，最多两行)
        var debugTitle = new Label {
            Text = "调试控制台 (Debug Console):",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        debugTitle.AddThemeFontSizeOverride("font_size", 13);
        debugTitle.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        vbox.AddChild(debugTitle);

        // 调试指令输入行 (C#注释，最多两行)
        var hboxCmd = new HBoxContainer();
        hboxCmd.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(hboxCmd);

        _cmdInput = new LineEdit {
            PlaceholderText = "输入调试指令 (输入help查看帮助)...",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 36)
        };
        _cmdInput.TextSubmitted += ExecuteDebugCommand;
        hboxCmd.AddChild(_cmdInput);

        var btnExec = new Button {
            Text = "执行",
            CustomMinimumSize = new Vector2(70, 36)
        };
        btnExec.Pressed += () => ExecuteDebugCommand(_cmdInput.Text);
        hboxCmd.AddChild(btnExec);
        
        // 自动聚焦输入框 (C#注释，最多两行)
        _cmdInput.GrabFocus();
    }

    private void CloseSystemOverlay()
    {
        // 销毁全屏系统弹窗 (C#注释，最多两行)
        if (_systemOverlay != null && IsInstanceValid(_systemOverlay))
        {
            _systemOverlay.QueueFree();
            _systemOverlay = null;
        }
    }

    private void OnSystemMenuSavePressed()
    {
        // 跳转至保存存档界面 (C#注释，最多两行)
        CloseSystemOverlay();
        SaveSlotScreen.IsSaveMode = true;
        SaveSlotScreen.BackScenePath = "res://scenes/SimulationMainScreen.tscn";
        LoadingScreen.TargetScene = "res://scenes/SaveSlotScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
    }

    private void OnSystemMenuLoadPressed()
    {
        // 跳转至读取存档界面 (C#注释，最多两行)
        CloseSystemOverlay();
        SaveSlotScreen.IsSaveMode = false;
        SaveSlotScreen.BackScenePath = "res://scenes/SimulationMainScreen.tscn";
        LoadingScreen.TargetScene = "res://scenes/SaveSlotScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
    }

    private void OnSystemMenuReturnPressed()
    {
        // 自动存一下档，并返回主菜单 (C#注释，最多两行)
        CloseSystemOverlay();
        GameManager.Instance?.AutoSave();
        LoadingScreen.TargetScene = "res://scenes/MainMenuScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
    }

    private void ExecuteDebugCommand(string rawCommand)
    {
        if (string.IsNullOrWhiteSpace(rawCommand)) return;
        string text = rawCommand.Trim();
        _cmdInput.Clear();

        var state = GameManager.Instance?.CurrentState;
        if (state == null)
        {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("错误：当前游戏状态为空！");
            return;
        }

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string cmd = parts[0].ToLower();
        var notifier = GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier");

        try
        {
            switch (cmd)
            {
                case "help":
                    notifier?.ShowToast("指令: set money/speed/stamina/power/guts/int/turn [v] 或 scout nominated [id]");
                    break;
                case "set":
                    if (parts.Length < 3)
                    {
                        notifier?.ShowToast("用法: set [属性] [数值]");
                        return;
                    }
                    string prop = parts[1].ToLower();
                    if (!int.TryParse(parts[2], out int val))
                    {
                        notifier?.ShowToast("错误：数值必须为整数！");
                        return;
                    }

                    switch (prop)
                    {
                        case "money":
                            state.Player.Money = val;
                            notifier?.ShowToast($"[调试] 金钱已设为 {val}");
                            break;
                        case "speed":
                            state.Uma.Speed = val;
                            notifier?.ShowToast($"[调试] 马娘速度已设为 {val}");
                            break;
                        case "stamina":
                            state.Uma.Stamina = val;
                            notifier?.ShowToast($"[调试] 马娘耐力已设为 {val}");
                            break;
                        case "power":
                            state.Uma.Power = val;
                            notifier?.ShowToast($"[调试] 马娘力量已设为 {val}");
                            break;
                        case "guts":
                            state.Uma.Guts = val;
                            notifier?.ShowToast($"[调试] 马娘根性已设为 {val}");
                            break;
                        case "int":
                        case "intelligence":
                            state.Uma.Intelligence = val;
                            notifier?.ShowToast($"[调试] 马娘智力已设为 {val}");
                            break;
                        case "turn":
                            state.CurrentTurn = val;
                            notifier?.ShowToast($"[调试] 当前回合已设为 {val}");
                            break;
                        default:
                            notifier?.ShowToast($"错误：不支持修改的属性 '{prop}'");
                            break;
                    }
                    UpdateUI();
                    GameManager.Instance.MarkSaveDirty("调试数值修改");
                    break;

                case "scout":
                    if (parts.Length < 2)
                    {
                        notifier?.ShowToast("用法: scout nominate [uma_id] 或 scout list");
                        return;
                    }
                    string sub = parts[1].ToLower();
                    if (sub == "list")
                    {
                        GD.Print("[Scout List] Available characters in manager:");
                        foreach (var charData in CharacterManager.Characters)
                        {
                            GD.Print($" - {charData.ActorId} ({charData.DisplayName})");
                        }
                        notifier?.ShowToast("已打印可用马娘ID池到日志控制台。");
                    }
                    else if (sub == "nominate" || sub == "nominated")
                    {
                        if (parts.Length < 3)
                        {
                            notifier?.ShowToast("用法: scout nominate [uma_id]");
                            return;
                        }
                        string targetId = parts[2];
                        state.CurrentScoutPool.Clear();
                        state.CurrentScoutPool.Add(targetId);
                        GameManager.Instance.MarkSaveDirty("调试签约池修改");
                        notifier?.ShowToast($"[调试] 已将签约池重置并指定为: {targetId}");
                    }
                    else
                    {
                        notifier?.ShowToast($"错误：未知的子指令 '{sub}'");
                    }
                    break;

                default:
                    notifier?.ShowToast($"错误：未知调试指令 '{cmd}'，输入 help 查看帮助。");
                    break;
            }
        }
        catch (Exception ex)
        {
            notifier?.ShowToast($"异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理点击“去运动场看看”按钮的事件，打开签约面板
    /// </summary>
    private void OnVisitPlaygroundPressed()
    {
        var scene = GD.Load<PackedScene>("res://scenes/ScoutingUI.tscn");
        if (scene != null)
        {
            var ui = scene.Instantiate<EraDream.Game.UI.ScoutingUI>();
            AddChild(ui);
            ui.ContractSigned += () => {
                InitializeUma();
                UpdateUI();
            };
        }
    }
}
