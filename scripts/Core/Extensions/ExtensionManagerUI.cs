using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EraDream.Core;

namespace EraDream.Core.Extensions
{
    // 扩展包管理器界面逻辑控制器
    public partial class ExtensionManagerUI : CanvasLayer
    {
        public static ExtensionManagerUI Instance { get; private set; }

        private const string ExtDir = "user://extensions";

        private ExtensionManifest _selectedManifest;

        public override void _Ready()
        {
            Instance = this;
            Layer = 80; // 低于 SettingsOverlay
            InitUI();
            
            // 初始隐藏
            Visible = false;
        }

        public void ShowUI()
        {
            Visible = true;
            RefreshList();
        }

        public void HideUI()
        {
            Visible = false;
        }

        private void RefreshList()
        {
            if (ExtensionManager.Instance == null) return;
            
            ExtensionManager.Instance.ScanExtensions();
            var extensions = ExtensionManager.Instance.GetAvailableExtensions();
            
            _itemList.Clear();
            foreach (var ext in extensions)
            {
                int idx = _itemList.AddItem($"{ext.Name} (v{ext.Version})");
                _itemList.SetItemMetadata(idx, ext.Id);
                
                if (ExtensionManager.Instance.IsExtensionActive(ext.Id))
                {
                    _itemList.SetItemCustomFgColor(idx, new Color(0.4f, 1.0f, 0.4f));
                }
            }
            
            _detailsPanel.Visible = false;
        }

        private void OnItemSelected(long index)
        {
            string id = _itemList.GetItemMetadata((int)index).AsString();
            _selectedManifest = ExtensionManager.Instance.GetAvailableExtensions().FirstOrDefault(e => e.Id == id);
            
            if (_selectedManifest != null)
            {
                UpdateDetails(_selectedManifest);
            }
        }

        private void UpdateDetails(ExtensionManifest manifest)
        {
            _nameLabel.Text = $"{manifest.Name} [ID: {manifest.Id}]";
            _authorLabel.Text = $"作者: {manifest.Author} | 版本: {manifest.Version}";
            _descLabel.Text = manifest.Description ?? "无描述";
            
            // 安全信息已经彻底废除，隐藏警告区域
            _riskContainer.Visible = false;

            // 状态与按钮：支持反激活/关闭功能
            bool isActive = ExtensionManager.Instance.IsExtensionActive(manifest.Id);
            _activateBtn.Text = isActive ? "关闭激活" : "激活";
            _activateBtn.Disabled = false;

            // 异步预览已有配置的 behavior.json 内容
            string jsonContent = GetBehaviorJsonContent(manifest);
            PopulateConfigTree(jsonContent);
            
            _detailsPanel.Visible = true;
        }

