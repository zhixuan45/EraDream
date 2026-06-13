using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class StorySelectorScreen : Control
{
    public static bool IsForSimulation = false;
    
    private string _storiesDir = "user://stories/";
    private VBoxContainer _storyList;
    private LineEdit _searchEdit;
    private Button _btnPlay;
    
    // 经典单选模式
    private string _selectedStoryPath = "";
    // 养成模式复选
    private List<string> _selectedScenarios = new List<string>();
    private List<string> _selectedCharacters = new List<string>();
    private List<string> _selectedMods = new List<string>();
    
    // 分类展示容器
    private TabContainer _tabContainer;
    private VBoxContainer _scenarioList;
    private VBoxContainer _characterList;
    private VBoxContainer _modList;

    public override void _Ready()
    {
        _searchEdit = GetNode<LineEdit>("SafeAreaAdapter/VBoxContainer/Header/SearchEdit");
        _btnPlay = GetNode<Button>("SafeAreaAdapter/VBoxContainer/Footer/BtnPlay");
        
        // 动态创建 TabContainer 替换原有的 ScrollContainer
        var scroll = GetNode<ScrollContainer>("SafeAreaAdapter/VBoxContainer/ScrollContainer");
        var parent = scroll.GetParent();
        int idx = scroll.GetIndex();
        parent.RemoveChild(scroll);
        
        _tabContainer = new TabContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        parent.AddChild(_tabContainer);
        parent.MoveChild(_tabContainer, idx);

        _scenarioList = CreateTab("剧情包 (Stories)");
        _characterList = CreateTab("马娘包 (Characters)");
        _modList = CreateTab("MODS");

        _searchEdit.TextChanged += OnSearchTextChanged;
        GetNode<Button>("SafeAreaAdapter/VBoxContainer/Footer/BtnBack").Pressed += () => {
            IsForSimulation = false; 
            LoadingScreen.TargetScene = "res://scenes/MainMenuScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
        };
        _btnPlay.Pressed += LaunchStory;

        EnsureDirs();
        RefreshAllLists();

        // 养成模式下，允许免除选剧本限制，直接开始游戏进行签约
        if (IsForSimulation)
        {
            _btnPlay.Text = "开始养成 (Start)";
            _btnPlay.Disabled = false;
        }
        else
        {
            _btnPlay.Disabled = true;
        }
    }

    private void EnsureDirs()
    {
        string[] dirs = { "user://stories/", "user://characters/", "user://mods/" };
        foreach (var d in dirs)
        {
            if (!DirAccess.DirExistsAbsolute(d)) DirAccess.MakeDirAbsolute(d);
        }
    }

    private VBoxContainer CreateTab(string title)
    {
        var sc = new ScrollContainer { Name = title, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var vb = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        sc.AddChild(vb);
        _tabContainer.AddChild(sc);
        return vb;
    }

    private void RefreshAllLists()
    {
        RefreshList("user://stories/", _scenarioList, _selectedScenarios);
        
        // 动态加载来自已激活扩展的剧本
        if (EraDream.Core.Extensions.ExtensionManager.Instance != null)
        {
            var extStories = EraDream.Core.Extensions.ExtensionManager.Instance.GetActiveStoryPaths();
            foreach (var storyPath in extStories)
            {
                AddStoryItem(storyPath, _scenarioList, _selectedScenarios);
            }
        }

        if (IsForSimulation)
        {
            // 智能自动加载并注册当前激活的缓存马娘与本地开发马娘配置
            CharacterManager.LoadRegisteredActors(ProjectSettings.GlobalizePath("user://cache/ext/"), true);
            CharacterManager.LoadRegisteredActors(ProjectSettings.GlobalizePath("user://extensions/"), false);

            // 养成模式下，智能直接载入当前游戏已注册激活的所有马娘，脱离物理 user:// 限制
            foreach (Node child in _characterList.GetChildren()) child.QueueFree();
            foreach (var character in CharacterManager.Characters)
            {
                if (!string.IsNullOrEmpty(character.ActorId))
                {
                    AddActiveUmaItem(character.ActorId, character.DisplayName, _characterList, _selectedCharacters);
                }
            }
        }
        else
        {
            RefreshList("user://characters/", _characterList, _selectedCharacters);
        }

        RefreshList("user://mods/", _modList, _selectedMods);
    }

    private void RefreshList(string path, VBoxContainer container, List<string> selection)
    {
        foreach (Node child in container.GetChildren()) child.QueueFree();

        using var dir = DirAccess.Open(path);
        if (dir == null) return;

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (!dir.CurrentIsDir() && (fileName.EndsWith(".json") || fileName.EndsWith(".era") || fileName.EndsWith(".zip")))
            {
                string fullPath = path + fileName;
                AddStoryItem(fullPath, container, selection);
            }
            fileName = dir.GetNext();
        }
    }

    private void AddStoryItem(string fullPath, VBoxContainer container, List<string> selection)
    {
        string fileName = System.IO.Path.GetFileName(fullPath);
        CheckBox cb = new CheckBox {
            Text = fileName,
            TooltipText = fullPath,
            CustomMinimumSize = new Vector2(0, 40),
            ButtonPressed = selection.Contains(fullPath)
        };
        cb.Toggled += (bool pressed) => {
            if (pressed)
            {
                if (!IsForSimulation)
                {
                    // 非养成模式下强制排他单选
                    foreach (Node node in container.GetChildren())
                    {
                        if (node is CheckBox otherCb && otherCb != cb)
                        {
                            otherCb.SetPressedNoSignal(false);
                        }
                    }
                    selection.Clear();
                    _selectedStoryPath = fullPath;
                }
                if (!selection.Contains(fullPath)) selection.Add(fullPath);
            }
            else
            {
                selection.Remove(fullPath);
                if (!IsForSimulation && _selectedStoryPath == fullPath)
                {
                    _selectedStoryPath = "";
                }
            }
            // 仅在非养成（即普通剧本播放模式）下才强制置灰开始按钮
            _btnPlay.Disabled = !IsForSimulation && _selectedScenarios.Count == 0;
        };
        container.AddChild(cb);
    }

    private void OnSearchTextChanged(string newText)
    {
        // 简易搜索逻辑：根据关键字显示/隐藏各个列表中的 CheckBox
        SearchInList(_scenarioList, newText);
        SearchInList(_characterList, newText);
        SearchInList(_modList, newText);
    }

    private void SearchInList(VBoxContainer container, string filter)
    {
        foreach (Node child in container.GetChildren())
        {
            if (child is CheckBox cb)
            {
                cb.Visible = string.IsNullOrWhiteSpace(filter) || cb.Text.ToLower().Contains(filter.ToLower());
            }
        }
    }

    private void LaunchStory()
    {
        if (IsForSimulation)
        {
            // 允许在没有附加剧本文件时直接开始养成（走无马娘运动场签约前置流）
            if (EraDream.Game.GameManager.Instance != null)
            {
                EraDream.Game.GameManager.Instance.StartNewGame(_selectedScenarios, _selectedCharacters, _selectedMods);
                
                // 若勾选了多个马娘，填充至候选签约池，但不进行直接签约以防跳过招募流程
                if (_selectedCharacters.Count > 0)
                {
                    EraDream.Game.GameManager.Instance.CurrentState.CurrentScoutPool.Clear();
                    foreach (var charId in _selectedCharacters)
                    {
                        EraDream.Game.GameManager.Instance.CurrentState.CurrentScoutPool.Add(charId);
                    }
                }

                EraDream.Game.GameManager.Instance.AutoSave();
            }
            IsForSimulation = false;
            LoadingScreen.TargetScene = "res://scenes/SimulationMainScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
            return;
        }

        if (string.IsNullOrEmpty(_selectedStoryPath)) return;

        if (_selectedStoryPath.EndsWith(".era") || _selectedStoryPath.EndsWith(".zip"))
        {
            GD.Print($"[Selector] Loading Package: {_selectedStoryPath}");
            bool success = ProjectSettings.LoadResourcePack(_selectedStoryPath);
            if (success)
            {
                StoryPlayerEngine.CurrentStoryPath = "res://story.json";
                CharacterManager.LoadCharacters("res://characters.json");
            }
            else
            {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowErrorDialog("加载失败", "[Selector] Failed to load package!");
                return;
            }
        }
        else
        {
            StoryPlayerEngine.CurrentStoryPath = _selectedStoryPath;
            string baseDir = _selectedStoryPath.Substring(0, _selectedStoryPath.LastIndexOf('/'));
            string charFile = baseDir + "/characters.json";
            if (FileAccess.FileExists(charFile)) CharacterManager.LoadCharacters(charFile);
        }

        LoadingScreen.TargetScene = "res://scenes/StoryPlayerScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
    }

    /// <summary>
    /// 在已载入的马娘列表中添加复选框条目，支持自由多选
    /// </summary>
    private void AddActiveUmaItem(string actorId, string displayName, VBoxContainer container, List<string> selection)
    {
        CheckBox cb = new CheckBox {
            Text = $"{displayName} ({actorId})",
            TooltipText = $"马娘 ID: {actorId}",
            CustomMinimumSize = new Vector2(0, 40),
            ButtonPressed = selection.Contains(actorId)
        };

        cb.Toggled += (bool pressed) => {
            if (pressed)
            {
                if (!selection.Contains(actorId)) selection.Add(actorId);
            }
            else
            {
                selection.Remove(actorId);
            }
        };

        container.AddChild(cb);
    }
}

