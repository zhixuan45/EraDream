using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UmaEraArchive.Core.Extensions
{
    /// <summary>
    /// 扩展包管理界面逻辑
    /// </summary>
    public partial class ExtensionManagerUI : CanvasLayer
    {
        public static ExtensionManagerUI Instance { get; private set; }

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
            
            // 安全信息
            _riskContainer.Visible = manifest.Type == PackType.Gameplay;
            if (_riskContainer.Visible)
            {
                _riskList.Clear();
                if (manifest.DetectedPermissions.Count > 0)
                {
                    _riskWarningLabel.Text = "检测到以下敏感权限:";
                    _riskWarningLabel.Modulate = Colors.OrangeRed;
                    foreach (var risk in manifest.DetectedPermissions)
                    {
                        _riskList.AddItem(risk);
                    }
                }
                else
                {
                    _riskWarningLabel.Text = "未检测到已知风险调用。";
                    _riskWarningLabel.Modulate = Colors.LightGreen;
                    _riskList.AddItem("无敏感权限请求");
                }
            }

            // 状态与按钮：支持反激活/关闭功能
            bool isActive = ExtensionManager.Instance.IsExtensionActive(manifest.Id);
            _activateBtn.Text = isActive ? "关闭激活" : (manifest.IsRisky ? "同意并激活" : "激活");
            _activateBtn.Disabled = false;
            
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
                if (_selectedManifest.IsRisky)
                {
                    _selectedManifest.IsAuthorized = true;
                    GD.Print($"[ExtensionManagerUI] User accepted risks for {_selectedManifest.Id}");
                }

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
    }
}
