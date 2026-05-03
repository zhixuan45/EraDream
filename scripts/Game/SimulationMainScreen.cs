using Godot;
using System;
using UmaEraArchive.Core;

namespace umaEraArchive.Game;

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

    // 按钮
    private Button _btnTrain;
    private Button _btnWork;
    private Button _btnOuting;
    private Button _btnShop;
    private Button _btnInventory;
    private Button _btnRest;
    private Button _btnNextWeek;
    private Button _btnSystem;

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

        // 动态创建立绘
        _umaSprite = new CharacterSprite { Name = "UmaSprite" };
        _portraitContainer.AddChild(_umaSprite);
        _portraitContainer.GetNodeOrNull<Control>("Placeholder")?.Hide();

        // 动态创建悬浮对话标签 (Bark)
        _barkLabel = new Label {
            Name = "BarkLabel",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(250, 0)
        };
        _barkLabel.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0));
        _barkLabel.AddThemeConstantOverride("outline_size", 4);
        _portraitContainer.AddChild(_barkLabel);
        _barkLabel.Position = new Vector2(20, 20);

        _speedLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/SpeedLabel");
        _staminaLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/StaminaLabel");
        _powerLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/PowerLabel");
        _gutsLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/GutsLabel");
        _intLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/IntLabel");
        _affectionLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/AffectionLabel");

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
            _umaSprite.UpdateCharacter(activeId);
            TriggerBark();
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
            if (string.IsNullOrEmpty(voicePath))
            {
                var actor = CharacterManager.GetActor(activeId);
                if (actor?.Audio.FallbackVoices.Count > 0)
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
            _turnInfo.Text = $"当前回合: {state.CurrentTurn} / {state.MaxTurns} (本周剩余操作: {state.Uma.ActionStamina}/{state.Uma.MaxActionStamina})";
            _playerInfo.Text = $"{state.Player.PlayerName} 体力: {state.Player.Stamina}/{state.Player.MaxStamina} | 精力: {state.Player.Energy} | 金钱: {state.Player.Money}";

            var uma = state.Uma;
            _speedLabel.Text = $"速度: {uma.Speed}";
            _staminaLabel.Text = $"耐力: {uma.Stamina}";
            _powerLabel.Text = $"力量: {uma.Power}";
            _gutsLabel.Text = $"根性: {uma.Guts}";
            _intLabel.Text = $"智力: {uma.Intelligence}";
            _affectionLabel.Text = $"好感度: {uma.Affection} | 心情: {uma.CurrentMoodStage}";
        }
    }

    private void OnTrainPressed()
    {
        var menu = _trainingMenuScene.Instantiate<UmaEraArchive.Game.UI.TrainingMenuUI>();
        AddChild(menu);
        menu.TrainingSelected += (type) => OnTrainingSelected((long)type);
        menu.CloseRequested += () => menu.Close();
    }

    private void OnTrainingSelected(long id)
    {
        TrainingType type = (TrainingType)id;
        if (GameManager.Instance != null && !GameManager.Instance.CurrentState.IsGameOver)
        {
            if (GameManager.Instance.Training.ExecuteTraining(GameManager.Instance.CurrentState, type))
            {
                UpdateUI();
                TriggerBark();
            }
            else
            {
                GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("马娘行动力不足，无法进行训练！");
            }
        }
    }

    private void OnWorkPressed()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CurrentState.IsGameOver)
        {
            if (GameManager.Instance.Work.ExecuteWork(GameManager.Instance.CurrentState))
            {
                GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("打工成功，获得了金钱！");
                UpdateUI();
            }
            else
            {
                GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("训练员体力或精力不足！");
            }
        }
    }

    private void OnOutingPressed()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CurrentState.IsGameOver)
        {
            if (GameManager.Instance.Outing.ExecuteOuting(GameManager.Instance.CurrentState))
            {
                UpdateUI();
                TriggerBark();
            }
            else
            {
                GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("训练员体力或精力不足！");
            }
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
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast($"购买并使用了物品 {itemId}");
            UpdateUI();
            TriggerBark();
        }
        else
        {
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("金钱不足或物品未定义！");
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
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("进行了休息，体力已恢复。");
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
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("新的一周开始了！");
        }
    }

    private void OnSystemPressed()
    {
        GameManager.Instance?.AutoSave();
        LoadingScreen.TargetScene = "res://scenes/MainMenuScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
    }
}
