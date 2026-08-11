using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using EraDream.Core;
using EraDream.StoryEditor;
using EraDream.StoryEditor.Nodes;

/// <summary>
/// 剧情播放器主流程。演出画布、触控编辑与扩展节点实现在同名 partial 文件中。
/// </summary>
public partial class StoryPlayerEngine : Control
{
    private const string NameLabelPath = "UI_Layer/SafeAreaAdapter/Control_Root/DialogueBox/NameLabel";
    private const string ContentLabelPath = "UI_Layer/SafeAreaAdapter/Control_Root/DialogueBox/ContentLabel";
    private const string ChoiceContainerPath = "UI_Layer/SafeAreaAdapter/Control_Root/ChoiceContainer";
    private const string DialogueBoxPath = "UI_Layer/SafeAreaAdapter/Control_Root/DialogueBox";
    private const string InteractButtonPath = "UI_Layer/InteractButton";
    private const string AutoPlayButtonPath = "UI_Layer/AutoPlayButton";
    private const string BackgroundPath = "Background";
    private const string OverlayPath = "ColorRectOverlay";

    private Label _nameLabel;
    private RichTextLabel _contentLabel;
    private VBoxContainer _choiceContainer;
    private Control _dialogueBox;
    private Button _interactButton;
    private Button _autoPlayButton;
    private TextureRect _backgroundRect;
    private Control _overlay;
    private AudioStreamPlayer _bgmPlayer;
    private Control _characterContainer;

    private readonly Dictionary<string, CharacterSprite> _activeSprites = new();
    private readonly Dictionary<int, CharacterSprite> _activeStickerSprites = new();
    private List<BaseNodeData> _storyNodes = new();
    private readonly Dictionary<string, BaseNodeData> _nodeMap = new();
    private BaseNodeData _currentNode;
    private string _nextNonVisualNodeId = "";
    private string _currentBgmPath = "";
    private bool _isTextAnimating;
    private bool _isAutoPlayEnabled;
    private bool _autoPlayUserOverride;
    private Tween _textTween;

    public static string CurrentStoryPath = "";
    public static List<BaseNodeData> PreviewNodes;
    public static string StartNodeId;
    public static bool EnableVisualEditing;
    public static string ReturnScenePath = "res://scenes/MainMenuScreen.tscn";
    public bool IsPreviewMode { get; set; }

    [Signal] public delegate void StoryFinishedEventHandler();

    public override void _Ready()
    {
        _nameLabel = GetNodeOrNull<Label>(NameLabelPath);
        _contentLabel = GetNodeOrNull<RichTextLabel>(ContentLabelPath);
        _choiceContainer = GetNodeOrNull<VBoxContainer>(ChoiceContainerPath);
        _dialogueBox = GetNodeOrNull<Control>(DialogueBoxPath);
        _interactButton = GetNodeOrNull<Button>(InteractButtonPath);
        _autoPlayButton = GetNodeOrNull<Button>(AutoPlayButtonPath);
        _backgroundRect = GetNodeOrNull<TextureRect>(BackgroundPath);
        _overlay = GetNodeOrNull<Control>(OverlayPath);

        ConfigureExistingNodes();
        InitializePresentationSurface();
        InitializePlaybackTools();

        if (_interactButton != null)
            _interactButton.Pressed += OnInteraction;
        if (_autoPlayButton != null)
        {
            _autoPlayButton.Pressed += ToggleAutoPlay;
            UpdateAutoPlayButton();
        }

        if (PreviewNodes != null && PreviewNodes.Count > 0)
        {
            IsPreviewMode = true;
            _storyNodes = PreviewNodes;
            BuildNodeMap();
            _currentNode = !string.IsNullOrEmpty(StartNodeId) && _nodeMap.TryGetValue(StartNodeId, out var start)
                ? start : _storyNodes.FirstOrDefault(n => n is StartNodeData) ?? _storyNodes[0];
            PreviewNodes = null;
            ProcessCurrentNode();
            if (EnableVisualEditing)
                EnableVisualEditMode();
            return;
        }

        if (string.IsNullOrEmpty(CurrentStoryPath))
        {
            ErrorNotifier.Instance?.ShowErrorDialog("加载失败", "剧情路径为空。");
            GetTree().ChangeSceneToFile(ReturnScenePath);
            return;
        }
        LoadStory(CurrentStoryPath);
    }

