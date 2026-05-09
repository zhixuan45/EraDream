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
            LoadingScreen.TargetScene = "res://scenes/UI/MainMenuScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/UI/LoadingScreen.tscn");
        };
        _btnPlay.Pressed += LaunchStory;

        EnsureDirs();
        RefreshAllLists();
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
        RefreshList("user://characters/", _characterList, _selectedCharacters);
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
                CheckBox cb = new CheckBox {
                    Text = fileName,
                    CustomMinimumSize = new Vector2(0, 40),
                    ButtonPressed = selection.Contains(fullPath)
                };
                cb.Toggled += (bool pressed) => {
                    if (pressed) { if (!selection.Contains(fullPath)) selection.Add(fullPath); }
                    else selection.Remove(fullPath);
                    _btnPlay.Disabled = _selectedScenarios.Count == 0;
                };
                container.AddChild(cb);
            }
            fileName = dir.GetNext();
        }
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
            if (_selectedScenarios.Count == 0) return;
            
            if (umaEraArchive.Game.GameManager.Instance != null)
            {
                umaEraArchive.Game.GameManager.Instance.StartNewGame(_selectedScenarios, _selectedCharacters, _selectedMods);
                
                // 自动将第一个马娘包的 ID 设为当前活跃马娘 (需从包名或 manifest 解析，暂时简化为路径 ID)
                if (_selectedCharacters.Count > 0)
                {
                    string charId = System.IO.Path.GetFileNameWithoutExtension(_selectedCharacters[0]);
                    umaEraArchive.Game.GameManager.Instance.CurrentState.ActiveUmaId = charId;
                }

                umaEraArchive.Game.GameManager.Instance.AutoSave();
            }
            IsForSimulation = false;
            LoadingScreen.TargetScene = "res://scenes/Game/SimulationMainScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/UI/LoadingScreen.tscn");
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
                GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowErrorDialog("加载失败", "[Selector] Failed to load package!");
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

        LoadingScreen.TargetScene = "res://scenes/Game/StoryPlayerScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/UI/LoadingScreen.tscn");
    }
}

