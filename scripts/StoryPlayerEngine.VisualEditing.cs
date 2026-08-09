using Godot;
using System.Collections.Generic;

public partial class StoryPlayerEngine
{
    private CharacterSprite _draggedSprite;
    private Vector2 _dragOffset;
    private bool _draggingBackground;
    private readonly Dictionary<int, Vector2> _touchPositions = new();
    private float _lastPinchDistance;
    private Label _editHint;
    private Button _editDoneButton;

    private void EnableVisualEditMode()
    {
        if (_interactButton != null) _interactButton.MouseFilter = MouseFilterEnum.Ignore;
        _dialogueBox?.Hide();

        bool isMobile = OS.HasFeature("mobile");
        _editHint = new Label
        {
            Text = isMobile
                ? "编辑：单指拖动定位 · 双指缩放 · 点击完成编辑"
                : "编辑：拖动定位 · 滚轮缩放 · 右键结束编辑",
            Position = new Vector2(16, 16),
            Modulate = Colors.White,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _designCanvas.AddChild(_editHint);

        if (isMobile)
        {
            _editDoneButton = new Button
            {
                Text = "完成编辑",
                CustomMinimumSize = new Vector2(160, 64),
                Position = new Vector2(DesignSize.X - 176, 16)
            };
            _editDoneButton.Pressed += CompleteVisualEditing;
            _designCanvas.AddChild(_editDoneButton);
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (IsPreviewMode && inputEvent is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
        {
            GetViewport().SetInputAsHandled();
            FinishStory();
            return;
        }
        if (!IsPreviewMode || !EnableVisualEditing) return;
        if (inputEvent is InputEventScreenTouch || inputEvent is InputEventScreenDrag)
        {
            HandleTouchVisualEditing(inputEvent);
            return;
        }
        if (inputEvent is InputEventMouseButton || inputEvent is InputEventMouseMotion)
            HandleDesktopVisualEditing(inputEvent);
    }

    private void HandleDesktopVisualEditing(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton button)
        {
            Vector2 design = ToDesignPosition(button.Position);
            var target = GetEditableSprite(design);
            if (button.ButtonIndex == MouseButton.Left)
            {
                if (button.Pressed)
                {
                    _draggedSprite = target;
                    _draggingBackground = target == null && _activeBackgroundData != null;
                    _dragOffset = target == null ? Vector2.Zero : target.Position - design;
                }
                else { _draggedSprite = null; _draggingBackground = false; }
            }
            else if (button.Pressed && button.ButtonIndex == MouseButton.Right)
            {
                CompleteVisualEditing();
                GetViewport().SetInputAsHandled();
                return;
            }
            else if (button.Pressed && (button.ButtonIndex == MouseButton.WheelUp || button.ButtonIndex == MouseButton.WheelDown))
            {
                float delta = button.ButtonIndex == MouseButton.WheelUp ? .05f : -.05f;
                if (target != null) target.AdjustScale(delta); else AdjustBackgroundScale(delta);
            }
            GetViewport().SetInputAsHandled();
        }
        else if (inputEvent is InputEventMouseMotion motion && _draggedSprite != null)
        {
            _draggedSprite.ApplyDrag(ToDesignPosition(motion.Position) + _dragOffset);
            GetViewport().SetInputAsHandled();
        }
        else if (inputEvent is InputEventMouseMotion backgroundMotion && _draggingBackground)
        {
            ApplyBackgroundDrag(backgroundMotion.Relative / Mathf.Max(_designCanvas.Scale.X, .001f));
            GetViewport().SetInputAsHandled();
        }
    }

    private void HandleTouchVisualEditing(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventScreenTouch touch:
                if (touch.Pressed) _touchPositions[touch.Index] = ToDesignPosition(touch.Position);
                else
                {
                    _touchPositions.Remove(touch.Index);
                    _draggedSprite = null;
                    _draggingBackground = false;
                    _lastPinchDistance = 0;
                }
                if (touch.Pressed && _touchPositions.Count == 1)
                {
                    _draggedSprite = GetEditableSprite(ToDesignPosition(touch.Position));
                    _draggingBackground = _draggedSprite == null && _activeBackgroundData != null;
                    _dragOffset = _draggedSprite == null ? Vector2.Zero : _draggedSprite.Position - ToDesignPosition(touch.Position);
                }
                break;
            case InputEventScreenDrag drag:
                _touchPositions[drag.Index] = ToDesignPosition(drag.Position);
                if (_touchPositions.Count >= 2)
                {
                    var points = new List<Vector2>(_touchPositions.Values);
                    float distance = points[0].DistanceTo(points[1]);
                    if (_lastPinchDistance > 0)
                    {
                        float delta = (distance - _lastPinchDistance) / 180f;
                        if (_draggedSprite != null) _draggedSprite.AdjustScale(delta); else AdjustBackgroundScale(delta);
                    }
                    _lastPinchDistance = distance;
                }
                else if (_draggedSprite != null)
                    _draggedSprite.ApplyDrag(ToDesignPosition(drag.Position) + _dragOffset);
                else
                    ApplyBackgroundDrag(drag.Relative / Mathf.Max(_designCanvas.Scale.X, .001f));
                break;
        }
        GetViewport().SetInputAsHandled();
    }

    private CharacterSprite GetEditableSprite(Vector2 designPosition)
    {
        foreach (var sprite in _activeSprites.Values)
            if (sprite.GetRect().HasPoint(designPosition - sprite.Position)) return sprite;
        foreach (var sprite in _activeStickerSprites.Values)
            if (sprite.GetRect().HasPoint(designPosition - sprite.Position)) return sprite;
        return null;
    }

    private void ApplyBackgroundDrag(Vector2 delta)
    {
        if (_activeBackgroundData == null) return;
        SetFloatProperty(_activeBackgroundData, "OffsetX", GetFloatProperty(_activeBackgroundData, "OffsetX", 0) + delta.X);
        SetFloatProperty(_activeBackgroundData, "OffsetY", GetFloatProperty(_activeBackgroundData, "OffsetY", 0) + delta.Y);
        ApplyBackgroundTransform(_backgroundRect, _activeBackgroundData);
    }

    private void AdjustBackgroundScale(float delta)
    {
        if (_activeBackgroundData == null) return;
        SetFloatProperty(_activeBackgroundData, "Scale", Mathf.Clamp(GetFloatProperty(_activeBackgroundData, "Scale", 1) + delta, .1f, 5f));
        ApplyBackgroundTransform(_backgroundRect, _activeBackgroundData);
    }

    private void CompleteVisualEditing()
    {
        if (!IsPreviewMode || !EnableVisualEditing) return;
        EnableVisualEditing = false;
        FinishStory();
    }

    private static void SetFloatProperty(object source, string name, float value)
    {
        source?.GetType().GetProperty(name)?.SetValue(source, value);
    }
}
