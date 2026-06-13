using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using EraDream.StoryEditor.Nodes;
using EraDream.Core;
using EraDream.StoryEditor;

public partial class StoryPlayerEngine : Control
{
    // 定义节点的硬编码路径常量，防止硬崩溃
    private const string NameLabelPath = "UI_Layer/SafeAreaAdapter/Control_Root/DialogueBox/NameLabel";
    private const string ContentLabelPath = "UI_Layer/SafeAreaAdapter/Control_Root/DialogueBox/ContentLabel";
    private const string ChoiceContainerPath = "UI_Layer/SafeAreaAdapter/Control_Root/ChoiceContainer";
    private const string DialogueBoxPath = "UI_Layer/SafeAreaAdapter/Control_Root/DialogueBox";
    private const string InteractButtonPath = "UI_Layer/InteractButton";
    private const string BackgroundPath = "Background";
    private const string OverlayPath = "ColorRectOverlay";

    private Label _nameLabel;
    private RichTextLabel _contentLabel;
    private VBoxContainer _choiceContainer;
    private Control _dialogueBox;
    private Button _interactButton;
    private TextureRect _backgroundRect;
    private AudioStreamPlayer _bgmPlayer;
    private Control _characterContainer;
    private Control _overlay;
    
    private Dictionary<string, CharacterSprite> _activeSprites = new Dictionary<string, CharacterSprite>();
    private Dictionary<int, CharacterSprite> _activeStickerSprites = new Dictionary<int, CharacterSprite>();

    private List<BaseNodeData> _storyNodes = new List<BaseNodeData>();
    private Dictionary<string, BaseNodeData> _nodeMap = new Dictionary<string, BaseNodeData>();
    private BaseNodeData _currentNode;
    private bool _isTextAnimating = false;
    private float _textSpeed = 0.05f;
    private Tween _textTween;

    private string _currentBgmPath = ""; // 缓存当前播放的 BGM 路径，防重复加载
    private string _nextNonVisualNodeId = ""; // 缓存预执行后第一个非视觉节点 ID

    public static string CurrentStoryPath = "";
    public static List<BaseNodeData> PreviewNodes = null;
    public static string StartNodeId = null;
    public static bool EnableVisualEditing = false;
    public static string ReturnScenePath = "res://scenes/MainMenuScreen.tscn";
    public bool IsPreviewMode { get; set; } = false;

    private CharacterSprite _draggedSprite = null;
    private Vector2 _dragOffset = Vector2.Zero;

    [Signal] public delegate void StoryFinishedEventHandler();

    // 在 LoadStory 和 Preview 模式中构建节点 O(1) 查找图
    private void BuildNodeMap()
    {
        _nodeMap.Clear();
        foreach (var n in _storyNodes)
        {
            if (n != null && !string.IsNullOrEmpty(n.Id))
                _nodeMap[n.Id] = n;
        }
    }