        private async void OnActivatePressed()
        {
            if (_selectedManifest == null) return;

            bool isActive = ExtensionManager.Instance.IsExtensionActive(_selectedManifest.Id);
            if (isActive)
            {
                // 关闭停用扩展包
                ExtensionManager.Instance.DeactivateExtension(_selectedManifest.Id);
                RefreshList();
                UpdateDetails(_selectedManifest);
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"扩展包 {_selectedManifest.Name} 已关闭激活");
            }
            else
            {
                // 激活扩展包
                bool success = await ExtensionManager.Instance.ActivateExtension(_selectedManifest.Id);
                if (success)
                {
                    RefreshList();
                    UpdateDetails(_selectedManifest);
                    GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"扩展包 {_selectedManifest.Name} 已激活");
                }
                else
                {
                    GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"激活失败: {_selectedManifest.Id}");
                }
            }
        }

        // 递归复制整个目录的内容
        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relPath = Path.GetRelativePath(sourceDir, file);
                string destFile = Path.Combine(destDir, relPath);
                string destSubDir = Path.GetDirectoryName(destFile);
                if (!Directory.Exists(destSubDir)) Directory.CreateDirectory(destSubDir);
                File.Copy(file, destFile, true);
            }
        }

        // 调用原生文件对话框导入 .umaext 压缩包或选择 manifest.json 导入文件夹扩展
        private void OnImportPressed()
        {
            FileIOManager.OpenLoadDialog("选择导入的扩展包 (*.umaext, manifest.json)", "*.umaext, *.json", (path) =>
            {
                if (string.IsNullOrEmpty(path)) return;

                try
                {
                    string fileName = Path.GetFileName(path).ToLower();
                    string destDir = ProjectSettings.GlobalizePath(ExtDir);
                    if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                    if (fileName == "manifest.json")
                    {
                        string sourceDir = Path.GetDirectoryName(path);
                        string jsonContent = File.ReadAllText(path);
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var manifest = JsonSerializer.Deserialize<ExtensionManifest>(jsonContent, options);
                        
                        if (manifest == null || string.IsNullOrWhiteSpace(manifest.Id))
                        {
                            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("无效的 manifest.json，无法识别扩展 ID");
                            return;
                        }

                        if (!ExtensionManager.Instance.ValidateManifest(manifest)) return;

                        string targetFolder = Path.Combine(destDir, manifest.Id);
                        CopyDirectory(sourceDir, targetFolder);
                        GD.Print($"[ExtensionManagerUI] Successfully imported folder extension to: {targetFolder}");
                        GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"文件夹扩展包 {manifest.Name} 导入成功！");
                    }
                    else if (fileName.EndsWith(".umaext"))
                    {
                        string destPath = Path.Combine(destDir, Path.GetFileName(path));
                        File.Copy(path, destPath, true);
                        GD.Print($"[ExtensionManagerUI] Successfully imported archived extension: {destPath}");
                        GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"压缩扩展包 {fileName} 导入成功！");
                    }
                    else
                    {
                        GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("不支持的文件格式，仅支持 .umaext 或 manifest.json");
                        return;
                    }

                    RefreshList();
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[ExtensionManagerUI] Failed to import: {ex.Message}");
                    GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"导入失败: {ex.Message}");
                }
            });
        }

        // 从活动缓存或扩展源中读取 behavior.json 内容
        private string GetBehaviorJsonContent(ExtensionManifest manifest)
        {
            string rawPath = ExtensionManager.Instance.GetRawExtensionPath(manifest.Id);
            if (string.IsNullOrEmpty(rawPath)) return null;

            try
            {
                if (rawPath.EndsWith(".umaext"))
                {
                    using var reader = new ZipReader();
                    if (reader.Open(rawPath) == Error.Ok && reader.FileExists("Logic/behavior.json"))
                    {
                        byte[] data = reader.ReadFile("Logic/behavior.json");
                        return System.Text.Encoding.UTF8.GetString(data);
                    }
                }
                else
                {
                    string behaviorPath = Path.Combine(rawPath, "Logic", "behavior.json");
                    if (File.Exists(behaviorPath)) return File.ReadAllText(behaviorPath);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ExtensionManagerUI] Error reading behavior.json: {ex.Message}");
            }
            return null;
        }

        // 解析并层级渲染配置内容
        private void PopulateConfigTree(string jsonContent)
        {
            _configTree.Clear();
            var root = _configTree.CreateItem();
            
            if (string.IsNullOrEmpty(jsonContent))
            {
                var emptyNode = _configTree.CreateItem(root);
                emptyNode.SetText(0, "此扩展包未声明任何行为规则或自定义物品配置 (无 behavior.json)");
                return;
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var pack = JsonSerializer.Deserialize<BehaviorPack>(jsonContent, options);
                if (pack == null) return;

                // 1. 渲染规则 (Rules)
                if (pack.Rules != null && pack.Rules.Count > 0)
                {
                    var rulesCategory = _configTree.CreateItem(root);
                    rulesCategory.SetText(0, $"⚡ 行为规则 (Rules) [{pack.Rules.Count}个]");
                    
                    foreach (var rule in pack.Rules)
                    {
                        var ruleNode = _configTree.CreateItem(rulesCategory);
                        string overrideStr = rule.Override ? " [覆写]" : "";
                        ruleNode.SetText(0, $"• Hook: {rule.Hook} | ID: {rule.Id}{overrideStr} (概率: {rule.Probability * 100}%)");
                        
                        if (rule.Conditions != null && rule.Conditions.Count > 0)
                        {
                            var condsNode = _configTree.CreateItem(ruleNode);
                            condsNode.SetText(0, "  条件 (Conditions):");
                            foreach (var cond in rule.Conditions)
                            {
                                var sub = _configTree.CreateItem(condsNode);
                                sub.SetText(0, $"    - {cond.Property} {cond.Operator} {cond.Value}");
                            }
                        }
                        var actionNode = _configTree.CreateItem(ruleNode);
                        actionNode.SetText(0, $"  动作: {rule.Action.Type} -> {rule.Action.Path}");
                    }
                }

                // 2. 渲染物品定义 (Items)
                if (pack.Items != null && pack.Items.Count > 0)
                {
                    var itemsCategory = _configTree.CreateItem(root);
                    itemsCategory.SetText(0, $"📦 道具与物品 (Items) [{pack.Items.Count}个]");

                    foreach (var item in pack.Items)
                    {
                        var itemNode = _configTree.CreateItem(itemsCategory);
                        string overrideStr = item.Override ? " [覆写]" : "";
                        itemNode.SetText(0, $"• {item.Name} [ID: {item.Id}]{overrideStr} (最大堆叠: {item.MaxStack})");
                        
                        var sub = _configTree.CreateItem(itemNode);
                        sub.SetText(0, $"  描述: {item.Description}");
                    }
                }

                // 3. 渲染动态 UI 选项 (Menus)
                if (pack.Menus != null && pack.Menus.Count > 0)
                {
                    var menusCategory = _configTree.CreateItem(root);
                    menusCategory.SetText(0, $"🎨 动态UI菜单 (Menus) [{pack.Menus.Count}个]");

                    foreach (var menu in pack.Menus)
                    {
                        var menuNode = _configTree.CreateItem(menusCategory);
                        menuNode.SetText(0, $"• 菜单 ID: {menu.MenuId}");

                        foreach (var opt in menu.Options)
                        {
                            var optNode = _configTree.CreateItem(menuNode);
                            string overrideStr = opt.Override ? " [覆写]" : "";
                            optNode.SetText(0, $"  - 选项: {opt.Name} [ID: {opt.Id}]{overrideStr} (动作: {opt.Action.Type})");
                        }
                    }
                }

                // 4. 渲染赛马赛事 (Races)
                if (pack.Races != null && pack.Races.Count > 0)
                {
                    var racesCategory = _configTree.CreateItem(root);
                    racesCategory.SetText(0, $"🏁 赛马赛事 (Races) [{pack.Races.Count}个]");

                    foreach (var race in pack.Races)
                    {
                        var raceNode = _configTree.CreateItem(racesCategory);
                        string overrideStr = race.Override ? " [覆写]" : "";
                        raceNode.SetText(0, $"• 赛事: {race.Name} [ID: {race.Id}]{overrideStr} (举办回合: 第{race.Turn}回合)");
                        
                        var detailNode = _configTree.CreateItem(raceNode);
                        detailNode.SetText(0, $"  门槛: 速度 >= {race.MinSpeed} | 奖励: {race.RewardStat} +{race.RewardValue}");
                    }
                }
            }
            catch (Exception ex)
            {
                var errorNode = _configTree.CreateItem(root);
                errorNode.SetText(0, $"配置解析出错: {ex.Message}");
            }
        }
    }
}
