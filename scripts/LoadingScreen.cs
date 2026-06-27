using Godot;
using System;
using System.Collections.Generic;

public partial class LoadingScreen : Control
{
    private class BarrageRow
    {
        public Label L1;
        public Label L2;
        public float Width;
        public float Speed;

        public void Update(float delta)
        {
            float move = Speed * delta;
            L1.Position -= new Vector2(move, 0);
            L2.Position -= new Vector2(move, 0);

            if (L1.Position.X < -Width)
            {
                L1.Position = new Vector2(L2.Position.X + Width, L1.Position.Y);
            }
            if (L2.Position.X < -Width)
            {
                L2.Position = new Vector2(L1.Position.X + Width, L2.Position.Y);
            }
        }
    }

    private Control _bgContainer;
    private ColorRect _dimmerLayer;
    private Label _loadingText;
    private List<BarrageRow> _rows = new List<BarrageRow>();
    private float _timer = 0f;
    private float _minLoadTime = 0.5f; // 给一个最小加载时间，防止画面一闪而过

    // 静态变量，用于控制加载后的跳转目标
    public static string TargetScene = "res://scenes/MainMenuScreen.tscn";

    private bool _isDarkMode => SettingsManager.Instance?.IsDarkMode ?? true;

    [Export]
    public string[] BarrageTexts = new string[] 
    {
        " SYSTEM LOADING ",
        " U.M.A NEW WORLD！ ",
        " 笙溪正在掉发... ",
        " 我想睡觉觉... ",
        " 看完这集睡着了... ", 
        " 再等等，马上就好！ ",
        " POWERD BY GEMINI ！  ",
        " C#语言，更快 更强 更好！ "
    };

    [Export]
    public Shader TextShader;

