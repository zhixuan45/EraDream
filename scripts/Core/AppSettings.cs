using System;

public class AppSettings
{
    // 深色模式设置
    public bool IsDarkMode { get; set; } = true;
    
    // 主音量设置
    public float MasterVolume { get; set; } = 1.0f;
    
    // 嵌入式窗口设置（Godot 4 核心功能）
    public bool IsEmbeddedSubwindows { get; set; } = false; 
}
