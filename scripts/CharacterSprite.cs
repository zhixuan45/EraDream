using Godot;
using System.Collections.Generic;

public partial class CharacterSprite : Control
{
    private TextureRect _textureRect;
    public string CurrentCharacterId { get; private set; } = "";
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

    public void UpdateCharacter(string actorId, string emotion = "Neutral", bool silhouette = false)
    {
        CurrentCharacterId = actorId;
        var actor = CharacterManager.GetActor(actorId);
        if (actor == null) {
            _textureRect.Texture = null;
            return;
        }

        string fileName = actor.Visuals.DefaultSprite;
        if (!string.IsNullOrEmpty(emotion) && actor.Visuals.Expressions.ContainsKey(emotion))
        {
            fileName = actor.Visuals.Expressions[emotion];
        }

        if (string.IsNullOrEmpty(fileName)) {
            _textureRect.Texture = null;
            return;
        }

        GD.Print($"[Sprite] Attempting to load sprite: {fileName}");

        ImageTexture texture = null;
        // 智能处理扩展包中的角色立绘：检查是否为注册激活的扩展包角色并拼接绝对路径
        if (CharacterManager.ActorToExtensionPathMap.TryGetValue(actorId, out string extRoot))
        {
            string absPath = System.IO.Path.Combine(extRoot, fileName);
            texture = UmaEraArchive.Core.ResourceProxy.LoadImageTexture(absPath);
        }
        else
        {
            texture = UmaEraArchive.Core.ResourceProxy.LoadSpriteTexture(fileName);
        }

        if (texture != null)
        {
            _textureRect.Texture = texture;
            _textureRect.SelfModulate = silhouette ? new Color(0, 0, 0, 1) : new Color(1, 1, 1, 1);
            GD.Print($"[Sprite] Loaded actor {actorId} successfully");
            ApplyTransform();
        }
        else
        {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"[Sprite] Failed to load/find sprite: {fileName}");
        }
    }

    /// <summary>
    /// 直接加载纹理（供贴纸节点使用，不走角色系统）
    /// </summary>
    public void UpdateTextureDirect(string fileName)
    {
        var texture = UmaEraArchive.Core.ResourceProxy.LoadSpriteTexture(fileName);
        if (texture != null)
        {
            _textureRect.Texture = texture;
            _textureRect.SelfModulate = new Color(1, 1, 1, 1);
            ApplyTransform();
        }
    }
}
