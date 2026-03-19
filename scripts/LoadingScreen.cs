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
    private const float LoadTime = 1.2f; // 缩短加载时间

    // 静态变量，用于控制加载后的跳转目标
    public static string TargetScene = "res://scenes/MainMenuScreen.tscn";

    private bool _isDarkMode = true;

    [Export]
    public string[] BarrageTexts = new string[] 
    {
        " SYSTEM LOADING ",
        " U.M.A NEW WORLD！ ",
        " 笙溪正在掉发... ",
        " 牢笙和牢芝也是苦命鸳鸯 ",
        " 看完这集睡着了... ", 
        " 再等等，马上就好！ ",
        " POWERD BY GEMINI ！  ",
        " C#语言，更快 更强 更好！ "
    };

    [Export]
    public Shader TextShader;

    public override void _Ready()
    {
        if (TextShader == null)
        {
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
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_focus_next") || (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.D))
        {
            _isDarkMode = !_isDarkMode;
            UpdateModeConfiguration();
            GenerateBarrageBackground();
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

    private void GenerateBarrageBackground()
    {
        foreach (Node child in _bgContainer.GetChildren()) child.QueueFree();
        _rows.Clear();

        _bgContainer.PivotOffset = Size / 2;
        _bgContainer.RotationDegrees = 27.5f;

        int fontSize = Mathf.Max(18, (int)(Size.Y / 25));
        SystemFont boldFont = new SystemFont();
        boldFont.FontWeight = 700;

        float diagonal = Size.Length() * 1.5f;
        float ySpacing = fontSize * 1.6f;
        int rowCount = Mathf.CeilToInt(diagonal / ySpacing) + 2;
        
        Vector2 startPos = (Size / 2) - new Vector2(diagonal / 2, diagonal / 2);
        
        Color rowColorA, rowColorB;
        if (_isDarkMode)
        {
            rowColorA = new Color("#444444"); 
            rowColorB = new Color("#888888");
        }
        else
        {
            rowColorA = new Color("#555555");
            rowColorB = new Color("#999999");
        }

        for (int i = 0; i < rowCount; i++)
        {
            string rawText = BarrageTexts[i % BarrageTexts.Length];
            string repeatedText = rawText;
            while (boldFont.GetStringSize(repeatedText, fontSize: fontSize).X < diagonal)
            {
                repeatedText += rawText;
            }

            float rowWidth = boldFont.GetStringSize(repeatedText, fontSize: fontSize).X;
            Color rowColor = (i % 2 == 0) ? rowColorA : rowColorB;
            float rowY = startPos.Y + i * ySpacing;
            
            float rowSpeed = 60f + (GD.Randi() % 40);
            float initialOffset = (float)GD.RandRange(0, rowWidth);

            Label l1 = CreateLabel(repeatedText, boldFont, fontSize, rowColor);
            Label l2 = CreateLabel(repeatedText, boldFont, fontSize, rowColor);

            l1.Position = new Vector2(startPos.X - initialOffset, rowY);
            l2.Position = new Vector2(l1.Position.X + rowWidth, rowY);

            _bgContainer.AddChild(l1);
            _bgContainer.AddChild(l2);

            _rows.Add(new BarrageRow { L1 = l1, L2 = l2, Width = rowWidth, Speed = rowSpeed });
        }
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
        foreach (var row in _rows)
        {
            row.Update((float)delta);
        }

        _timer += (float)delta;
        
        // 更新百分比显示
        float progress = Mathf.Clamp(_timer / LoadTime, 0f, 1f);
        _loadingText.Text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";

        if (_timer >= LoadTime)
        {
            GetTree().ChangeSceneToFile(TargetScene);
        }
    }
}