    public override void _ExitTree()
    {
        if (_interactButton != null)
            _interactButton.Pressed -= OnInteraction;
        if (_autoPlayButton != null)
            _autoPlayButton.Pressed -= ToggleAutoPlay;
        _textTween?.Kill();
        CancelAutoAdvance();
        StopPresentationTweens();
    }

    private void ConfigureExistingNodes()
    {
        if (_backgroundRect != null)
        {
            _backgroundRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _backgroundRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
            _backgroundRect.MouseFilter = MouseFilterEnum.Ignore;
        }
        if (_overlay != null)
        {
            _overlay.MouseFilter = MouseFilterEnum.Ignore;
            var shader = ResourceProxy.LoadBlurOverlayShader();
            if (shader != null)
            {
                var material = new ShaderMaterial { Shader = shader };
                material.SetShaderParameter("color_over", Colors.Black);
                material.SetShaderParameter("blur_amount", 0f);
                material.SetShaderParameter("mix_amount", 0f);
                _overlay.Material = material;
            }
        }
        if (_choiceContainer != null)
            _choiceContainer.MouseFilter = MouseFilterEnum.Pass;

        _characterContainer = new Control { Name = "CharacterContainer", MouseFilter = MouseFilterEnum.Ignore };
        _characterContainer.SetAnchorsPreset(LayoutPreset.FullRect);
    }

    private void BuildNodeMap()
    {
        _nodeMap.Clear();
        foreach (var node in _storyNodes.Where(n => n != null && !string.IsNullOrEmpty(n.Id)))
            _nodeMap[node.Id] = node;
    }

    private void LoadStory(string path)
    {
        _storyNodes = StoryNodeManager.LoadProject(path);
        BuildNodeMap();
        if (_storyNodes.Count == 0)
        {
            ErrorNotifier.Instance?.ShowErrorDialog("无法加载剧情", "剧情文件为空或格式错误。");
            GetTree().ChangeSceneToFile(ReturnScenePath);
            return;
        }
        _currentNode = _storyNodes.FirstOrDefault(n => n is StartNodeData) ?? _storyNodes[0];
        ProcessCurrentNode();
    }

    private void ProcessCurrentNode()
    {
        if (_currentNode == null) { FinishStory(); return; }
        CancelAutoAdvance();
        ClearChoiceButtons();
        _choiceContainer?.Hide();
        _dialogueBox?.Show();
        if (_interactButton != null)
            _interactButton.Visible = _currentNode is DialogueNodeData || _currentNode is NarrativeNodeData;

        bool pauseForEdit = EnableVisualEditing && _currentNode.Id == StartNodeId;
        try
        {
            switch (_currentNode)
            {
                case StartNodeData node: AdvanceOrPause(node.NextNodeId, pauseForEdit); break;
                case EndNodeData node: FinishStory(node.EndType); break;
                case BackgroundNodeData node:
                    UpdateBackground(node.BackgroundFile, node.TransitionType, node);
                    AdvanceOrPause(node.NextNodeId, pauseForEdit);
                    break;
                case SpriteNodeData node:
                    HandleSpriteNode(node);
                    AdvanceOrPause(node.NextNodeId, pauseForEdit);
                    break;
                case StickerNodeData node:
                    HandleStickerNode(node);
                    AdvanceOrPause(node.NextNodeId, pauseForEdit);
                    break;
                case MusicNodeData node:
                    PlayBGM(node.AudioFile, GetFloatProperty(node, "Volume", 1f));
                    AdvanceOrPause(node.NextNodeId, pauseForEdit);
                    break;
                case DialogueNodeData node:
                    UpdateDialogueAndCharacter(node);
                    PlayVoice(node.VoiceFile);
                    PlaySfx(node.SoundEffectFile, node.SoundEffectVolume, false, null);
                    _nextNonVisualNodeId = PreExecuteVisualNodes(node.NextNodeId);
                    break;
                case NarrativeNodeData node:
                    ApplyVisualEffects(node.BlurValue, node.Darkness);
                    UpdateDialogueUI("", node.Content, node);
                    PlaySfx(node.SoundEffectFile, node.SoundEffectVolume, false, null);
                    break;
                case ChoiceNodeData node: ShowChoiceButtons(node); break;
                case ValueNodeData node:
                    HandleValueNode(node);
                    GoToNextNode(node.NextNodeId);
                    break;
                case BranchNodeData node: HandleBranchNode(node); break;
                default:
                    if (!TryHandleExtensionNode(_currentNode, pauseForEdit))
                        GoToNextNode(_currentNode.NextNodeId);
                    break;
            }
        }
        catch (Exception ex) { GD.PushError($"[StoryPlayerEngine] 节点执行失败: {ex}"); }
    }

