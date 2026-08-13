using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using EraDream.Core;

public partial class StoryPlayerEngine
{
    private const float GlobalAutoPlayDelaySeconds = 3f;
    // 所有剧情演出都使用固定逻辑画布，窗口仅负责等比显示。
    private static readonly Vector2 DesignSize = new(1280, 720);
    private Control _designCanvas;
    private TextureRect _backgroundTransitionRect;
    private ColorRect _transitionOverlay;
    private Tween _backgroundTween;
    private Tween _transitionTween;
    private Tween _autoAdvanceTween;
    private readonly List<AudioStreamPlayer> _sfxPlayers = new();
    private readonly Dictionary<string, List<AudioStreamPlayer>> _nodeAudioPlayers = new();
    private readonly Dictionary<AudioStreamPlayer, Tween> _audioFadeTweens = new();
    private readonly HashSet<string> _startedVoiceNodeIds = new(StringComparer.Ordinal);
    private object _activeBackgroundData;
    private float _dialogueBaseTop;
    private float _dialogueBaseBottom;
    private Vector2 _autoPlayBasePosition;

    private void InitializePresentationSurface()
    {
        _autoPlayBasePosition = _autoPlayButton?.Position ?? Vector2.Zero;
        _designCanvas = new Control { Name = "DesignCanvas", MouseFilter = MouseFilterEnum.Ignore, Size = DesignSize };
        AddChild(_designCanvas);
        MoveChild(_designCanvas, 0);

        // 将视觉层移入统一画布；安全区 UI 仍留在根节点，由 Godot 在移动端避让系统区域。
        if (_backgroundRect != null) ReparentToCanvas(_backgroundRect, 0);
        _backgroundTransitionRect = CreateBackgroundLayer("BackgroundTransition");
        _designCanvas.AddChild(_backgroundTransitionRect);
        _designCanvas.MoveChild(_backgroundTransitionRect, 1);
        if (_overlay != null) ReparentToCanvas(_overlay, 2);
        _designCanvas.AddChild(_characterContainer);
        _designCanvas.MoveChild(_characterContainer, 3);
        ReparentStoryUi();

        _transitionOverlay = new ColorRect { Name = "TransitionOverlay", Color = new Color(0, 0, 0, 0), MouseFilter = MouseFilterEnum.Ignore };
        _transitionOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
        _designCanvas.AddChild(_transitionOverlay);
        _designCanvas.MoveChild(_transitionOverlay, 4);
        Resized += RefreshCanvasTransform;
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSafeAreaPaddingChanged += OnPresentationSafeAreaChanged;
        RefreshCanvasTransform();
    }

    private void ReparentStoryUi()
    {
        if (_dialogueBox != null)
        {
            _dialogueBaseTop = _dialogueBox.OffsetTop;
            _dialogueBaseBottom = _dialogueBox.OffsetBottom;
            _dialogueBox.Reparent(_designCanvas, false);
            _designCanvas.MoveChild(_dialogueBox, 5);
        }
        if (_choiceContainer != null)
        {
            _choiceContainer.Reparent(_designCanvas, false);
            _designCanvas.MoveChild(_choiceContainer, 6);
        }
    }

    private void ReparentToCanvas(Control node, int index)
    {
        node.Reparent(_designCanvas, false);
        node.SetAnchorsPreset(LayoutPreset.FullRect);
        node.Position = Vector2.Zero;
        node.Size = DesignSize;
        _designCanvas.MoveChild(node, index);
    }

    private TextureRect CreateBackgroundLayer(string name)
    {
        var layer = new TextureRect { Name = name, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = MouseFilterEnum.Ignore };
        layer.SetAnchorsPreset(LayoutPreset.FullRect);
        layer.Size = DesignSize;
        return layer;
    }

    private void RefreshCanvasTransform()
    {
        if (_designCanvas == null || Size.X <= 0 || Size.Y <= 0) return;
        float scale = Mathf.Min(Size.X / DesignSize.X, Size.Y / DesignSize.Y);
        _designCanvas.Scale = Vector2.One * scale;
        _designCanvas.Position = (Size - DesignSize * scale) / 2;
        ApplyPresentationSafeArea(scale);
    }