    public override void _Ready()
    {
        _nameLabel = GetNodeOrNull<Label>(NameLabelPath);
        if (_nameLabel == null) GD.PushError("[StoryPlayerEngine] NameLabel not found!");
        
        _contentLabel = GetNodeOrNull<RichTextLabel>(ContentLabelPath);
        if (_contentLabel == null) GD.PushError("[StoryPlayerEngine] ContentLabel not found!");
        
        _choiceContainer = GetNodeOrNull<VBoxContainer>(ChoiceContainerPath);
        if (_choiceContainer == null) GD.PushError("[StoryPlayerEngine] ChoiceContainer not found!");
        
        _dialogueBox = GetNodeOrNull<Control>(DialogueBoxPath);
        if (_dialogueBox == null) GD.PushError("[StoryPlayerEngine] DialogueBox not found!");
        
        _interactButton = GetNodeOrNull<Button>(InteractButtonPath);
        if (_interactButton == null) GD.PushError("[StoryPlayerEngine] InteractButton not found!");
        
        _backgroundRect = GetNodeOrNull<TextureRect>(BackgroundPath);
        if (_backgroundRect == null) GD.PushError("[StoryPlayerEngine] BackgroundRect not found!");
        
        _overlay = GetNodeOrNull<Control>(OverlayPath);
        if (_overlay == null) GD.PushError("[StoryPlayerEngine] Overlay not found!");

        if (_overlay != null)
        {
            _overlay.MouseFilter = MouseFilterEnum.Ignore; // 确保遮罩层不拦截点击
            var blurShader = EraDream.Core.ResourceProxy.LoadBlurOverlayShader();
            var mat = new ShaderMaterial();
            if (blurShader != null)
            {
                mat.Shader = blurShader;
                mat.SetShaderParameter("color_over", new Color(0, 0, 0, 1));
                mat.SetShaderParameter("blur_amount", 0.0f);
                mat.SetShaderParameter("mix_amount", 0.0f);
            }
            _overlay.Material = mat;
        }
        
        if (_backgroundRect != null)
        {
            _backgroundRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _backgroundRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
            _backgroundRect.SetAnchorsPreset(LayoutPreset.FullRect);
            _backgroundRect.MouseFilter = MouseFilterEnum.Ignore; // 确保背景不拦截点击
        }

        _characterContainer = new Control { Name = "CharacterContainer" };
        _characterContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _characterContainer.MouseFilter = Control.MouseFilterEnum.Ignore; // 立绘容器不拦截
        AddChild(_characterContainer);

        if (_backgroundRect != null && _overlay != null)
        {
            MoveChild(_backgroundRect, 0);
            MoveChild(_overlay, 1);
            MoveChild(_characterContainer, 2);
        }

        if (_choiceContainer != null)
        {
            _choiceContainer.MouseFilter = MouseFilterEnum.Pass; // 允许子按钮接收事件
        }
        
        _bgmPlayer = new AudioStreamPlayer();
        AddChild(_bgmPlayer);

        if (_interactButton != null)
        {
            _interactButton.Pressed += OnInteraction;
            _interactButton.MouseFilter = MouseFilterEnum.Pass; // 交互按钮默认 Pass
        }

        if (PreviewNodes != null && PreviewNodes.Count > 0)
        {
            GD.Print("[Engine] Entering Preview Mode...");
            IsPreviewMode = true;
            _storyNodes = PreviewNodes;
            BuildNodeMap();
            
            if (!string.IsNullOrEmpty(StartNodeId)) {
                _currentNode = _nodeMap.TryGetValue(StartNodeId, out var sn) ? sn : null;
            } else {
                _currentNode = _storyNodes.FirstOrDefault(n => n is StartNodeData) ?? _storyNodes[0];
            }
            
            ProcessCurrentNode();
            PreviewNodes = null;
            // 可视化编辑模式：禁用全屏交互按钮并隐藏对话框，避免遮挡立绘操作
            if (EnableVisualEditing && _interactButton != null && _dialogueBox != null)
            {
                _interactButton.MouseFilter = MouseFilterEnum.Ignore;
                _dialogueBox.Hide();
            }
            return;
        }

        if (string.IsNullOrEmpty(CurrentStoryPath))
        {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowErrorDialog("加载失败", "[Engine] Story path is empty!");
            GetTree().ChangeSceneToFile("res://scenes/MainMenuScreen.tscn");
            return;
        }

        LoadStory(CurrentStoryPath);
    }

    public override void _ExitTree()
    {
        // 退出树时释放点击订阅与文本补字 Tween
        if (_interactButton != null)
            _interactButton.Pressed -= OnInteraction;
        if (_textTween != null && _textTween.IsValid())
            _textTween.Kill();
    }

    private void LoadStory(string path)
    {
        _storyNodes = StoryNodeManager.LoadProject(path);
        BuildNodeMap();
        if (_storyNodes.Count == 0)
        {
            ErrorNotifier.Instance?.ShowErrorDialog("无法加载剧情", "文件为空或格式错误");
            GetTree().ChangeSceneToFile(ReturnScenePath);
            return;
        }
        _currentNode = _storyNodes.FirstOrDefault(n => n is StartNodeData) ?? _storyNodes[0];
        ProcessCurrentNode();
    }

