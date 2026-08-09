using Godot;
using System;

namespace EraDream.Core
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
        public float SafeAreaPadding { get; private set; }

        // 事件：当屏幕方向改变时触发
        // 参数：是否为横屏
        public event Action<bool> OnOrientationChanged;
        
        // 事件：当安全区偏移改变时触发
        public event Action<float> OnSafeAreaChanged;

        // 事件：可用视口尺寸变化时触发，横竖屏切换与桌面窗口缩放都会通知。
        public event Action<Vector2> ScreenSizeChanged;

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
            
            // 监听设置中的安全区改变（使用命名方法以便在 _ExitTree 取消订阅）
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnSafeAreaPaddingChanged += OnSafeAreaPaddingChangedHandler;
                SafeAreaPadding = SettingsManager.Instance.SafeAreaPadding;
            }

            // 初始化计算一次
            CallDeferred(nameof(OnScreenSizeChanged));
        }

        private void OnScreenSizeChanged()
        {
            CurrentScreenSize = GetViewport().GetVisibleRect().Size;
            ScreenSizeChanged?.Invoke(CurrentScreenSize);
            
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

        // 安全区偏移改变的命名处理方法
        private void OnSafeAreaPaddingChangedHandler(float padding)
        {
            SafeAreaPadding = padding;
            OnSafeAreaChanged?.Invoke(padding);
        }

        public override void _ExitTree()
        {
            if (Instance == this)
            {
                Instance = null;
                if (GetTree() != null && GetTree().Root != null)
                    GetTree().Root.SizeChanged -= OnScreenSizeChanged;
                // 取消订阅安全区事件，防止 Autoload 持有悬挂引用
                if (SettingsManager.Instance != null)
                    SettingsManager.Instance.OnSafeAreaPaddingChanged -= OnSafeAreaPaddingChangedHandler;
            }
        }
    }
}
