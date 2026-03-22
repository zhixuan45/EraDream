using System;

public class AppSettings
{
    // 深色模式设置
    public bool IsDarkMode { get; set; } = true;
    
    // 主音量设置
    public float MasterVolume { get; set; } = 1.0f;
    
    // 嵌入式窗口设置（Godot 4 核心功能）
    public bool IsEmbeddedSubwindows { get; set; } = false;

    // 安全区偏移量 (用于圆角屏和刘海屏适配)
    public float SafeAreaPadding { get; set; } = 0.0f;

    // 是否显示鼠标光标 (某些移动端设备可能需要隐藏或显示)
    public bool ShowMouseCursor { get; set; } = true;
}