    public override void _Ready()
    {
        GD.Print($"[Debug] LoadingScreen: _Ready starting, Target: {TargetScene}");
        if (TextShader == null)
        {
            GD.Print("[Debug] LoadingScreen: Loading shader...");
            TextShader = ResourceLoader.Load<Shader>("res://Shaders/Text.gdshader");
        }
        _bgContainer = GetNode<Control>("BackgroundContainer");
        _loadingText = GetNode<Label>("LoadingText");
        
        // 强化Loading文字样式
        _loadingText.AddThemeFontSizeOverride("font_size", 42);
        SystemFont boldFont = new SystemFont();
        boldFont.FontWeight = 900; // 更粗的字体
        _loadingText.AddThemeFontOverride("font", boldFont);
        
        UpdateModeConfiguration();
        
        Resized += () => CallDeferred(nameof(GenerateBarrageBackground));
        CallDeferred(nameof(GenerateBarrageBackground));
        GD.Print("[Debug] LoadingScreen: _Ready finished.");
        
        // 开启后台异步加载，避免主线程卡死
        if (ResourceLoader.Exists(TargetScene))
        {
            ResourceLoader.LoadThreadedRequest(TargetScene);
        }
        else
        {
            GD.PrintErr($"[Error] LoadingScreen: TargetScene {TargetScene} does not exist!");
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_focus_next") || (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.D))
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.IsDarkMode = !SettingsManager.Instance.IsDarkMode;
                UpdateModeConfiguration();
                GenerateBarrageBackground();
            }
        }
    }

    private void UpdateModeConfiguration()
    {
        if (_dimmerLayer == null)
        {
            _dimmerLayer = new ColorRect();
            _dimmerLayer.SetAnchorsPreset(LayoutPreset.FullRect);
            _dimmerLayer.MouseFilter = MouseFilterEnum.Ignore;
            AddChild(_dimmerLayer);
            MoveChild(_dimmerLayer, 1);
        }

        if (_isDarkMode)
        {
            RenderingServer.SetDefaultClearColor(new Color("#0a0a0a"));
            _dimmerLayer.Color = new Color(0, 0, 0, 0.4f);
            _dimmerLayer.Visible = true;
            
            // 深色模式：突出显示为纯白色
            _loadingText.AddThemeColorOverride("font_color", new Color(1, 1, 1));
            _loadingText.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.5f));
            _loadingText.AddThemeConstantOverride("outline_size", 8);
        }
        else
        {
            RenderingServer.SetDefaultClearColor(new Color("#e0e0e0"));
            _dimmerLayer.Visible = false;
            
            // 浅色模式：突出显示为纯黑色
            _loadingText.AddThemeColorOverride("font_color", new Color(0, 0, 0));
            _loadingText.AddThemeColorOverride("font_outline_color", new Color(1, 1, 1, 0.5f));
            _loadingText.AddThemeConstantOverride("outline_size", 8);
        }
    }

    // === 分帧渐进生成弹幕的状态 ===
    private bool _barrageReady = false;
    private int _barrageCurrentRow = 0;
    private int _barrageTargetRowCount = 0;
    private int _barrageFontSize = 18;
    private float _barrageDiagonal = 0f;
    private Vector2 _barrageStartPos;
    private Color _barrageColorA, _barrageColorB;
    private SystemFont _barrageBoldFont;
    private const int MaxBarrageRows = 50; // 限制最大行数
    private const int RowsPerFrame = 5;    // 每帧生成行数

    private void GenerateBarrageBackground()
    {
        // 清理旧内容
        foreach (Node child in _bgContainer.GetChildren()) child.QueueFree();
        _rows.Clear();

        _bgContainer.PivotOffset = Size / 2;
        _bgContainer.RotationDegrees = 27.5f;

        // 预计算参数，存到成员变量供 _Process 分帧使用
        _barrageFontSize = Mathf.Max(32, (int)(Size.Y / 20));
        _barrageBoldFont = new SystemFont();
        _barrageBoldFont.FontWeight = 700;

        _barrageDiagonal = Size.Length() * 1.5f;
        float ySpacing = _barrageFontSize * 2.2f;
        _barrageTargetRowCount = Mathf.Min(Mathf.CeilToInt(_barrageDiagonal / ySpacing) + 2, MaxBarrageRows);
        _barrageStartPos = (Size / 2) - new Vector2(_barrageDiagonal / 2, _barrageDiagonal / 2);

        if (_isDarkMode)
        {
            _barrageColorA = new Color("#444444");
            _barrageColorB = new Color("#888888");
        }
        else
        {
            _barrageColorA = new Color("#555555");
            _barrageColorB = new Color("#999999");
        }

        _barrageCurrentRow = 0;
        _barrageReady = false;
    }

    /// <summary>
    /// 每帧创建若干行弹幕，避免一次性全生成导致卡顿
    /// </summary>
    private void ProgressiveBarrageGenerate()
    {
        if (_barrageReady) return;

        float ySpacing = _barrageFontSize * 2.2f;
        int endRow = Mathf.Min(_barrageCurrentRow + RowsPerFrame, _barrageTargetRowCount);

        for (int i = _barrageCurrentRow; i < endRow; i++)
        {
            string rawText = BarrageTexts[i % BarrageTexts.Length];
            string repeatedText = rawText;
            while (_barrageBoldFont.GetStringSize(repeatedText, fontSize: _barrageFontSize).X < _barrageDiagonal)
            {
                repeatedText += rawText;
            }

            float rowWidth = _barrageBoldFont.GetStringSize(repeatedText, fontSize: _barrageFontSize).X;
            Color rowColor = (i % 2 == 0) ? _barrageColorA : _barrageColorB;
            float rowY = _barrageStartPos.Y + i * ySpacing;

            float rowSpeed = 60f + (GD.Randi() % 40);
            float initialOffset = (float)GD.RandRange(0, rowWidth);

            Label l1 = CreateLabel(repeatedText, _barrageBoldFont, _barrageFontSize, rowColor);
            Label l2 = CreateLabel(repeatedText, _barrageBoldFont, _barrageFontSize, rowColor);

            l1.Position = new Vector2(_barrageStartPos.X - initialOffset, rowY);
            l2.Position = new Vector2(l1.Position.X + rowWidth, rowY);

            _bgContainer.AddChild(l1);
            _bgContainer.AddChild(l2);
            _rows.Add(new BarrageRow { L1 = l1, L2 = l2, Width = rowWidth, Speed = rowSpeed });
        }

        _barrageCurrentRow = endRow;
        if (_barrageCurrentRow >= _barrageTargetRowCount)
            _barrageReady = true;
    }

    private Label CreateLabel(string text, Font font, int fontSize, Color color)
    {
        Label lbl = new Label();
        lbl.Text = text;
        lbl.AddThemeFontOverride("font", font);
        lbl.AddThemeFontSizeOverride("font_size", fontSize);
        lbl.AddThemeColorOverride("font_color", color);
        
        if (TextShader != null)
        {
            ShaderMaterial mat = new ShaderMaterial();
            mat.Shader = TextShader;
            lbl.Material = mat;
        }
        
        return lbl;
    }

    public override void _Process(double delta)
    {
        // 先推进弹幕的分帧生成
        ProgressiveBarrageGenerate();

        // 驱动已有弹幕行的滚动动画
        foreach (var row in _rows)
        {
            row.Update((float)delta);
        }

        _timer += (float)delta;
        
        Godot.Collections.Array progressArray = new Godot.Collections.Array();
        ResourceLoader.ThreadLoadStatus status = ResourceLoader.LoadThreadedGetStatus(TargetScene, progressArray);

        if (status == ResourceLoader.ThreadLoadStatus.Loaded)
        {
            _loadingText.Text = "Loading... 100%";
            if (_timer >= _minLoadTime)
            {
                SetProcess(false);
                PackedScene nextScene = (PackedScene)ResourceLoader.LoadThreadedGet(TargetScene);
                GetTree().ChangeSceneToPacked(nextScene);
            }
        }
        else if (status == ResourceLoader.ThreadLoadStatus.InProgress)
        {
            float currentProgress = progressArray.Count > 0 ? (float)progressArray[0] : 0f;
            // 防止异步加载太快，结合 _timer 做一个缓动
            float visualProgress = Mathf.Min(currentProgress, _timer / _minLoadTime);
            _loadingText.Text = $"Loading... {Mathf.RoundToInt(visualProgress * 100)}%";
        }
        else
        {
            _loadingText.Text = "Loading Failed!";
            if (_timer >= _minLoadTime)
            {
                SetProcess(false);
                GetTree().ChangeSceneToFile(TargetScene); // 回退方案
            }
        }
    }
}
