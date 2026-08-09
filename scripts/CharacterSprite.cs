using Godot;
using EraDream.StoryEditor.Nodes;

/// <summary>设计画布内的角色或贴纸。变换数据始终使用 1280x720 逻辑坐标。</summary>
public partial class CharacterSprite : Control
{
    private TextureRect _textureRect;
    public string CurrentCharacterId { get; private set; } = "";
    public SpriteNodeData SourceData { get; set; }
    private string _currentEmotion = "";
    private bool _currentSilhouette;

    public override void _Ready()
    {
        _textureRect = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore };
        _textureRect.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_textureRect);
    }

    public void AdjustScale(float delta)
    {
        if (SourceData == null) return;
        SourceData.Scale = Mathf.Clamp(SourceData.Scale + delta, .1f, 5f);
        ApplyTransform();
    }

    public void ToggleFlip()
    {
        if (SourceData == null) return;
        SourceData.FlipH = !SourceData.FlipH;
        ApplyTransform();
    }

    public void ApplyDrag(Vector2 designPosition)
    {
        if (SourceData == null) return;
        Vector2 delta = designPosition - Position;
        SourceData.OffsetX += delta.X;
        SourceData.OffsetY += delta.Y;
        Position = designPosition;
    }

    public void ApplyTransform()
    {
        if (SourceData == null) return;
        PivotOffset = Size / 2;
        Scale = Vector2.One * SourceData.Scale;
        _textureRect.FlipH = SourceData.FlipH;
    }

    public void UpdateCharacter(string actorId, string emotion = "Neutral", bool silhouette = false)
    {
        var actor = CharacterManager.GetActor(actorId);
        if (actor == null) return;
        string file = !string.IsNullOrEmpty(emotion) && actor.Visuals.Expressions.ContainsKey(emotion) ? actor.Visuals.Expressions[emotion] : actor.Visuals.DefaultSprite;
        if (CurrentCharacterId == actorId && _currentEmotion == emotion && _currentSilhouette == silhouette) return;
        CurrentCharacterId = actorId;
        _currentEmotion = emotion;
        _currentSilhouette = silhouette;
        _textureRect.Texture = EraDream.Core.ResourceProxy.LoadSpriteTexture(file, actorId);
        _textureRect.SelfModulate = silhouette ? Colors.Black : Colors.White;
        ApplyTransform();
    }

    public void UpdateTextureDirect(string fileName)
    {
        _textureRect.Texture = EraDream.Core.ResourceProxy.LoadSpriteTexture(fileName);
        _textureRect.SelfModulate = Colors.White;
        ApplyTransform();
    }
}
