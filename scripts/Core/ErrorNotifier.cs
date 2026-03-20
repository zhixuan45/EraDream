using Godot;
using System.Collections.Generic;

public partial class ErrorNotifier : CanvasLayer
{
    private AcceptDialog _errorDialog;
    private PanelContainer _toastContainer;
    private Label _toastLabel;
    private Godot.Timer _toastTimer;
    private Tween _toastTween;

    private Queue<string> _toastQueue = new Queue<string>();
    private bool _isToastShowing = false;
    private float _toastDuration = 3.0f;

    public override void _Ready()
    {
        Layer = 100; // 确保在最顶层

        Theme mainTheme = null;
        if (ResourceLoader.Exists("res://resources/theme_main.tres"))
        {
            mainTheme = GD.Load<Theme>("res://resources/theme_main.tres");
        }

        // 1. 初始化模态对话框
        _errorDialog = new AcceptDialog();
        _errorDialog.Title = "错误";
        _errorDialog.DialogText = "";
        _errorDialog.Exclusive = true;
        if (mainTheme != null) _errorDialog.Theme = mainTheme;
        AddChild(_errorDialog);

        // 2. 初始化 Toast 容器
        _toastContainer = new PanelContainer();
        if (mainTheme != null) _toastContainer.Theme = mainTheme;
        
        // Toast样式
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        styleBox.CornerRadiusTopLeft = 8;
        styleBox.CornerRadiusTopRight = 8;
        styleBox.CornerRadiusBottomLeft = 8;
        styleBox.CornerRadiusBottomRight = 8;
        styleBox.ContentMarginLeft = 16;
        styleBox.ContentMarginRight = 16;
        styleBox.ContentMarginTop = 8;
        styleBox.ContentMarginBottom = 8;
        _toastContainer.AddThemeStyleboxOverride("panel", styleBox);

        _toastLabel = new Label();
        _toastLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _toastLabel.VerticalAlignment = VerticalAlignment.Center;
        _toastLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
        _toastContainer.AddChild(_toastLabel);

        // Toast 初始不可见
        _toastContainer.Modulate = new Color(1, 1, 1, 0);
        _toastContainer.Visible = false;
        
        AddChild(_toastContainer);

        // 3. 初始化 Timer
        _toastTimer = new Godot.Timer();
        _toastTimer.OneShot = true;
        _toastTimer.Timeout += OnToastTimeout;
        AddChild(_toastTimer);
    }

    public void ShowErrorDialog(string title, string message)
    {
        // 打印到控制台，作为日志备份
        GD.PrintErr($"[ModalError] {title}: {message}");
        
        _errorDialog.Title = title;
        _errorDialog.DialogText = message;
        _errorDialog.PopupCentered();
    }

    public void ShowToast(string message, float duration = 3.0f)
    {
        // 打印到控制台，作为日志备份
        GD.PrintErr($"[ToastError] {message}");
        
        _toastQueue.Enqueue(message);
        _toastDuration = duration;
        
        if (!_isToastShowing)
        {
            ShowNextToast();
        }
    }

    private void ShowNextToast()
    {
        if (_toastQueue.Count == 0)
        {
            _isToastShowing = false;
            return;
        }

        _isToastShowing = true;
        string msg = _toastQueue.Dequeue();
        _toastLabel.Text = msg;
        
        // 强制更新布局以获取真实大小
        _toastContainer.ForceUpdateTransform();
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 toastSize = _toastContainer.GetMinimumSize();
        
        // 居中靠下
        _toastContainer.Position = new Vector2((viewportSize.X - toastSize.X) / 2, viewportSize.Y - toastSize.Y - 100);

        _toastContainer.Visible = true;
        _toastContainer.Modulate = new Color(1, 1, 1, 0);

        _toastTween?.Kill();
        _toastTween = CreateTween();
        
        // 淡入
        _toastTween.TweenProperty(_toastContainer, "modulate:a", 1.0f, 0.3f);
        _toastTween.TweenCallback(Callable.From(() => _toastTimer.Start(_toastDuration)));
    }

    private void OnToastTimeout()
    {
        _toastTween?.Kill();
        _toastTween = CreateTween();
        
        // 淡出
        _toastTween.TweenProperty(_toastContainer, "modulate:a", 0.0f, 0.3f);
        _toastTween.TweenCallback(Callable.From(() => {
            _toastContainer.Visible = false;
            ShowNextToast();
        }));
    }
}