    private void ProcessCurrentNode()
    {
        if (_currentNode == null) { FinishStory(); return; }

        GD.Print($"[Engine] Node: {_currentNode.GetType().Name} (ID: {_currentNode.Id})");

        if (_choiceContainer != null) _choiceContainer.Hide();
        if (_dialogueBox != null) _dialogueBox.Show();
        
        // 仅在对话与叙述节点显示全屏点击交互按钮
        if (_interactButton != null)
        {
            if (_currentNode is DialogueNodeData || _currentNode is NarrativeNodeData)
                _interactButton.Show();
            else
                _interactButton.Hide();
        }
        
        if (_choiceContainer != null)
        {
            foreach (Node child in _choiceContainer.GetChildren()) child.QueueFree();
        }

        // 默认重置滤镜，但如果是视觉/逻辑节点则保持当前模糊度与暗度状态
        if (_currentNode is NarrativeNodeData narrative) 
            ApplyVisualEffects(narrative.BlurValue, narrative.Darkness);
        else if (_currentNode is ChoiceNodeData choice) 
            ApplyVisualEffects(choice.BlurValue, choice.Darkness);
        else if (_currentNode is DialogueNodeData || _currentNode is SpriteNodeData 
                 || _currentNode is MusicNodeData || _currentNode is BackgroundNodeData 
                 || _currentNode is StickerNodeData || _currentNode is StartNodeData 
                 || _currentNode is EndNodeData || _currentNode is ValueNodeData 
                 || _currentNode is BranchNodeData)
        {
            // 维持上一节点滤镜状态，防视觉突变
        }
        else 
        {
            ApplyVisualEffects(0, 0);
        }

        // 可视化编辑模式拦截：如果当前节点是目标编辑节点，则执行完逻辑后停止，不自动跳转
        bool shouldPauseForEdit = EnableVisualEditing && _currentNode.Id == StartNodeId;

        try
        {
            switch (_currentNode)
            {
                case StartNodeData start: 
                    if (shouldPauseForEdit) return;
                    GoToNextNode(start.NextNodeId); 
                    break;
                case EndNodeData end: 
                    FinishStory(end.EndType); 
                    break;
                case BackgroundNodeData bg: 
                    UpdateBackground(bg.BackgroundFile, bg.TransitionType); 
                    if (shouldPauseForEdit) return;
                    GoToNextNode(bg.NextNodeId); 
                    break;
                case DialogueNodeData dialogue: 
                    UpdateDialogueAndCharacter(dialogue); 
                    _nextNonVisualNodeId = PreExecuteVisualNodes(dialogue.NextNodeId);
                    break;
                case NarrativeNodeData narrativeNode: 
                    UpdateDialogueUI("", narrativeNode.Content); 
                    // 叙述节点需等待点击才跳转，不做预执行
                    break;
                case MusicNodeData music: 
                    PlayBGM(music.AudioFile); 
                    if (shouldPauseForEdit) return;
                    GoToNextNode(music.NextNodeId); 
                    break;
                case SpriteNodeData sprite: 
                    HandleSpriteNode(sprite); 
                    if (shouldPauseForEdit) return;
                    GoToNextNode(sprite.NextNodeId); 
                    break;
                case StickerNodeData sticker:
                    HandleStickerNode(sticker);
                    if (shouldPauseForEdit) return;
                    GoToNextNode(sticker.NextNodeId);
                    break;
                case ChoiceNodeData choiceNode: 
                    ShowChoiceButtons(choiceNode); 
                    break;
                case ValueNodeData valueNode:
                    HandleValueNode(valueNode);
                    GoToNextNode(valueNode.NextNodeId);
                    break;
                case BranchNodeData branch: 
                    if (shouldPauseForEdit) return;
                    HandleBranchNode(branch); 
                    break;
            }
        }
        catch (Exception ex)
        {
            GD.PushError($"[StoryPlayerEngine] ProcessCurrentNode error: {ex.Message}");
        }
    }