    private void OnPresentationSafeAreaChanged(float padding)
    {
        RefreshCanvasTransform();
    }

    private void ApplyPresentationSafeArea(float canvasScale)
    {
        float physicalPadding = SettingsManager.Instance?.SafeAreaPadding ?? 0f;
        physicalPadding = float.IsFinite(physicalPadding) ? Mathf.Clamp(physicalPadding, 0f, 100f) : 0f;
        float logicalPadding = physicalPadding / Mathf.Max(canvasScale, 0.001f);

        // 对话框在设计画布内使用逻辑像素，顶部工具按钮使用根视口物理像素。
        if (_dialogueBox != null)
        {
            _dialogueBox.OffsetTop = _dialogueBaseTop - logicalPadding;
            _dialogueBox.OffsetBottom = _dialogueBaseBottom - logicalPadding;
        }
        if (_autoPlayButton != null)
        {
            // 自动播放按钮位于根视口，只按左上安全区移动，避免每次刷新继续累加偏移。
            _autoPlayButton.Position = _autoPlayBasePosition + new Vector2(physicalPadding, physicalPadding);
        }
    }

    private void UnsubscribePresentationSettings()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSafeAreaPaddingChanged -= OnPresentationSafeAreaChanged;
    }

    private Vector2 ToDesignPosition(Vector2 rootPosition)
    {
        float scale = Mathf.Max(_designCanvas?.Scale.X ?? 1f, 0.001f);
        return (rootPosition - (_designCanvas?.Position ?? Vector2.Zero)) / scale;
    }

    private void InitializePlaybackTools()
    {
        _bgmPlayer = new AudioStreamPlayer { Name = "BgmPlayer" };
        AddChild(_bgmPlayer);
    }

    private void UpdateBackground(string file, string transition, object backgroundData)
    {
        if (string.IsNullOrEmpty(file)) return;
        Texture2D texture = ResourceProxy.LoadBackgroundTexture(file);
        if (texture == null) { GD.PushWarning($"[StoryPlayerEngine] 背景无法加载: {file}"); return; }
        _activeBackgroundData = backgroundData;
        ApplyBackgroundTexture(texture, transition, backgroundData);
    }

    private void ApplyBackgroundTexture(Texture2D texture, string transition, object backgroundData)
    {
        _backgroundTween?.Kill();
        ApplyBackgroundTransform(_backgroundTransitionRect, backgroundData);
        float duration = Mathf.Max(GetFloatProperty(backgroundData, "TransitionDuration", .35f), 0f);
        string type = string.IsNullOrWhiteSpace(transition) ? "Fade" : transition;
        if (_backgroundRect.Texture == null || string.Equals(type, "Cut", StringComparison.OrdinalIgnoreCase))
        {
            _backgroundRect.Texture = texture;
            ApplyBackgroundTransform(_backgroundRect, backgroundData);
            _backgroundRect.Modulate = Colors.White;
            _backgroundTransitionRect.Texture = null;
            return;
        }

        _backgroundTransitionRect.Texture = texture;
        _backgroundTransitionRect.Modulate = Colors.White;
        ApplyBackgroundTransform(_backgroundTransitionRect, backgroundData);
        _backgroundTween = CreateTween().SetParallel();
        if (string.Equals(type, "Slide", StringComparison.OrdinalIgnoreCase))
        {
            _backgroundTransitionRect.Position = new Vector2(DesignSize.X, 0);
            _backgroundTween.TweenProperty(_backgroundTransitionRect, "position", Vector2.Zero, duration);
            _backgroundTween.TweenProperty(_backgroundRect, "position", new Vector2(-DesignSize.X * .15f, 0), duration);
        }
        else
        {
            _backgroundTransitionRect.Modulate = new Color(1, 1, 1, 0);
            _backgroundTween.TweenProperty(_backgroundTransitionRect, "modulate:a", 1f, duration);
        }
        _backgroundTween.Finished += () =>
        {
            _backgroundRect.Texture = texture;
            ApplyBackgroundTransform(_backgroundRect, backgroundData);
            _backgroundRect.Modulate = Colors.White;
            _backgroundTransitionRect.Texture = null;
            _backgroundTransitionRect.Position = Vector2.Zero;
        };
    }

    private void ApplyBackgroundTransform(TextureRect target, object data)
    {
        float offsetX = GetFloatProperty(data, "OffsetX", 0f);
        float offsetY = GetFloatProperty(data, "OffsetY", 0f);
        float scale = Mathf.Clamp(GetFloatProperty(data, "Scale", 1f), .1f, 5f);
        target.PivotOffset = DesignSize / 2;
        target.Position = new Vector2(offsetX, offsetY);
        target.Scale = Vector2.One * scale;
        target.Size = DesignSize;
    }

    private void ScheduleAutoAdvance(object nodeData)
    {
        if (_currentNode is not EraDream.StoryEditor.Nodes.DialogueNodeData
            && _currentNode is not EraDream.StoryEditor.Nodes.NarrativeNodeData) return;
        float delay;
        if (_isAutoPlayEnabled)
        {
            // 全局自动模式始终以固定节奏模拟一次左键推进。
            delay = GlobalAutoPlayDelaySeconds;
        }
        else
        {
            if (_autoPlayUserOverride || !ProjectManager.Metadata.AutoPlayEnabled) return;
            float configuredDelay = GetFloatProperty(nodeData, "AutoAdvanceDelay", 0f);
            delay = configuredDelay > 0f ? configuredDelay : GetFloatProperty(nodeData, "HoldDuration", ProjectManager.Metadata.DefaultAutoAdvanceDelay);
        }
        if (delay <= 0f || _currentNode == null) return;
        string nodeId = _currentNode.Id;
        _autoAdvanceTween = CreateTween();
        _autoAdvanceTween.TweenInterval(delay);
        _autoAdvanceTween.Finished += () =>
        {
            // 节点已被手动切换时，旧计时器不可推进新节点。
            if (_currentNode?.Id != nodeId || _isTextAnimating) return;
            if (_currentNode is EraDream.StoryEditor.Nodes.DialogueNodeData) GoToNextNode(_nextNonVisualNodeId);
            else if (_currentNode is EraDream.StoryEditor.Nodes.NarrativeNodeData) GoToNextNode(_currentNode.NextNodeId);
        };
    }

    private void CancelAutoAdvance()
    {
        _autoAdvanceTween?.Kill();
        _autoAdvanceTween = null;
    }

    private void ApplyNodeFont(object nodeData)
    {
        string path = GetStringProperty(nodeData, "FontFile", "");
        if (string.IsNullOrWhiteSpace(path)) path = ProjectManager.Metadata.DefaultFontFile;
        _nameLabel?.RemoveThemeFontOverride("font");
        _contentLabel?.RemoveThemeFontOverride("normal_font");
        if (string.IsNullOrWhiteSpace(path)) return;
        FontFile font = LoadProjectFont(path);
        if (font == null) { GD.PushWarning($"[StoryPlayerEngine] 字体无法加载: {path}"); return; }
        _nameLabel?.AddThemeFontOverride("font", font);
        _contentLabel?.AddThemeFontOverride("normal_font", font);
    }

    private void PlayVoice(string file, string nodeId)
    {
        if (string.IsNullOrWhiteSpace(file) || string.IsNullOrWhiteSpace(nodeId)) return;
        // 同一文本节点只启动一次语音，场景音效不参与语音去重。
        if (!_startedVoiceNodeIds.Add(nodeId)) return;
        var stream = ResourceProxy.LoadAudioFromProject(file);
        if (stream == null)
        {
            _startedVoiceNodeIds.Remove(nodeId);
            GD.PushWarning($"[StoryPlayerEngine] 语音无法加载: {file}");
            return;
        }
        var player = new AudioStreamPlayer { Stream = stream, VolumeDb = 0f };
        AddChild(player);
        _sfxPlayers.Add(player);
        player.Finished += () =>
        {
            UntrackNodeAudio(nodeId, player);
            ReleaseAudioPlayer(player, true);
        };
        player.Play();
        TrackNodeAudio(nodeId, player);
    }

    private void PlaySfx(string file, float volume, bool waitForCompletion, string nextNodeId, string nodeId = "")
    {
        var stream = ResourceProxy.LoadAudioFromProject(file);
        if (stream == null)
        {
            // 预执行阶段没有后续跳转责任，加载失败也不能中断当前文本。
            if (nextNodeId != null) GoToNextNode(nextNodeId);
            return;
        }
        var player = new AudioStreamPlayer { Stream = stream, VolumeDb = LinearToDb(Mathf.Clamp(volume, 0f, 1f)) };
        AddChild(player);
        _sfxPlayers.Add(player);
        player.Finished += () =>
        {
            UntrackNodeAudio(nodeId, player);
            ReleaseAudioPlayer(player, true);
            if (waitForCompletion) GoToNextNode(nextNodeId);
        };
        player.Play();
        TrackNodeAudio(nodeId, player);
        if (!waitForCompletion && nextNodeId != null) GoToNextNode(nextNodeId);
    }

    private void TrackNodeAudio(string nodeId, AudioStreamPlayer player)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || player == null) return;
        if (!_nodeAudioPlayers.TryGetValue(nodeId, out var players))
            _nodeAudioPlayers[nodeId] = players = new List<AudioStreamPlayer>();
        if (!players.Contains(player)) players.Add(player);
    }

    private void UntrackNodeAudio(string nodeId, AudioStreamPlayer player)
    {
        if (!_nodeAudioPlayers.TryGetValue(nodeId, out var players)) return;
        players.Remove(player);
        if (players.Count == 0) _nodeAudioPlayers.Remove(nodeId);
    }

    private void FinishNodeAudio(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || !_nodeAudioPlayers.TryGetValue(nodeId, out var players)) return;
        _nodeAudioPlayers.Remove(nodeId);
        foreach (var player in players.ToArray())
        {
            if (player == null || !GodotObject.IsInstanceValid(player)) continue;
            StartAudioFade(player);
        }
    }

    private void StartAudioFade(AudioStreamPlayer player)
    {
        if (player == null || !GodotObject.IsInstanceValid(player) || player.IsQueuedForDeletion()) return;
        if (_audioFadeTweens.Remove(player, out var oldTween)) oldTween.Kill();
        var fade = CreateTween();
        _audioFadeTweens[player] = fade;
        fade.TweenProperty(player, "volume_db", -80f, .25f);
        fade.Finished += () =>
        {
            // 完成回调不能再次 Kill 当前 Tween，否则会与 Godot 原生回收流程交叉。
            if (!_audioFadeTweens.Remove(player)) return;
            if (!GodotObject.IsInstanceValid(player) || player.IsQueuedForDeletion()) return;
            player.Stop();
            ReleaseAudioPlayer(player, false);
        };
    }

    private void ReleaseAudioPlayer(AudioStreamPlayer player, bool cancelFade)
    {
        if (player == null) return;
        if (cancelFade && _audioFadeTweens.Remove(player, out var fade)) fade.Kill();
        _sfxPlayers.Remove(player);
        if (GodotObject.IsInstanceValid(player) && !player.IsQueuedForDeletion()) player.QueueFree();
    }

    private bool TryHandleExtensionNode(object node, bool pauseForEdit)
    {
        string typeName = node.GetType().Name;
        if (typeName == "TransitionNodeData")
        {
            PlayScreenTransition(GetStringProperty(node, "TransitionType", "FadeBlack"), GetFloatProperty(node, "Duration", .35f), () => AdvanceOrPause(GetStringProperty(node, "NextNodeId", ""), pauseForEdit));
            return true;
        }
        if (typeName == "SoundEffectNodeData" || typeName == "SfxNodeData")
        {
            PlaySfx(GetStringProperty(node, "AudioFile", GetStringProperty(node, "File", "")), GetFloatProperty(node, "Volume", 1f), GetBoolProperty(node, "WaitForCompletion", false), GetStringProperty(node, "NextNodeId", ""), GetStringProperty(node, "Id", ""));
            return true;
        }
        return false;
    }

    private bool TryExecuteExtensionNode(object node, out string nextNodeId)
    {
        nextNodeId = GetStringProperty(node, "NextNodeId", "");
        string typeName = node.GetType().Name;
        if (typeName == "TransitionNodeData")
        {
            PlayScreenTransition(GetStringProperty(node, "TransitionType", "FadeBlack"), GetFloatProperty(node, "Duration", .35f), null);
            return true;
        }
        if (typeName == "SoundEffectNodeData" || typeName == "SfxNodeData")
        {
            // 预执行阶段只允许非阻塞音效与文本并行。
            if (GetBoolProperty(node, "WaitForCompletion", false)) return false;
            PlaySfx(GetStringProperty(node, "AudioFile", GetStringProperty(node, "File", "")), GetFloatProperty(node, "Volume", 1f), false, null, GetStringProperty(node, "Id", ""));
            return true;
        }
        return false;
    }

    private void PlayScreenTransition(string type, float duration, Action after)
    {
        _transitionTween?.Kill();
        duration = Mathf.Max(duration, .01f);
        bool white = type.Contains("White", StringComparison.OrdinalIgnoreCase) || type.Contains("Flash", StringComparison.OrdinalIgnoreCase);
        _transitionOverlay.Color = new Color(white ? 1f : 0f, white ? 1f : 0f, white ? 1f : 0f, 0f);
        _transitionTween = CreateTween();
        if (type.Contains("SlideLeft", StringComparison.OrdinalIgnoreCase) || type.Contains("SlideRight", StringComparison.OrdinalIgnoreCase))
        {
            float startX = type.Contains("Left", StringComparison.OrdinalIgnoreCase) ? -DesignSize.X : DesignSize.X;
            _transitionOverlay.Position = new Vector2(startX, 0);
            _transitionOverlay.Color = new Color(white ? 1f : 0f, white ? 1f : 0f, white ? 1f : 0f, 1f);
            _transitionTween.TweenProperty(_transitionOverlay, "position", Vector2.Zero, duration * .7f);
            _transitionTween.TweenProperty(_transitionOverlay, "color:a", 0f, duration * .3f);
        }
        else
        {
            _transitionTween.TweenProperty(_transitionOverlay, "color:a", 1f, duration * .5f);
            _transitionTween.TweenProperty(_transitionOverlay, "color:a", 0f, duration * .5f);
        }
        _transitionTween.Finished += () => { _transitionOverlay.Position = Vector2.Zero; after?.Invoke(); };
    }

    private void StopPresentationTweens()
    {
        _backgroundTween?.Kill();
        _transitionTween?.Kill();
        foreach (var fade in _audioFadeTweens.Values) fade?.Kill();
        _audioFadeTweens.Clear();
        foreach (var player in _sfxPlayers.ToArray()) ReleaseAudioPlayer(player, false);
        _nodeAudioPlayers.Clear();
        _startedVoiceNodeIds.Clear();
    }

    private static float LinearToDb(float linear) => linear <= 0.001f ? -80f : Mathf.LinearToDb(linear);
    private static FontFile LoadProjectFont(string file)
    {
        // 字体统一保存为项目相对路径；Godot 已导入的资源优先直接加载。
        if (ResourceLoader.Exists(file)) return ResourceLoader.Load<FontFile>(file);
        string path = ProjectManager.IsProjectOpened ? $"{ProjectManager.FontDir}/{file}" : $"res://fonts/{file}";
        if (ResourceLoader.Exists(path)) return ResourceLoader.Load<FontFile>(path);
        // Fonts must be imported by Godot before runtime loading is available.
        return null;
    }
    private static object GetProperty(object source, string name) => source?.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(source);
    private static float GetFloatProperty(object source, string name, float fallback) => GetProperty(source, name) is object value && float.TryParse(value.ToString(), out float result) ? result : fallback;
    private static bool GetBoolProperty(object source, string name, bool fallback) => GetProperty(source, name) is object value && bool.TryParse(value.ToString(), out bool result) ? result : fallback;
    private static string GetStringProperty(object source, string name, string fallback) => GetProperty(source, name)?.ToString() ?? fallback;
}