    private void AdvanceOrPause(string nextNodeId, bool pauseForEdit)
    {
        if (!pauseForEdit) GoToNextNode(nextNodeId);
    }

    private void ApplyVisualEffects(float blur, float darkness)
    {
        if (_overlay?.Material is not ShaderMaterial material) return;
        var tween = CreateTween().SetParallel();
        tween.TweenMethod(Callable.From((float value) => material.SetShaderParameter("blur_amount", value)), (float)material.GetShaderParameter("blur_amount"), blur, 0.3f);
        tween.TweenMethod(Callable.From((float value) => material.SetShaderParameter("mix_amount", value)), (float)material.GetShaderParameter("mix_amount"), darkness, 0.3f);
    }

    private void UpdateDialogueAndCharacter(DialogueNodeData dialogue)
    {
        string name = string.IsNullOrEmpty(dialogue.CharacterId) ? "..." : GetCharacterName(dialogue.CharacterId);
        UpdateDialogueUI(name, dialogue.Content, dialogue);
        if (_activeSprites.TryGetValue(dialogue.CharacterId, out var sprite))
            sprite.UpdateCharacter(dialogue.CharacterId, dialogue.Emotion);
    }

    private void UpdateDialogueUI(string name, string content, object nodeData)
    {
        if (_nameLabel != null) _nameLabel.Text = LocalTr(name);
        if (_contentLabel == null) return;
        ApplyNodeFont(nodeData);
        string text = LocalTr(content);
        _contentLabel.Text = text;
        _contentLabel.VisibleRatio = 0;
        _textTween?.Kill();
        _isTextAnimating = true;
        float nodeSpeed = GetFloatProperty(nodeData, "TypingCharsPerSecond", GetFloatProperty(nodeData, "TypewriterSpeed", 0f));
        float speed = nodeSpeed > 0f ? nodeSpeed : ProjectManager.Metadata.DefaultTypewriterSpeed;
        float duration = text.Length == 0 ? 0 : text.Length / Mathf.Max(speed, 1f);
        _textTween = CreateTween();
        _textTween.TweenProperty(_contentLabel, "visible_ratio", 1f, duration);
        _textTween.Finished += () => { _isTextAnimating = false; ScheduleAutoAdvance(nodeData); };
    }

    private void HandleSpriteNode(SpriteNodeData data)
    {
        if (data == null || string.IsNullOrEmpty(data.CharacterId)) return;
        if (data.ActionType == "Hide")
        {
            if (_activeSprites.Remove(data.CharacterId, out var oldSprite)) oldSprite.QueueFree();
            return;
        }
        if (!_activeSprites.TryGetValue(data.CharacterId, out var sprite))
        {
            sprite = new CharacterSprite();
            _characterContainer.AddChild(sprite);
            _activeSprites[data.CharacterId] = sprite;
        }
        sprite.SourceData = data;
        sprite.UpdateCharacter(data.CharacterId, data.Expression, data.IsSilhouette);
        UpdateSpritePosition(sprite, data.Position);
    }

