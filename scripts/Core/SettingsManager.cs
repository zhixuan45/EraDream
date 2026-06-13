using Godot;
using System;
using System.Text.Json;

public partial class SettingsManager : Node
{
    public static SettingsManager Instance { get; private set; }

    private const string SettingsFilePath = "user://settings.json";
    private AppSettings _currentSettings = new AppSettings();

    // Debounce 定时器，防止频繁属性变更导致多次写盘
    private Godot.Timer _saveDebounceTimer;

    public event Action<bool> OnDarkModeChanged;
    public event Action<float> OnSafeAreaPaddingChanged;

    public float SafeAreaPadding
    {
        get => _currentSettings.SafeAreaPadding;
        set
        {
            if (_currentSettings.SafeAreaPadding != value)
            {
                _currentSettings.SafeAreaPadding = value;
                OnSafeAreaPaddingChanged?.Invoke(value);
                // 使用节流保存，避免滑块快速滑动时反复写盘
                SaveSettingsDebounced();
            }
        }
    }

    public bool IsDarkMode
    {
        get => _currentSettings.IsDarkMode;
        set
        {
            if (_currentSettings.IsDarkMode != value)
            {
                _currentSettings.IsDarkMode = value;
                OnDarkModeChanged?.Invoke(value);
                SaveSettingsDebounced();
            }
        }
    }

    public bool IsEmbeddedSubwindows
    {
        get => _currentSettings.IsEmbeddedSubwindows;
        set
        {
            if (_currentSettings.IsEmbeddedSubwindows != value)
            {
                _currentSettings.IsEmbeddedSubwindows = value;
                ApplyWindowSettings();
                SaveSettingsDebounced();
            }
        }
    }

    public bool ShowMouseCursor
    {
        get => _currentSettings.ShowMouseCursor;
        set
        {
            if (_currentSettings.ShowMouseCursor != value)
            {
                _currentSettings.ShowMouseCursor = value;
                ApplyMouseSettings();
                SaveSettingsDebounced();
            }
        }
    }

    public string LastSavePath
    {
        get => _currentSettings.LastSavePath;
        set
        {
            if (_currentSettings.LastSavePath != value)
            {
                _currentSettings.LastSavePath = value;
                SaveSettingsDebounced();
            }
        }
    }

    public override void _EnterTree()
    {
        // 提前在树进入阶段赋值，保证其他 Autoload 的 _Ready 能访问到 Instance
        Instance = this;
    }

    public override void _Ready()
    {
        LoadSettings();

        // 创建防抖 Timer（0.5s 延迟写盘）
        _saveDebounceTimer = new Godot.Timer { OneShot = true, WaitTime = 0.5 };
        _saveDebounceTimer.Timeout += SaveSettings;
        AddChild(_saveDebounceTimer);

        // 延迟调用以确保树已准备好
        CallDeferred(MethodName.ApplyWindowSettings);
        CallDeferred(MethodName.ApplyMouseSettings);
    }

    // 触发节流延迟保存
    private void SaveSettingsDebounced()
    {
        if (_saveDebounceTimer == null) return;
        _saveDebounceTimer.Stop();
        _saveDebounceTimer.Start();
    }

    private void ApplyMouseSettings()
    {
        Input.MouseMode = _currentSettings.ShowMouseCursor ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Hidden;
    }

    private void ApplyWindowSettings()
    {
        string osName = OS.GetName();
        if (osName == "Android" || osName == "iOS")
        {
            GetTree().Root.GuiEmbedSubwindows = true;
        }
        else
        {
            GetTree().Root.GuiEmbedSubwindows = _currentSettings.IsEmbeddedSubwindows;
        }
    }

    private void LoadSettings()
    {
        var loaded = EraDream.Core.FileIOManager.LoadJson<AppSettings>(SettingsFilePath);
        if (loaded != null)
        {
            _currentSettings = loaded;
        }
        else if (FileAccess.FileExists(SettingsFilePath))
        {
            // 文件存在但解析失败，说明损坏，备份并用默认值恢复
            GD.PrintErr("[SettingsManager] 配置文件损坏，备份并重置为默认值");
            EraDream.Core.FileIOManager.SaveJson(SettingsFilePath + ".corrupted", _currentSettings);
            _currentSettings = new AppSettings();
        }
    }

    public void SaveSettings()
    {
        EraDream.Core.FileIOManager.SaveJson(SettingsFilePath, _currentSettings);
    }
}
