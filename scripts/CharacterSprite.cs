using Godot;
using System.Collections.Generic;

public partial class CharacterSprite : Control
{
    private TextureRect _textureRect;
    public int CurrentCharacterId { get; private set; } = -1;
    public SpriteNodeData SourceData { get; set; } // 供预览回写数据

    public override void _Ready()
    {
        _textureRect = new TextureRect {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _textureRect.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_textureRect);
    }

    public void AdjustScale(float delta)
    {
        if (SourceData == null) return;
        SourceData.Scale = Mathf.Clamp(SourceData.Scale + delta, 0.1f, 5.0f);
        ApplyTransform();
    }

    public void ToggleFlip()
    {
        if (SourceData == null) return;
        SourceData.FlipH = !SourceData.FlipH;
        ApplyTransform();
    }

    public void ApplyDrag(Vector2 globalPos)
    {
        if (SourceData == null) return;
        // 计算相对于默认位置的偏移
        // 因为 UpdateSpritePosition 会设置 Position, 所以这里的拖拽偏移量需要叠加
        SourceData.OffsetX += globalPos.X - GlobalPosition.X;
        SourceData.OffsetY += globalPos.Y - GlobalPosition.Y;
        GlobalPosition = globalPos;
    }

    public void ApplyTransform()
    {
        if (SourceData == null) return;
        
        PivotOffset = Size / 2; // 确保中心缩放
        Scale = new Vector2(SourceData.Scale, SourceData.Scale);
        _textureRect.FlipH = SourceData.FlipH;
    }

    public void UpdateCharacter(int charId, string emotion = "Neutral", bool silhouette = false)
    {
        CurrentCharacterId = charId;
        var charData = CharacterManager.Characters.Find(c => c.Id == charId);
        if (charData == null) {
            _textureRect.Texture = null;
            return;
        }

        string fileName = charData.DefaultSprite;
        if (!string.IsNullOrEmpty(emotion) && charData.Expressions.ContainsKey(emotion))
        {
            fileName = charData.Expressions[emotion];
        }

        if (string.IsNullOrEmpty(fileName)) {
            _textureRect.Texture = null;
            return;
        }

        string path = ProjectManager.IsProjectOpened ? 
            System.IO.Path.Combine(ProjectManager.SpriteDir, fileName) : 
            "res://sprites/" + fileName;

        GD.Print($"[Sprite] Attempting to load from: {path}");

        if (Godot.FileAccess.FileExists(path))
        {
            using var fileAccess = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            byte[] data = fileAccess.GetBuffer((long)fileAccess.GetLength());
            var image = new Image();
            var error = image.LoadPngFromBuffer(data);
            if (error != Error.Ok) error = image.LoadJpgFromBuffer(data);
            if (error != Error.Ok) error = image.LoadWebpFromBuffer(data);

            if (error == Error.Ok)
            {
                var texture = ImageTexture.CreateFromImage(image);
                _textureRect.Texture = texture;
                _textureRect.SelfModulate = silhouette ? new Color(0, 0, 0, 1) : new Color(1, 1, 1, 1);
                GD.Print($"[Sprite] Loaded character {charId} successfully");
                
                ApplyTransform();
            }
            else
            {
                GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast($"[Sprite] Failed to load buffer from {path}, Error: {error}");
            }
        }
        else
        {
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast($"[Sprite] File not found: {path}");
        }
    }
}
