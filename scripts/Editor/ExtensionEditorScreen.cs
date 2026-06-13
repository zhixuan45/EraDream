using Godot;
using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using UmaEraArchive.Editor.Models;
using UmaEraArchive.Core;
using UmaEraArchive.Core.Models;
using UmaEraArchive.Core.Extensions;
using ExtensionManifest = UmaEraArchive.Editor.Models.ExtensionManifest;

public partial class ExtensionEditorScreen : Control
{
    private string _projectPath = "";
    private string _cachedNormalizedProjectBase = "";
    private string _cachedNormalizedProjectWithSlash = "";

    private void SetProjectPath(string path)
    {
        _projectPath = path;
        if (!string.IsNullOrEmpty(_projectPath)) {
            string absoluteProjectPath = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath(_projectPath));
            _cachedNormalizedProjectBase = absoluteProjectPath.Replace('\\', '/').TrimEnd('/');
            _cachedNormalizedProjectWithSlash = _cachedNormalizedProjectBase + "/";
        } else {
            _cachedNormalizedProjectBase = "";
            _cachedNormalizedProjectWithSlash = "";
        }
    }

    private UmaEraArchive.Editor.Models.ExtensionManifest _manifest = new();
    private ActorConfigData _currentActorConfig = null;
    private SimulationData _currentSimData = null;
    private BehaviorPack _currentBehaviorPack = null;

    // UI Nodes - SidePanel
    private Button _btnNew, _btnOpen, _btnExport, _btnBack;
    private MenuButton _btnCreateJSON;
    private Tree _fileTree;

    // UI Nodes - MainArea Containers
    private Control _manifestVBox, _fileEditorVBox, _imagePreviewVBox, _charEditorRoot, _behaviorEditorRoot;
    private TabContainer _charTabs;
    
    // Manifest Fields
    private LineEdit _idEdit, _nameEdit, _authorEdit, _versionEdit, _minVerEdit;
    private TextEdit _descEdit;
    private OptionButton _typeOption;

    // Behavior Editor Fields
    private VBoxContainer _behaviorRulesContainer;
    private Button _btnSaveBehavior;
    private Button _btnAddBehaviorRule;

    // Code Fields
    private Label _fileNameLabel;
    private TextEdit _fileContentEdit;
    private Button _btnSaveFile;

    // Image Fields
    private TextureRect _previewTexture;
    private Label _fileInfoLabel;

    // Character Editor Fields (Visuals)
    private LineEdit _charIdEdit, _charNameEdit, _charSpriteEdit;
    // Character Editor Fields (Simulation)
    private LineEdit _simInternalIdEdit, _simFullNameEdit, _simShortNameEdit, _simPersonalityEdit;
    private Button _btnSaveCombined;

    // Dialogs
    private PopupMenu _fileContextMenu;
    private ConfirmationDialog _deleteDialog;
    private AcceptDialog _renameDialog;
    private LineEdit _renameEdit;

    private string _currentEditingFilePath = "";
    private TreeItem _contextTargetItem = null;

    public override void _Ready()
    {
        var rootPath = "SafeArea/HSplit/MainPanel/MainArea/Margin/ContentRoot/";

        // SidePanel
        _btnNew = GetNode<Button>("SafeArea/HSplit/SidePanel/VBox/ProjBtns/BtnNewProject");
        _btnOpen = GetNode<Button>("SafeArea/HSplit/SidePanel/VBox/ProjBtns/BtnOpenProject");
        _btnCreateJSON = GetNode<MenuButton>("SafeArea/HSplit/SidePanel/VBox/ProjBtns/BtnCreateJSON");
        _btnExport = GetNode<Button>("SafeArea/HSplit/SidePanel/VBox/FooterBtns/BtnExport");
        _btnBack = GetNode<Button>("SafeArea/HSplit/SidePanel/VBox/FooterBtns/BtnBack");
        _fileTree = GetNode<Tree>("SafeArea/HSplit/SidePanel/VBox/FileTree");
        _fileTree.AllowRmbSelect = true;

        // MainArea Views
        _manifestVBox = GetNode<Control>(rootPath + "ManifestVBox");
        _fileEditorVBox = GetNode<Control>(rootPath + "FileEditorVBox");
        _imagePreviewVBox = GetNode<Control>(rootPath + "ImagePreviewVBox");
        _charEditorRoot = GetNode<Control>(rootPath + "CharacterEditorRoot");
        _charTabs = GetNode<TabContainer>(rootPath + "CharacterEditorRoot/TabContainer");
        _behaviorEditorRoot = GetNode<Control>(rootPath + "BehaviorEditorRoot");

        // Manifest
        _idEdit = GetNode<LineEdit>(rootPath + "ManifestVBox/Grid/IdEdit");
        _nameEdit = GetNode<LineEdit>(rootPath + "ManifestVBox/Grid/NameEdit");
        _authorEdit = GetNode<LineEdit>(rootPath + "ManifestVBox/Grid/AuthorEdit");
        _versionEdit = GetNode<LineEdit>(rootPath + "ManifestVBox/Grid/VersionEdit");
        _minVerEdit = GetNode<LineEdit>(rootPath + "ManifestVBox/Grid/MinVerEdit");
        _descEdit = GetNode<TextEdit>(rootPath + "ManifestVBox/Grid/DescEdit");
        _typeOption = GetNode<OptionButton>(rootPath + "ManifestVBox/Grid/TypeOption");

        // Code
        _fileNameLabel = GetNode<Label>(rootPath + "FileEditorVBox/FileHeader/FileNameLabel");
        _fileContentEdit = GetNode<TextEdit>(rootPath + "FileEditorVBox/FileContentEdit");
        _btnSaveFile = GetNode<Button>(rootPath + "FileEditorVBox/FileHeader/BtnSaveFile");

        // Image
        _previewTexture = GetNode<TextureRect>(rootPath + "ImagePreviewVBox/TextureRect");
        _fileInfoLabel = GetNode<Label>(rootPath + "ImagePreviewVBox/FileInfoLabel");

        // Character Visuals (Match with .tscn node names!)
        _charIdEdit = GetNode<LineEdit>(rootPath + "CharacterEditorRoot/TabContainer/Visuals/Grid/CharIdEdit");
        _charNameEdit = GetNode<LineEdit>(rootPath + "CharacterEditorRoot/TabContainer/Visuals/Grid/CharNameEdit");
        _charSpriteEdit = GetNode<LineEdit>(rootPath + "CharacterEditorRoot/TabContainer/Visuals/Grid/CharSpriteEdit");
        
        // Character Simulation (Match with .tscn node names!)
        _simInternalIdEdit = GetNode<LineEdit>(rootPath + "CharacterEditorRoot/TabContainer/Simulation/Grid/SimInternalIdEdit");
        _simFullNameEdit = GetNode<LineEdit>(rootPath + "CharacterEditorRoot/TabContainer/Simulation/Grid/SimFullNameEdit");
        _simShortNameEdit = GetNode<LineEdit>(rootPath + "CharacterEditorRoot/TabContainer/Simulation/Grid/SimShortNameEdit");
        _simPersonalityEdit = GetNode<LineEdit>(rootPath + "CharacterEditorRoot/TabContainer/Simulation/Grid/SimPersonalityEdit");
        
        _btnSaveCombined = GetNode<Button>(rootPath + "CharacterEditorRoot/Header/BtnSaveCombined");

        // Behavior Editor
        _behaviorEditorRoot = GetNode<Control>(rootPath + "BehaviorEditorRoot");
        _behaviorRulesContainer = GetNode<VBoxContainer>(rootPath + "BehaviorEditorRoot/ScrollContainer/RulesVBox");
        _btnSaveBehavior = GetNode<Button>(rootPath + "BehaviorEditorRoot/Header/BtnSaveBehavior");
        _btnAddBehaviorRule = GetNode<Button>(rootPath + "BehaviorEditorRoot/Header/BtnAddRule");

        // Common UI
        _fileContextMenu = GetNode<PopupMenu>("FileContextMenu");
        _deleteDialog = GetNode<ConfirmationDialog>("DeleteConfirmDialog");
        _renameDialog = GetNode<AcceptDialog>("RenameDialog");
        _renameEdit = GetNode<LineEdit>("RenameDialog/VBox/RenameEdit");

        // Events
        _btnNew.Pressed += OnNewPressed;
        _btnOpen.Pressed += OnOpenPressed;
        _btnCreateJSON.GetPopup().IdPressed += OnCreateJsonIdPressed;
        _btnExport.Pressed += OnExportPressed;
        _btnBack.Pressed += () => {
            LoadingScreen.TargetScene = "res://scenes/MainMenuScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
        };

        _fileTree.ItemSelected += OnFileItemSelected;
        _fileTree.ItemActivated += OnFileItemSelected;
        _fileTree.GuiInput += OnFileTreeGuiInput;
        
        _fileContextMenu.IdPressed += OnContextMenuIdPressed;
        _deleteDialog.Confirmed += OnDeleteConfirmed;
        _renameDialog.Confirmed += OnRenameConfirmed;
        
        _btnSaveFile.Pressed += OnSaveFilePressed;
        _btnSaveCombined.Pressed += OnSaveCombinedPressed;
        _btnSaveBehavior.Pressed += OnSaveBehaviorPressed;
        _btnAddBehaviorRule.Pressed += OnAddBehaviorRulePressed;

        GetNode<Button>(rootPath + "ManifestVBox/ResHBox/BtnImportSprite").Pressed += () => ImportAsset("Assets/Sprites");
        GetNode<Button>(rootPath + "ManifestVBox/ResHBox/BtnImportBG").Pressed += () => ImportAsset("Assets/Backgrounds");
        GetNode<Button>(rootPath + "ManifestVBox/ResHBox/BtnImportAudio").Pressed += () => ImportAsset("Assets/Audio");
        GetNode<Button>(rootPath + "ManifestVBox/ResHBox/BtnImportDLL").Pressed += () => ImportAsset("Logic");

        UpdateUIFromManifest();
        RefreshFileTree();
    }

    private void HideAllViews()
    {
        _manifestVBox.Hide();
        _fileEditorVBox.Hide();
        _imagePreviewVBox.Hide();
        _charEditorRoot.Hide();
        _behaviorEditorRoot.Hide();
    }

    private void OnFileItemSelected()
    {
        var item = _fileTree.GetSelected();
        if (item == null) return;
        string path = item.GetMetadata(0).AsString();
        if (string.IsNullOrEmpty(path)) { ShowManifestView(); return; }
        if (System.IO.Directory.Exists(path)) return;
        DispatchFileView(path);
    }

    private void DispatchFileView(string path)
    {
        _currentEditingFilePath = path;
        string fileName = path.GetFile().ToLower();
        string ext = path.GetExtension().ToLower();
        HideAllViews();

        if (fileName == "manifest.json") ShowManifestView();
        else if (fileName == "characters.json" || fileName == "actor_config.json" || fileName == "simulation.json") ShowCharacterEditor(path);
        else if (fileName.EndsWith(".behavior.json") || fileName == "behavior.json") ShowBehaviorEditor(path);
        else if (ext == "png" || ext == "jpg" || ext == "jpeg" || ext == "webp") ShowImageView(path);
        else ShowCodeView(path);
    }

    private void ShowManifestView()
    {
        HideAllViews();
        LoadManifest();
        UpdateUIFromManifest();
        _manifestVBox.Show();
    }

    private void ShowCodeView(string path)
    {
        _fileNameLabel.Text = path.GetFile();
        try {
            if (!Godot.FileAccess.FileExists(path)) return;
            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            if (file != null) {
                _fileContentEdit.Text = file.GetAsText();
                _fileEditorVBox.Show();
            }
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"无法读取文件: {ex.Message}");
        }
    }

    private void ShowImageView(string path)
    {
        try {
            var image = Image.LoadFromFile(path);
            if (image != null) {
                var tex = ImageTexture.CreateFromImage(image);
                _previewTexture.Texture = tex;
                _fileInfoLabel.Text = $"{path.GetFile()} ({image.GetWidth()}x{image.GetHeight()})";
                _imagePreviewVBox.Show();
            }
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"图片加载失败: {ex.Message}");
        }
    }

    private void ShowCharacterEditor(string path)
    {
        string fileName = path.GetFile().ToLower();
        string baseDir = path.GetBaseDir();
        _charEditorRoot.Show();
        _currentActorConfig = null; _currentSimData = null;

        try {
            // 加载当前选中的文件
            if (Godot.FileAccess.FileExists(path)) {
                using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                
                if (fileName == "simulation.json") {
                    _charTabs.CurrentTab = 1;
                    _currentSimData = string.IsNullOrWhiteSpace(json) ? new() : JsonSerializer.Deserialize<SimulationData>(json) ?? new();
                } else {
                    _charTabs.CurrentTab = 0;
                    if (!string.IsNullOrWhiteSpace(json) && json.TrimStart().StartsWith("[")) {
                        var list = JsonSerializer.Deserialize<List<ActorConfigData>>(json);
                        _currentActorConfig = list?.FirstOrDefault() ?? new ActorConfigData();
                    } else {
                        _currentActorConfig = string.IsNullOrWhiteSpace(json) ? new() : JsonSerializer.Deserialize<ActorConfigData>(json) ?? new();
                    }
                }
            }

            // 尝试加载配对文件 (如果是 Pack 模式)
            if (fileName == "simulation.json") {
                string actorPath = baseDir.PathJoin("actor_config.json");
                if (Godot.FileAccess.FileExists(actorPath)) {
                    using var f = Godot.FileAccess.Open(actorPath, Godot.FileAccess.ModeFlags.Read);
                    _currentActorConfig = JsonSerializer.Deserialize<ActorConfigData>(f.GetAsText());
                }
            } else if (fileName == "actor_config.json") {
                string simPath = baseDir.PathJoin("simulation.json");
                if (Godot.FileAccess.FileExists(simPath)) {
                    using var f = Godot.FileAccess.Open(simPath, Godot.FileAccess.ModeFlags.Read);
                    _currentSimData = JsonSerializer.Deserialize<SimulationData>(f.GetAsText());
                }
            }

            // 更新 UI 字段
            if (_currentSimData != null) {
                _simInternalIdEdit.Text = _currentSimData.Identity?.InternalId ?? "";
                _simFullNameEdit.Text = _currentSimData.Identity?.FullName ?? "";
                _simShortNameEdit.Text = _currentSimData.Identity?.ShortName ?? "";
                _simPersonalityEdit.Text = _currentSimData.Identity?.PersonalityId ?? "";
            }
            if (_currentActorConfig != null) {
                _charIdEdit.Text = _currentActorConfig.ActorId ?? "";
                _charNameEdit.Text = _currentActorConfig.DisplayName ?? "";
                _charSpriteEdit.Text = _currentActorConfig.Visuals?.DefaultSprite ?? "";
            }
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"加载失败: {ex.Message}");
            ShowCodeView(path);
        }
    }

    private void OnSaveCombinedPressed()
    {
        if (string.IsNullOrEmpty(_currentEditingFilePath)) return;

        string absolutePath = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath(_currentEditingFilePath));
        if (!IsPathWithinProject(absolutePath)) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("保存失败: 文件不在项目内！");
            return;
        }

        try {
            var options = new JsonSerializerOptions { WriteIndented = true };
            
            // 同步 UI 到数据对象
            if (_currentSimData != null) {
                if (_currentSimData.Identity == null) _currentSimData.Identity = new CharacterIdentity();
                _currentSimData.Identity.InternalId = _simInternalIdEdit.Text;
                _currentSimData.Identity.FullName = _simFullNameEdit.Text;
                _currentSimData.Identity.ShortName = _simShortNameEdit.Text;
                _currentSimData.Identity.PersonalityId = _simPersonalityEdit.Text;
            }
            if (_currentActorConfig != null) {
                _currentActorConfig.ActorId = _charIdEdit.Text;
                _currentActorConfig.DisplayName = _charNameEdit.Text;
                if (_currentActorConfig.Visuals == null) _currentActorConfig.Visuals = new ActorVisuals();
                _currentActorConfig.Visuals.DefaultSprite = _charSpriteEdit.Text;
            }

            // 保存当前编辑的文件
            string fileName = _currentEditingFilePath.GetFile().ToLower();
            string json = "";
            if (fileName == "simulation.json" && _currentSimData != null) {
                json = JsonSerializer.Serialize(_currentSimData, options);
            } else if (fileName == "actor_config.json" && _currentActorConfig != null) {
                json = JsonSerializer.Serialize(_currentActorConfig, options);
            } else if (fileName == "characters.json" && _currentActorConfig != null) {
                json = JsonSerializer.Serialize(new List<ActorConfigData> { _currentActorConfig }, options);
            }

            if (!string.IsNullOrEmpty(json)) {
                using var file = Godot.FileAccess.Open(_currentEditingFilePath, Godot.FileAccess.ModeFlags.Write);
                file.StoreString(json);
            }

            // 如果是配对文件，且 ID 一致，则提示是否同步保存另一半 (可选，此处直接静默保存以符合"模板化修改"预期)
            if (fileName == "simulation.json" && _currentActorConfig != null) {
                _currentActorConfig.ActorId = _currentSimData.Identity.InternalId; // 保持 ID 一致
                string actorPath = _currentEditingFilePath.GetBaseDir().PathJoin("actor_config.json");
                if (Godot.FileAccess.FileExists(actorPath)) {
                    using var f = Godot.FileAccess.Open(actorPath, Godot.FileAccess.ModeFlags.Write);
                    f.StoreString(JsonSerializer.Serialize(_currentActorConfig, options));
                }
            } else if (fileName == "actor_config.json" && _currentSimData != null) {
                if (_currentSimData.Identity == null) _currentSimData.Identity = new CharacterIdentity();
                _currentSimData.Identity.InternalId = _currentActorConfig.ActorId; // 保持 ID 一致
                string simPath = _currentEditingFilePath.GetBaseDir().PathJoin("simulation.json");
                if (Godot.FileAccess.FileExists(simPath)) {
                    using var f = Godot.FileAccess.Open(simPath, Godot.FileAccess.ModeFlags.Write);
                    f.StoreString(JsonSerializer.Serialize(_currentSimData, options));
                }
            }

            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("配置已同步保存！");
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"保存失败: {ex.Message}");
        }
    }

    private void OnSaveFilePressed()
    {
        if (string.IsNullOrEmpty(_currentEditingFilePath)) return;

        string absolutePath = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath(_currentEditingFilePath));
        if (!IsPathWithinProject(absolutePath)) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("保存失败: 文件不在项目内！");
            return;
        }

        try {
            using var file = Godot.FileAccess.Open(_currentEditingFilePath, Godot.FileAccess.ModeFlags.Write);
            if (file != null) {
                file.StoreString(_fileContentEdit.Text);
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("文件已保存！");
            }
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"保存失败: {ex.Message}");
        }
    }

    private void OnFileTreeGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Right)
        {
            var item = _fileTree.GetItemAtPosition(mb.Position);
            if (item != null && item.GetParent() != null) 
            {
                _contextTargetItem = item;
                item.Select(0);
                UIUtils.ShowContextMenu(_fileContextMenu, this);
            }
        }
    }

    private void OnContextMenuIdPressed(long id)
    {
        if (_contextTargetItem == null) return;
        if (id == 0) {
            _renameEdit.Text = _contextTargetItem.GetText(0);
            _renameDialog.PopupCentered();
        } else if (id == 1) {
            _deleteDialog.PopupCentered();
        }
    }

    private bool IsPathWithinProject(string absolutePath)
    {
        if (string.IsNullOrEmpty(_cachedNormalizedProjectBase)) return false;

        var normalizedPath = absolutePath.Replace('\\', '/').TrimEnd('/');

        // Use OS-aware string comparison to prevent bypasses on case-insensitive systems (Windows)
        // while remaining secure on case-sensitive ones (Linux/macOS).
        var comparison = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return normalizedPath.Equals(_cachedNormalizedProjectBase, comparison) ||
               normalizedPath.StartsWith(_cachedNormalizedProjectWithSlash, comparison);
    }

    private void OnDeleteConfirmed()
    {
        string path = _contextTargetItem.GetMetadata(0).AsString();
        string globalPath = ProjectSettings.GlobalizePath(path);

        try {
            string absolutePath = System.IO.Path.GetFullPath(globalPath);

            if (!IsPathWithinProject(absolutePath)) {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("删除失败: 不能删除项目外的文件！");
                return;
            }

            if (System.IO.Directory.Exists(absolutePath)) System.IO.Directory.Delete(absolutePath, true);
            else if (System.IO.File.Exists(absolutePath)) System.IO.File.Delete(absolutePath);

            if (_currentEditingFilePath == path) {
                _currentEditingFilePath = "";
                ShowManifestView();
            }
            RefreshFileTree();
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("已删除！");
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"删除失败: {ex.Message}");
        }
    }

    private void OnRenameConfirmed()
    {
        string oldPath = _contextTargetItem.GetMetadata(0).AsString();
        string newName = _renameEdit.Text.Trim();
        if (string.IsNullOrEmpty(newName)) return;

        string newPath = oldPath.GetBaseDir().PathJoin(newName);
        
        string globalOldPath = ProjectSettings.GlobalizePath(oldPath);
        string globalNewPath = ProjectSettings.GlobalizePath(newPath);

        try {
            string absoluteOldPath = System.IO.Path.GetFullPath(globalOldPath);
            string absoluteNewPath = System.IO.Path.GetFullPath(globalNewPath);

            if (!IsPathWithinProject(absoluteOldPath) || !IsPathWithinProject(absoluteNewPath)) {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("重命名失败: 目标文件必须在项目内！");
                return;
            }

            if (System.IO.Directory.Exists(absoluteOldPath)) System.IO.Directory.Move(absoluteOldPath, absoluteNewPath);
            else if (System.IO.File.Exists(absoluteOldPath)) System.IO.File.Move(absoluteOldPath, absoluteNewPath);
            if (_currentEditingFilePath == oldPath) _currentEditingFilePath = newPath;
            RefreshFileTree();
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("重命名成功！");
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"重命名失败: {ex.Message}");
        }
    }

    private void OnNewPressed()
    {
        FileIOManager.OpenFolderDialog("选择新扩展包保存文件夹", (path) => {
            SetProjectPath(path);
            InitializeFolderStructure();
            SaveManifest();
            RefreshFileTree();
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("新扩展包项目已初始化！");
        });
    }

    private void OnOpenPressed()
    {
        FileIOManager.OpenLoadDialog("选择扩展包清单 (manifest.json)", "manifest.json", (path) => {
            SetProjectPath(path.GetBaseDir());
            LoadManifest();
            UpdateUIFromManifest();
            RefreshFileTree();
            ShowManifestView();
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("项目已打开！");
        });
    }

    private void OnCreateJsonIdPressed(long id)
    {
        if (string.IsNullOrEmpty(_projectPath)) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("请先打开项目！");
            return;
        }

        string fileName = id switch {
            0 => "Data/simulation.json",
            1 => "Data/actor_config.json",
            2 => "characters.json",
            _ => "Data/behavior.json"
        };
        string path = _projectPath.PathJoin(fileName);
        
        if (Godot.FileAccess.FileExists(path)) {
            DispatchFileView(path);
            return;
        }

        string template = id switch {
            // 养成数据模板：包含基础数值、心情、成长率等
            0 => "{\n  \"identity\": {\n    \"internal_id\": \"author:uma_id\",\n    \"full_name\": \"新马娘\",\n    \"short_name\": \"马娘\",\n    \"personality_id\": \"normal\"\n  },\n  \"stats\": {\n    \"initial\": {\n      \"speed\": 120, \"stamina\": 110, \"power\": 115, \"guts\": 95, \"intelligence\": 85, \"skill_points\": 10\n    },\n    \"conditions\": {\n      \"motivation\": 3,\n      \"energy\": 100,\n      \"affection\": 0\n    },\n    \"growth_bonus\": {\n      \"speed\": 0.20,\n      \"stamina\": 0.10,\n      \"power\": 0.0,\n      \"guts\": 0.0,\n      \"intelligence\": 0.0\n    },\n    \"custom_stats\": {}\n  }\n}",
            // 表现数据模板：包含立绘、表情、悬浮对话及语音
            1 => "{\n  \"actor_id\": \"author:uma_id\",\n  \"display_name\": \"新角色\",\n  \"visuals\": {\n    \"default_sprite\": \"Assets/Sprites/body_idle.png\",\n    \"expressions\": {\n      \"0\": \"Assets/Sprites/body_normal.png\"\n    },\n    \"stickers\": {}\n  },\n  \"barks\": [\n    {\n      \"condition\": \"energy < 30\",\n      \"text\": \"有点累了...\",\n      \"expression\": \"0\"\n    }\n  ],\n  \"audio\": {\n    \"typing_sound\": \"\",\n    \"fallback_voices\": []\n  }\n}",
            // 剧本客串角色模板：数组形式的表现数据
            2 => "[\n  {\n    \"actor_id\": \"guest_actor_01\",\n    \"display_name\": \"客串角色\",\n    \"visuals\": {\n      \"default_sprite\": \"\",\n      \"expressions\": {},\n      \"stickers\": {}\n    },\n    \"barks\": [],\n    \"audio\": {}\n  }\n]",
            // 行为包模板
            _ => "{\n  \"rules\": [\n    {\n      \"id\": \"rule_01\",\n      \"hook\": \"OnTraining\",\n      \"conditions\": [],\n      \"probability\": 1.0,\n      \"action\": {\n        \"type\": \"DetailedStory\",\n        \"path\": \"\"\n      }\n    }\n  ]\n}"
        };

        try {
            var dirPath = path.GetBaseDir();
            if (!DirAccess.DirExistsAbsolute(dirPath)) DirAccess.MakeDirRecursiveAbsolute(dirPath);

            using (var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write)) {
                file.StoreString(template);
            }
            RefreshFileTree();
            DispatchFileView(path);
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"已创建: {fileName.GetFile()}");
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"创建失败: {ex.Message}");
        }
    }

    private void RefreshFileTree()
    {
        _fileTree.Clear();
        if (string.IsNullOrEmpty(_projectPath)) return;
        TreeItem root = _fileTree.CreateItem();
        root.SetText(0, _projectPath.GetFile());
        root.SetMetadata(0, ""); 
        ScanFolderToTree(_projectPath, root);
    }

    private void ScanFolderToTree(string path, TreeItem parent)
    {
        using var dir = Godot.DirAccess.Open(path);
        if (dir == null) return;
        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "") {
            if (fileName == "." || fileName == "..") { fileName = dir.GetNext(); continue; }
            string fullPath = path.PathJoin(fileName);
            TreeItem item = _fileTree.CreateItem(parent);
            item.SetText(0, fileName);
            item.SetMetadata(0, fullPath);
            item.SetSelectable(0, true);
            if (dir.CurrentIsDir()) { ScanFolderToTree(fullPath, item); }
            fileName = dir.GetNext();
        }
    }

    private void OnExportPressed()
    {
        if (string.IsNullOrEmpty(_projectPath)) return;
        SyncManifestFromUI();
        SaveManifest();
        string exportName = $"{_manifest.Id}_{_manifest.Version}.umaext";
        FileIOManager.OpenSaveDialog("导出扩展包", exportName, "*.umaext", (path) => {
            try {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                ZipFile.CreateFromDirectory(ProjectSettings.GlobalizePath(_projectPath), ProjectSettings.GlobalizePath(path), CompressionLevel.Optimal, false);
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("导出成功！");
            } catch (Exception ex) {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowErrorDialog("导出失败", ex.Message);
            }
        });
    }

    private void InitializeFolderStructure()
    {
        if (string.IsNullOrEmpty(_projectPath)) return;
        string[] dirs = { "Data", "Assets/Sprites", "Assets/Backgrounds", "Assets/Audio", "Logic" };
        foreach (string d in dirs) Godot.DirAccess.MakeDirRecursiveAbsolute(_projectPath.PathJoin(d));
    }

    private void ImportAsset(string subDir)
    {
        if (string.IsNullOrEmpty(_projectPath)) { GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("请先新建或打开一个项目！"); return; }
        FileIOManager.OpenLoadDialog("选择资源文件", "*.*", (sourcePath) => {
            string destPath = _projectPath.PathJoin(subDir).PathJoin(sourcePath.GetFile());
            if (Godot.DirAccess.CopyAbsolute(sourcePath, destPath) == Godot.Error.Ok) {
                RefreshFileTree();
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"导入成功: {sourcePath.GetFile()}");
            }
        });
    }

    private void LoadManifest()
    {
        string path = _projectPath.PathJoin("manifest.json");
        if (Godot.FileAccess.FileExists(path)) {
            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            _manifest = JsonSerializer.Deserialize<ExtensionManifest>(file.GetAsText()) ?? new();
        }
    }

    private void SaveManifest()
    {
        if (string.IsNullOrEmpty(_projectPath)) return;
        SyncManifestFromUI();
        string json = JsonSerializer.Serialize(_manifest, new JsonSerializerOptions { WriteIndented = true });
        using (var file = Godot.FileAccess.Open(_projectPath.PathJoin("manifest.json"), Godot.FileAccess.ModeFlags.Write)) {
            file.StoreString(json);
        }
    }

    private void UpdateUIFromManifest()
    {
        _idEdit.Text = _manifest.Id ?? ""; _nameEdit.Text = _manifest.Name ?? ""; _authorEdit.Text = _manifest.Author ?? "";
        _versionEdit.Text = _manifest.Version ?? ""; _minVerEdit.Text = _manifest.MinGameVersion ?? "";
        _descEdit.Text = _manifest.Description ?? ""; _typeOption.Selected = _manifest.Type == "gameplay" ? 1 : 0;
    }

    private void SyncManifestFromUI()
    {
        _manifest.Id = _idEdit.Text; _manifest.Name = _nameEdit.Text; _manifest.Author = _authorEdit.Text;
        _manifest.Version = _versionEdit.Text; _manifest.MinGameVersion = _minVerEdit.Text;
        _manifest.Description = _descEdit.Text; _manifest.Type = _typeOption.Selected == 1 ? "gameplay" : "character";
    }

    private void ShowBehaviorEditor(string path)
    {
        _behaviorEditorRoot.Show();
        _currentBehaviorPack = null;

        try {
            if (Godot.FileAccess.FileExists(path)) {
                using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                _currentBehaviorPack = string.IsNullOrWhiteSpace(json) ? new() : JsonSerializer.Deserialize<BehaviorPack>(json) ?? new();
            } else {
                _currentBehaviorPack = new();
            }

            RefreshBehaviorRulesUI();
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"加载失败: {ex.Message}");
            ShowCodeView(path);
        }
    }

    private void RefreshBehaviorRulesUI()
    {
        foreach (Node child in _behaviorRulesContainer.GetChildren()) child.QueueFree();
        if (_currentBehaviorPack == null) return;

        foreach (var rule in _currentBehaviorPack.Rules) {
            _behaviorRulesContainer.AddChild(CreateRuleUI(rule));
        }
    }

    private void OnAddBehaviorRulePressed()
    {
        if (_currentBehaviorPack == null) _currentBehaviorPack = new();
        var newRule = new BehaviorRule { Id = $"rule_{_currentBehaviorPack.Rules.Count + 1}", Hook = "OnTraining" };
        _currentBehaviorPack.Rules.Add(newRule);
        _behaviorRulesContainer.AddChild(CreateRuleUI(newRule));
    }

    private Control CreateRuleUI(BehaviorRule rule)
    {
        var panel = new PanelContainer();
        var vBox = new VBoxContainer();
        panel.AddChild(vBox);

        // Header: ID and Delete
        var header = new HBoxContainer();
        vBox.AddChild(header);
        
        var idEdit = new LineEdit { Text = rule.Id, PlaceholderText = "Rule ID", CustomMinimumSize = new Vector2(150, 0) };
        idEdit.TextChanged += (val) => rule.Id = val;
        header.AddChild(idEdit);

        header.AddChild(new Label { Text = " " + Tr("KEY_LABEL_HOOK") + " " });
        var hookOption = new OptionButton { CustomMinimumSize = new Vector2(150, 0) };
        string[] hooks = { "OnTraining", "OnOuting", "OnTurnStart", "OnTurnEnd", "OnRaceStart", "OnRaceEnd" };
        foreach (var h in hooks) hookOption.AddItem(h);
        hookOption.Text = rule.Hook;
        hookOption.ItemSelected += (idx) => rule.Hook = hookOption.GetItemText((int)idx);
        header.AddChild(hookOption);

        header.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        var btnDel = new Button { Text = Tr("KEY_BTN_DELETE_RULE"), Modulate = Colors.Salmon };
        btnDel.Pressed += () => {
            _currentBehaviorPack.Rules.Remove(rule);
            panel.QueueFree();
        };
        header.AddChild(btnDel);

        // Action
        var actionHBox = new HBoxContainer();
        vBox.AddChild(actionHBox);
        actionHBox.AddChild(new Label { Text = Tr("KEY_LABEL_ACTION_TYPE") + " " });
        var typeOption = new OptionButton();
        typeOption.AddItem("BriefStory"); typeOption.AddItem("DetailedStory");
        typeOption.Text = rule.Action.Type;
        typeOption.ItemSelected += (idx) => rule.Action.Type = typeOption.GetItemText((int)idx);
        actionHBox.AddChild(typeOption);

        actionHBox.AddChild(new Label { Text = " " + Tr("KEY_LABEL_PATH") + " " });
        var pathEdit = new LineEdit { Text = rule.Action.Path, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        pathEdit.TextChanged += (val) => rule.Action.Path = val;
        actionHBox.AddChild(pathEdit);

        // Conditions
        var condTitleHBox = new HBoxContainer();
        vBox.AddChild(condTitleHBox);
        condTitleHBox.AddChild(new Label { Text = Tr("KEY_LABEL_CONDITIONS") + " " });
        var btnAddCond = new Button { Text = "+" };
        condTitleHBox.AddChild(btnAddCond);

        var condVBox = new VBoxContainer();
        vBox.AddChild(condVBox);

        foreach (var cond in rule.Conditions) condVBox.AddChild(CreateConditionUI(cond, rule, condVBox));

        btnAddCond.Pressed += () => {
            var newCond = new BehaviorCondition { Property = "Player.Money", Operator = ">=", Value = "100" };
            rule.Conditions.Add(newCond);
            condVBox.AddChild(CreateConditionUI(newCond, rule, condVBox));
        };

        return panel;
    }

    private Control CreateConditionUI(BehaviorCondition cond, BehaviorRule rule, VBoxContainer parentVBox)
    {
        var hbox = new HBoxContainer();
        var propEdit = new LineEdit { Text = cond.Property, PlaceholderText = "Property", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        propEdit.TextChanged += (val) => cond.Property = val;
        hbox.AddChild(propEdit);

        var opOption = new OptionButton();
        string[] ops = { "==", "!=", ">", "<", ">=", "<=" };
        foreach (var op in ops) opOption.AddItem(op);
        opOption.Text = cond.Operator;
        opOption.ItemSelected += (idx) => cond.Operator = opOption.GetItemText((int)idx);
        hbox.AddChild(opOption);

        var valEdit = new LineEdit { Text = cond.Value, PlaceholderText = "Value" };
        valEdit.TextChanged += (val) => cond.Value = val;
        hbox.AddChild(valEdit);

        var btnDelCond = new Button { Text = "x", Modulate = Colors.Salmon };
        btnDelCond.Pressed += () => {
            rule.Conditions.Remove(cond);
            hbox.QueueFree();
        };
        hbox.AddChild(btnDelCond);

        return hbox;
    }

    private void OnSaveBehaviorPressed()
    {
        if (string.IsNullOrEmpty(_currentEditingFilePath) || _currentBehaviorPack == null) return;

        string absolutePath = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath(_currentEditingFilePath));
        if (!IsPathWithinProject(absolutePath)) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("保存失败: 文件不在项目内！");
            return;
        }

        try {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_currentBehaviorPack, options);
            using var file = Godot.FileAccess.Open(_currentEditingFilePath, Godot.FileAccess.ModeFlags.Write);
            file.StoreString(json);
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("行为包已保存！");
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"保存失败: {ex.Message}");
        }
    }
}