    private void HandleStickerNode(StickerNodeData data)
    {
        if (data == null) return;
        if (data.ActionType == "Hide")
        {
            if (_activeStickerSprites.Remove(data.StickerId, out var oldSprite)) oldSprite.QueueFree();
            return;
        }
        var sticker = StickerManager.Stickers.Find(item => item.Id == data.StickerId);
        if (sticker == null || string.IsNullOrEmpty(sticker.ImageFile)) return;
        if (!_activeStickerSprites.TryGetValue(data.StickerId, out var sprite))
        {
            sprite = new CharacterSprite();
            _characterContainer.AddChild(sprite);
            _activeStickerSprites[data.StickerId] = sprite;
        }
        sprite.SourceData = new SpriteNodeData { OffsetX = data.OffsetX, OffsetY = data.OffsetY, Scale = data.Scale, FlipH = data.FlipH };
        sprite.Size = new Vector2(400, 400);
        sprite.Position = new Vector2((DesignSize.X - 400) / 2 + data.OffsetX, (DesignSize.Y - 400) / 2 + data.OffsetY);
        sprite.UpdateTextureDirect(sticker.ImageFile);
    }

    private void UpdateSpritePosition(CharacterSprite sprite, string position)
    {
        Vector2 size = new(600, 800);
        sprite.Size = size;
        float x = position switch { "Left" => DesignSize.X * .25f - size.X / 2, "Right" => DesignSize.X * .75f - size.X / 2, _ => (DesignSize.X - size.X) / 2 };
        sprite.Position = new Vector2(x + sprite.SourceData.OffsetX, DesignSize.Y - size.Y + sprite.SourceData.OffsetY);
        sprite.ApplyTransform();
    }

    private void PlayBGM(string file, float volume = 1f)
    {
        if (_bgmPlayer == null) return;
        _bgmPlayer.VolumeDb = LinearToDb(Mathf.Clamp(volume, 0f, 1f));
        if (string.IsNullOrEmpty(file)) { _bgmPlayer.Stop(); _currentBgmPath = ""; return; }
        if (_currentBgmPath == file) return;
        var stream = ResourceProxy.LoadAudioFromProject(file);
        if (stream == null) { GD.PushWarning($"[StoryPlayerEngine] BGM 无法加载: {file}"); return; }
        _currentBgmPath = file;
        _bgmPlayer.Stream = stream;
        _bgmPlayer.Play();
    }

    private void HandleValueNode(ValueNodeData data)
    {
        var manager = EraDream.Game.GameManager.Instance;
        if (manager?.CurrentState == null) return;
        var state = manager.CurrentState;
        switch (data.TargetAttribute)
        {
            case "Money": state.Player.Money += data.ChangeValue; break;
            case "Vitality": state.Player.AddStamina(data.ChangeValue); break;
            case "Energy": state.Player.AddEnergy(data.ChangeValue); break;
            case "Speed": state.Uma.AddStat(EraDream.Game.StatType.Speed, data.ChangeValue); break;
            case "Stamina": state.Uma.AddStat(EraDream.Game.StatType.Stamina, data.ChangeValue); break;
            case "Power": state.Uma.AddStat(EraDream.Game.StatType.Power, data.ChangeValue); break;
            case "Guts": state.Uma.AddStat(EraDream.Game.StatType.Guts, data.ChangeValue); break;
            case "Intelligence": state.Uma.AddStat(EraDream.Game.StatType.Intelligence, data.ChangeValue); break;
            case "SkillPoints": state.Uma.SkillPoints += data.ChangeValue; break;
            case "Affection": state.Uma.Affection += data.ChangeValue; break;
            case "Custom":
                if (!string.IsNullOrWhiteSpace(data.CustomId)) state.Uma.AddCustomStat(data.CustomId, data.ChangeValue);
                break;
        }
        manager.MarkSaveDirty("story value changed");
    }