    private void ApplyVisualEffects(float blur, float darkness)
    {
        if (_overlay.Material is ShaderMaterial mat)
        {
            // 使用 Tween 实现平滑过渡
            var tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenMethod(Callable.From((float v) => mat.SetShaderParameter("blur_amount", v)), (float)mat.GetShaderParameter("blur_amount"), blur, 0.5f);
            tween.TweenMethod(Callable.From((float v) => mat.SetShaderParameter("mix_amount", v)), (float)mat.GetShaderParameter("mix_amount"), darkness, 0.5f);
        }
    }

    private void HandleValueNode(ValueNodeData data)
    {
        var manager = EraDream.Game.GameManager.Instance;
        var errorNotifier = GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier");
        string storyId = System.IO.Path.GetFileNameWithoutExtension(CurrentStoryPath);
        string valueId = data.TargetAttribute == "Custom" ? data.CustomId : data.TargetAttribute;

        if (manager == null || manager.CurrentState == null)
        {
            errorNotifier?.ShowToast($"{storyId} 访问了一个意外的数值 {valueId}!");
            return;
        }

        var state = manager.CurrentState;
        bool success = true;

        try
        {
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
                    if (string.IsNullOrWhiteSpace(data.CustomId))
                    {
                        errorNotifier?.ShowToast($"{storyId} 访问了一个意外的数值 {valueId}!");
                        success = false;
                    }
                    else
                    {
                        // 优先保存到养成系统的持久化状态中
                        state.Uma.AddCustomStat(data.CustomId, data.ChangeValue);
                        
                        // 同时同步到全局状态，方便 BranchNode 等即时查询
                        var globalState = EraDream.Core.GlobalGameState.Instance;
                        if (globalState != null)
                        {
                            float current = globalState.GetVariable(data.CustomId);
                            globalState.SetVariable(data.CustomId, current + data.ChangeValue);
                        }
                    }
                    break;
                default:
                    errorNotifier?.ShowToast($"{storyId} 访问了一个意外的数值 {valueId}!");
                    success = false;
                    break;
            }
        }
        catch (Exception)
        {
            errorNotifier?.ShowToast($"{storyId} 访问了一个意外的数值 {valueId}!");
            success = false;
        }

