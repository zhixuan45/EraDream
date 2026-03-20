using Godot;
using System;

namespace UmaEraArchive.Core
{
    public enum ScreenOrientation
    {
        Landscape, // 横屏 / 宽屏
        Portrait   // 竖屏 / 窄屏
    }

    /// <summary>
    /// 全局响应式布局管理器 (Autoload)
    /// 负责监听窗口尺寸变化并分发横竖屏切换事件
    /// </summary>
    public partial class ResponsiveManager : Node
    {
        // 全局单例访问点
        public static ResponsiveManager Instance { get; private set; }

        public ScreenOrientation CurrentOrientation { get; private set; }
        public Vector2 CurrentScreenSize { get; private set; }

        // 事件：当屏幕方向改变时触发
        // 参数：是否为横屏
        public event Action<bool> OnOrientationChanged;

        public override void _EnterTree()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                QueueFree();
                return;
            }
        }

        public override void _Ready()
        {
            // 连接屏幕尺寸改变信号
            GetTree().Root.SizeChanged += OnScreenSizeChanged;
            
            // 初始化计算一次
            CallDeferred(nameof(OnScreenSizeChanged));
        }

        private void OnScreenSizeChanged()
        {
            CurrentScreenSize = GetViewport().GetVisibleRect().Size;
            
            // 判断横竖屏
            ScreenOrientation newOrientation = CurrentScreenSize.X > CurrentScreenSize.Y 
                ? ScreenOrientation.Landscape 
                : ScreenOrientation.Portrait;

            // 如果方向发生改变（或者初始赋值），发送信号
            if (newOrientation != CurrentOrientation || CurrentScreenSize == Vector2.Zero)
            {
                CurrentOrientation = newOrientation;
                bool isLandscape = (CurrentOrientation == ScreenOrientation.Landscape);
                
                GD.Print($"[ResponsiveManager] Orientation changed to: {CurrentOrientation} ({CurrentScreenSize.X}x{CurrentScreenSize.Y})");
                OnOrientationChanged?.Invoke(isLandscape);
            }
            else
            {
                // 即使方向不变，如果需要更精细的 breakpoint 监听可以在这里分发一个 Resize 事件
            }
        }

        public override void _ExitTree()
        {
            if (Instance == this)
            {
                Instance = null;
                if (GetTree() != null && GetTree().Root != null)
                {
                    GetTree().Root.SizeChanged -= OnScreenSizeChanged;
                }
            }
        }
    }
}