    private void ShowChoiceButtons(ChoiceNodeData choice)
    {
        // 选项需要玩家明确选择，进入时终止自动播放。
        SetAutoPlayEnabled(false);
        _dialogueBox?.Hide();
        _choiceContainer?.Show();
        if (_interactButton != null) _interactButton.Hide();
        foreach (var option in choice.Options)
        {
            var button = new Button { Text = LocalTr(option.Text), CustomMinimumSize = new Vector2(300, 50) };
            button.Pressed += () => GoToNextNode(option.TargetNodeId);
            _choiceContainer?.AddChild(button);
        }
    }

    private void ClearChoiceButtons()
    {
        if (_choiceContainer == null) return;
        foreach (var child in _choiceContainer.GetChildren()) child.QueueFree();
    }

    private void HandleBranchNode(BranchNodeData branch)
    {
        float value = GlobalGameState.Instance?.GetVariable(branch.VariableId) ?? 0;
        float.TryParse(branch.ComparisonValue, out float threshold);
        GoToNextNode(value >= threshold ? branch.SuccessNodeId : branch.FailNodeId);
    }

    private void OnInteraction()
    {
        if (_isTextAnimating)
        {
            _textTween?.Kill();
            _contentLabel.VisibleRatio = 1;
            _isTextAnimating = false;
            ScheduleAutoAdvance(_currentNode);
            return;
        }
        CancelAutoAdvance();
        if (_currentNode is DialogueNodeData) GoToNextNode(_nextNonVisualNodeId);
        else if (_currentNode is NarrativeNodeData) GoToNextNode(_currentNode.NextNodeId);
    }

    private void ToggleAutoPlay()
    {
        SetAutoPlayEnabled(!_isAutoPlayEnabled);
    }

    private void SetAutoPlayEnabled(bool enabled)
    {
        // 用户操作优先于项目旧配置，确保“自动：关”不会被旧配置重新打开。
        _autoPlayUserOverride = true;
        _isAutoPlayEnabled = enabled;
        UpdateAutoPlayButton();
        CancelAutoAdvance();
        if (_isAutoPlayEnabled && !_isTextAnimating)
            ScheduleAutoAdvance(_currentNode);
    }

    private void UpdateAutoPlayButton()
    {
        if (_autoPlayButton == null) return;
        _autoPlayButton.Text = _isAutoPlayEnabled ? "自动：开" : "自动：关";
    }

    private void GoToNextNode(string nodeId)
    {
        _currentNode = !string.IsNullOrEmpty(nodeId) && _nodeMap.TryGetValue(nodeId, out var node) ? node : null;
        ProcessCurrentNode();
    }

    private string PreExecuteVisualNodes(string nextId)
    {
        string id = nextId;
        while (!string.IsNullOrEmpty(id) && _nodeMap.TryGetValue(id, out var node))
        {
            switch (node)
            {
                case SpriteNodeData value: HandleSpriteNode(value); id = value.NextNodeId; continue;
                case StickerNodeData value: HandleStickerNode(value); id = value.NextNodeId; continue;
                case BackgroundNodeData value: UpdateBackground(value.BackgroundFile, value.TransitionType, value); id = value.NextNodeId; continue;
                case MusicNodeData value: PlayBGM(value.AudioFile, GetFloatProperty(value, "Volume", 1f)); id = value.NextNodeId; continue;
            }
            if (TryExecuteExtensionNode(node, out string nextNodeId)) { id = nextNodeId; continue; }
            break;
        }
        return id;
    }

    private void FinishStory(string type = "Title")
    {
        if (IsPreviewMode) { EmitSignal(SignalName.StoryFinished); return; }
        GetTree().ChangeSceneToFile(ReturnScenePath);
    }

    private string GetCharacterName(string actorId) => CharacterManager.GetActor(actorId)?.DisplayName ?? "...";
    private string LocalTr(string key) => string.IsNullOrEmpty(key) ? key : Tr(key);
}
