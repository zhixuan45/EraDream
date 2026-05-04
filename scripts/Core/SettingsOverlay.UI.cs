using Godot;
using System;

public partial class SettingsOverlay
{
    private Control _overlayRoot;
    private CheckButton _darkModeToggle;
    private CheckButton _embeddedWindowToggle;
    private CheckButton _mouseCursorToggle;
    private HSlider _safeAreaSlider;
    private ColorRect _safeAreaPreview;


    private void OnMouseCursorToggled(bool isToggled)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ShowMouseCursor = isToggled;
        }
    }

    private void OnSafeAreaSliderChanged(double value)
    {
        if (SettingsManager.Instance != null)
        {
            float padding = (float)value;
            SettingsManager.Instance.SafeAreaPadding = padding;
            UpdatePreview(padding);
        }
    }

    private void UpdatePreview(float padding)
    {
        if (_safeAreaPreview != null)
        {
            // 使用 Offset 来调整 Margin
            _safeAreaPreview.OffsetLeft = padding;
            _safeAreaPreview.OffsetTop = padding;
            _safeAreaPreview.OffsetRight = -padding;
            _safeAreaPreview.OffsetBottom = -padding;
        }
    }

    private HBoxContainer CreateSettingRow(string labelText, out CheckButton toggle, Theme theme = null)
    {
        var hbox = new HBoxContainer();
        var label = new Label();
        label.Text = labelText;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.AddThemeColorOverride("font_color", new Color(1, 1, 1));
        hbox.AddChild(label);

        toggle = new CheckButton();
        if (theme != null) toggle.Theme = theme;
        hbox.AddChild(toggle);
        return hbox;
    }
}