        if (success)
        {
            GD.Print($"[Engine] Value Changed: {data.TargetAttribute} ({(data.TargetAttribute == "Custom" ? data.CustomId : "")}) by {data.ChangeValue}");
        }
    }

    private void UpdateDialogueAndCharacter(DialogueNodeData dialogue)
    {
        try
        {
            if (dialogue == null) return;
            // 防御角色 ID 为空，此时只更新对话文本
            if (string.IsNullOrEmpty(dialogue.CharacterId))
            {
                UpdateDialogueUI("...", dialogue.Content);
                return;
            }
            string actorName = GetCharacterName(dialogue.CharacterId);
            UpdateDialogueUI(actorName, dialogue.Content);
            if (_activeSprites.TryGetValue(dialogue.CharacterId, out var existingSprite))
            {
                existingSprite.UpdateCharacter(dialogue.CharacterId, dialogue.Emotion);
            }
        }
        catch (Exception ex)
        {
            GD.PushError($"[StoryPlayerEngine] UpdateDialogueAndCharacter error: {ex.Message}");
        }
    }

    private void HandleSpriteNode(SpriteNodeData data)
    {
        try
        {
            if (data == null || string.IsNullOrEmpty(data.CharacterId)) return;
            if (data.ActionType == "Hide")
            {
                if (_activeSprites.TryGetValue(data.CharacterId, out var s)) 
                { 
                    s.QueueFree(); 
                    _activeSprites.Remove(data.CharacterId); 
                }
                return;
            }
            if (!_activeSprites.TryGetValue(data.CharacterId, out var targetSprite))
            {
                targetSprite = new CharacterSprite();
                _characterContainer.AddChild(targetSprite);
                _activeSprites[data.CharacterId] = targetSprite;
            }
            targetSprite.SourceData = data; // 传递数据引用以供回写
            targetSprite.UpdateCharacter(data.CharacterId, data.Expression, data.IsSilhouette);
            UpdateSpritePosition(targetSprite, data.Position);
        }
        catch (Exception ex)
        {
            GD.PushError($"[StoryPlayerEngine] HandleSpriteNode error: {ex.Message}");
        }
    }

    // 处理贴纸节点：加载贴纸图片到场景
    private void HandleStickerNode(StickerNodeData data)
    {
        try
        {
            if (data == null) return;
            int stickerKey = data.StickerId; // 使用 StickerId 做键，无溢出与冲突风险
            if (data.ActionType == "Hide")
            {
                if (_activeStickerSprites.TryGetValue(stickerKey, out var s)) 
                { 
                    s.QueueFree(); 
                    _activeStickerSprites.Remove(stickerKey); 
                }
                return;
            }

            var stickerData = StickerManager.Stickers.Find(s => s.Id == data.StickerId);
            if (stickerData == null || string.IsNullOrEmpty(stickerData.ImageFile)) return;

            if (!_activeStickerSprites.TryGetValue(stickerKey, out var targetSprite))
            {
                targetSprite = new CharacterSprite();
                _characterContainer.AddChild(targetSprite);
                _activeStickerSprites[stickerKey] = targetSprite;
            }

            // 复用 SpriteNodeData 格式传递变换参数
            var proxyData = new SpriteNodeData {
                OffsetX = data.OffsetX, OffsetY = data.OffsetY,
                Scale = data.Scale, FlipH = data.FlipH
            };
            targetSprite.SourceData = proxyData;

            // 设置贴纸尺寸和位置，通过 UpdateTextureDirect 加载纹理
            targetSprite.Size = new Vector2(400, 400);
            targetSprite.Position = new Vector2(
                (Size.X - 400) / 2 + data.OffsetX,
                (Size.Y - 400) / 2 + data.OffsetY
            );
            targetSprite.UpdateTextureDirect(stickerData.ImageFile);
        }
        catch (Exception ex)
        {
            GD.PushError($"[StoryPlayerEngine] HandleStickerNode error: {ex.Message}");
        }
    }

    public override void _Input(InputEvent @event)
    {
        // 全局退出预览快捷键
        if (IsPreviewMode && @event is InputEventKey ek && ek.Pressed && ek.Keycode == Key.Escape)
        {
            GetViewport().SetInputAsHandled();
            FinishStory();
            return;
        }

        if (!IsPreviewMode || !EnableVisualEditing) return;

        // 仅在鼠标按键交互触发时分配列表，极大地减少 MouseMotion 分配开销
        if (@event is InputEventMouseButton mb)
        {
            var allSprites = new List<CharacterSprite>(_activeSprites.Values);
            allSprites.AddRange(_activeStickerSprites.Values);

            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed) {
                    foreach (var sprite in allSprites) {
                        if (sprite.GetGlobalRect().HasPoint(mb.GlobalPosition)) {
                            _draggedSprite = sprite;
                            _dragOffset = sprite.GlobalPosition - mb.GlobalPosition;
                            GetViewport().SetInputAsHandled();
                            break;
                        }
                    }
                } else {
                    _draggedSprite = null;
                }
            }
            else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed) {
                foreach (var sprite in allSprites) {
                    if (sprite.GetGlobalRect().HasPoint(mb.GlobalPosition)) {
                        sprite.ToggleFlip();
                        GetViewport().SetInputAsHandled();
                        break;
                    }
                }
            }
            else if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed) {
                foreach (var sprite in allSprites) {
                    if (sprite.GetGlobalRect().HasPoint(mb.GlobalPosition)) {
                        sprite.AdjustScale(0.05f);
                        GetViewport().SetInputAsHandled();
                        break;
                    }
                }
            }
            else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed) {
                foreach (var sprite in allSprites) {
                    if (sprite.GetGlobalRect().HasPoint(mb.GlobalPosition)) {
                        sprite.AdjustScale(-0.05f);
                        GetViewport().SetInputAsHandled();
                        break;
                    }
                }
            }
        }
        else if (@event is InputEventMouseMotion mm && _draggedSprite != null)
        {
            _draggedSprite.ApplyDrag(mm.GlobalPosition + _dragOffset);
            GetViewport().SetInputAsHandled();
        }
    }

    private void UpdateSpritePosition(CharacterSprite sprite, string position)
    {
        Vector2 size = new Vector2(600, 800); 
        sprite.Size = size;
        float xPos = position switch {
            "Left" => Size.X * 0.25f - size.X / 2,
            "Right" => Size.X * 0.75f - size.X / 2,
            _ => (Size.X - size.X) / 2
        };
        
        float offsetX = sprite.SourceData != null ? sprite.SourceData.OffsetX : 0;
        float offsetY = sprite.SourceData != null ? sprite.SourceData.OffsetY : 0;
        
        sprite.Position = new Vector2(xPos + offsetX, Size.Y - size.Y + offsetY);
    }

    private void UpdateBackground(string file, string transition)
    {
        try
        {
            if (string.IsNullOrEmpty(file)) return;
            
            GD.Print($"[Engine] Attempting to load background: {file}");

            var bgTexture = EraDream.Core.ResourceProxy.LoadBackgroundTexture(file);
            if (bgTexture != null)
            {
                _backgroundRect.Texture = bgTexture;
                _backgroundRect.Modulate = new Color(1, 1, 1, 1);
                GD.Print($"[Engine] Background Loaded Successfully: {file}");
            }
            else
            {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"[Engine] Background file NOT found/failed to load: {file}");
            }
        }
        catch (Exception ex)
        {
            GD.PushError($"[StoryPlayerEngine] UpdateBackground error: {ex.Message}");
        }
    }

    private void PlayBGM(string file)
    {
        try
        {
            if (string.IsNullOrEmpty(file)) 
            { 
                _bgmPlayer.Stop(); 
                _currentBgmPath = ""; 
                return; 
            }
            
            // 路径比对缓存，避免重复播放
            if (_currentBgmPath != file)
            {
                AudioStream stream = EraDream.Core.ResourceProxy.LoadAudioFromProject(file);
                if (stream != null)
                {
                    _currentBgmPath = file;
                    _bgmPlayer.Stream = stream;
                    _bgmPlayer.Play();
                }
                else
                {
                    GD.PushWarning($"[StoryPlayerEngine] BGM file not found/failed to load: {file}");
                }
            }
        }
        catch (Exception ex)
        {
            GD.PushError($"[StoryPlayerEngine] PlayBGM error: {ex.Message}");
        }
    }

    private void FinishStory(string type = "Title")
    {
        if (IsPreviewMode) { EmitSignal(SignalName.StoryFinished); return; }
        GetTree().ChangeSceneToFile(ReturnScenePath);
    }

    private string GetCharacterName(string actorId)
    {
        if (string.IsNullOrEmpty(actorId)) return "...";
        var actor = CharacterManager.GetActor(actorId);
        return actor != null ? LocalTr(actor.DisplayName) : "...";
    }

    private void UpdateDialogueUI(string name, string content)
    {
        _nameLabel.Text = LocalTr(name);
        string translatedContent = LocalTr(content);
        // 必须先赋值 Text，visible_ratio 动画才有效果
        _contentLabel.Text = translatedContent;
        _contentLabel.VisibleRatio = 0.0f;
        _isTextAnimating = true;
        if (_textTween != null) _textTween.Kill();
        _textTween = CreateTween();
        
        // 限制打字机动画最大时长为 2.0s，提升观感与防死锁
        float duration = Mathf.Min(translatedContent.Length * _textSpeed, 2.0f);
        _textTween.TweenProperty(_contentLabel, "visible_ratio", 1.0f, duration);
        _textTween.Finished += () => _isTextAnimating = false;
    }

    private void ShowChoiceButtons(ChoiceNodeData choice)
    {
        try
        {
            if (choice == null) return;
            if (_dialogueBox != null) _dialogueBox.Hide(); 
            if (_choiceContainer != null) _choiceContainer.Show();
            if (_interactButton != null) _interactButton.Hide(); // 隐藏全屏交互按钮，否则它会挡住选项按钮点击
            
            foreach (var option in choice.Options)
            {
                Button btn = new Button { Text = LocalTr(option.Text), CustomMinimumSize = new Vector2(300, 50) };
                btn.Pressed += () => GoToNextNode(option.TargetNodeId);
                if (_choiceContainer != null) _choiceContainer.AddChild(btn);
            }
        }
        catch (Exception ex)
        {
            GD.PushError($"[StoryPlayerEngine] ShowChoiceButtons error: {ex.Message}");
        }
    }

    private void HandleBranchNode(BranchNodeData branch)
    {
        try
        {
            if (branch == null) return;
            var gs = GlobalGameState.Instance;
            if (gs == null)
            {
                GD.PushError("[StoryPlayerEngine] GlobalGameState.Instance is null, branch condition cannot be evaluated");
                GoToNextNode(branch.FailNodeId);
                return;
            }
            float currentVal = gs.GetVariable(branch.VariableId);
            float threshold = 0;
            if (!string.IsNullOrEmpty(branch.ComparisonValue))
            {
                if (!float.TryParse(branch.ComparisonValue, out threshold))
                {
                    GD.PushWarning($"[StoryPlayerEngine] Failed to parse comparison value '{branch.ComparisonValue}' on branch '{branch.Id}', fallback to 0");
                }
            }
            GoToNextNode(currentVal >= threshold ? branch.SuccessNodeId : branch.FailNodeId);
        }
        catch (Exception ex)
        {
            GD.PushError($"[StoryPlayerEngine] HandleBranchNode error: {ex.Message}");
        }
    }

    private void OnInteraction()
    {
        if (_isTextAnimating) { _textTween.Stop(); _contentLabel.VisibleRatio = 1.0f; _isTextAnimating = false; return; }
        if (_currentNode is DialogueNodeData) 
            GoToNextNode(_nextNonVisualNodeId);
        else if (_currentNode is NarrativeNodeData) 
            GoToNextNode(_currentNode.NextNodeId);
    }

    private void GoToNextNode(string nextId)
    {
        _currentNode = string.IsNullOrEmpty(nextId) ? null : (_nodeMap.TryGetValue(nextId, out var n) ? n : null);
        ProcessCurrentNode();
    }

    private string PreExecuteVisualNodes(string nextId)
    {
        string currentCheckId = nextId;
        while (!string.IsNullOrEmpty(currentCheckId))
        {
            var node = _nodeMap.TryGetValue(currentCheckId, out var n) ? n : null;
            if (node == null) break;

            if (node is SpriteNodeData sprite)
            {
                HandleSpriteNode(sprite);
                currentCheckId = sprite.NextNodeId;
            }
            else if (node is BackgroundNodeData bg)
            {
                UpdateBackground(bg.BackgroundFile, bg.TransitionType);
                currentCheckId = bg.NextNodeId;
            }
            else if (node is MusicNodeData music)
            {
                PlayBGM(music.AudioFile);
                currentCheckId = music.NextNodeId;
            }
            else if (node is StickerNodeData sticker)
            {
                HandleStickerNode(sticker);
                currentCheckId = sticker.NextNodeId;
            }
            else
            {
                break; // Stop when hitting a blocking node (Dialogue, Choice, Branch, etc.)
            }
        }
        return currentCheckId;
    }

    private void LoadLocalTranslations(string storyPath) { }
    private string LocalTr(string key) => string.IsNullOrEmpty(key) ? key : Tr(key);
}
