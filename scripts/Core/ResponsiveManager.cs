using Godot;
using System;

namespace EraDream.Core
{
    public enum ScreenOrientation
    {
        Landscape,
        Portrait
    }

    /// <summary>
    /// 全局响应式布局管理器，负责尺寸、方向和系统 DPI 状态。
    /// </summary>
    public partial class ResponsiveManager : Node
    {
        public static ResponsiveManager Instance { get; private set; }

        public ScreenOrientation CurrentOrientation { get; private set; }
        public Vector2 CurrentScreenSize { get; private set; }
        public float SafeAreaPadding { get; private set; }

        // 系统缩放比例以 96 DPI 为基准，不能与 ContentScaleFactor 叠加使用。
        public float SystemScale { get; private set; } = 1.0f;

        public event Action<bool> OnOrientationChanged;
        public event Action<float> OnSafeAreaChanged;
        public event Action<Vector2> ScreenSizeChanged;
        public event Action<float> SystemScaleChanged;

        private Window _rootWindow;
        private int _currentScreen = -1;
        private int _currentDpi;
        private bool _hasScreenState;
        private bool _hasSizeState;
        private bool _isPrimaryInstance;
        private double _dpiCheckElapsed;

        private const double DpiCheckIntervalSeconds = 1.0;

        public override void _EnterTree()
        {
            if (Instance == null)
            {
                Instance = this;
                _isPrimaryInstance = true;
            }
            else
            {
                QueueFree();
            }
        }

        public override void _Ready()
        {
            if (!_isPrimaryInstance)
                return;

            _rootWindow = GetTree().Root;
            ProcessMode = ProcessModeEnum.Always;
            _rootWindow.SizeChanged += OnRootWindowSizeChanged;

            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnSafeAreaPaddingChanged += OnSafeAreaPaddingChangedHandler;
                SafeAreaPadding = SettingsManager.Instance.SafeAreaPadding;
            }

            // 延迟到窗口和可见视口完成初始化后读取实际尺寸。
            CallDeferred(nameof(OnRootWindowSizeChanged));
        }

        public override void _Process(double delta)
        {
            // Godot 没有跨平台统一的 DPI 变化信号，低频轮询即可覆盖跨屏移动。
            _dpiCheckElapsed += delta;
            if (_dpiCheckElapsed < DpiCheckIntervalSeconds)
                return;

            _dpiCheckElapsed = 0.0;
            if (UpdateSystemScale())
                UpdateResponsiveState(false);
        }

        private void OnRootWindowSizeChanged()
        {
            UpdateResponsiveState();
        }

        private void UpdateResponsiveState(bool updateSystemScale = true)
        {
            if (!IsInstanceValid(_rootWindow))
                return;

            Vector2 visibleSize = _rootWindow.GetVisibleRect().Size;
            CurrentScreenSize = visibleSize;
            ScreenSizeChanged?.Invoke(visibleSize);

            ScreenOrientation newOrientation = visibleSize.X >= visibleSize.Y
                ? ScreenOrientation.Landscape
                : ScreenOrientation.Portrait;

            // 使用显式初始化标记，避免首次恰好为横屏时漏发事件。
            if (!_hasSizeState || newOrientation != CurrentOrientation)
            {
                CurrentOrientation = newOrientation;
                _hasSizeState = true;
                OnOrientationChanged?.Invoke(newOrientation == ScreenOrientation.Landscape);
            }

            if (updateSystemScale)
                UpdateSystemScale();
        }

        private bool UpdateSystemScale()
        {
            if (!IsInstanceValid(_rootWindow))
                return false;

            int screen = _rootWindow.CurrentScreen;
            int dpi = DisplayServer.ScreenGetDpi(screen);
            float scale = dpi > 0 ? dpi / 96.0f : 1.0f;

            if (!_hasScreenState || screen != _currentScreen || dpi != _currentDpi)
            {
                bool scaleChanged = !_hasScreenState || !Mathf.IsEqualApprox(SystemScale, scale);
                _currentScreen = screen;
                _currentDpi = dpi;
                _hasScreenState = true;
                SystemScale = scale;

                if (scaleChanged)
                    SystemScaleChanged?.Invoke(scale);

                return true;
            }

            return false;
        }

        private void OnSafeAreaPaddingChangedHandler(float padding)
        {
            SafeAreaPadding = padding;
            OnSafeAreaChanged?.Invoke(padding);
        }

        public override void _ExitTree()
        {
            if (Instance != this)
                return;

            if (IsInstanceValid(_rootWindow))
                _rootWindow.SizeChanged -= OnRootWindowSizeChanged;

            if (SettingsManager.Instance != null)
                SettingsManager.Instance.OnSafeAreaPaddingChanged -= OnSafeAreaPaddingChangedHandler;

            Instance = null;
            _rootWindow = null;
        }
    }
}
