using Godot;
using System;

namespace UmaEraArchive.Core.Extensions
{
    public partial class ExtensionManagerUI
    {
        private ItemList _itemList;
        private Control _detailsPanel;
        private Label _nameLabel;
        private Label _authorLabel;
        private RichTextLabel _descLabel;
        private VBoxContainer _riskContainer;
        private Label _riskWarningLabel;
        private ItemList _riskList;
        private Button _activateBtn;

        private void InitUI()
        {
            // 创建暗色遮罩背景
            var bgOverlay = new ColorRect();
            bgOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            bgOverlay.Color = new Color(0, 0, 0, 0.7f);
            AddChild(bgOverlay);

            var mainPanel = new PanelContainer();
            mainPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
            mainPanel.CustomMinimumSize = new Vector2(900, 600);
            AddChild(mainPanel);

            var styleBox = new StyleBoxFlat();
            styleBox.BgColor = new Color(0.12f, 0.12f, 0.12f);
            styleBox.SetCornerRadiusAll(8);
            styleBox.ContentMarginLeft = 20;
            styleBox.ContentMarginRight = 20;
            styleBox.ContentMarginTop = 15;
            styleBox.ContentMarginBottom = 15;
            mainPanel.AddThemeStyleboxOverride("panel", styleBox);

            var layout = new VBoxContainer();
            layout.AddThemeConstantOverride("separation", 15);
            mainPanel.AddChild(layout);

            // 标题栏
            var header = new HBoxContainer();
            layout.AddChild(header);

            var title = new Label();
            title.Text = "扩展包管理器";
            title.AddThemeFontSizeOverride("font_size", 28);
            header.AddChild(title);

            header.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

            var closeBtn = new Button { Text = " 关闭 ", CustomMinimumSize = new Vector2(80, 0) };
            closeBtn.Pressed += HideUI;
            header.AddChild(closeBtn);

            // 分割区
            var split = new HSplitContainer();
            split.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            layout.AddChild(split);

            // 左侧列表
            var listVBox = new VBoxContainer();
            listVBox.CustomMinimumSize = new Vector2(300, 0);
            split.AddChild(listVBox);

            listVBox.AddChild(new Label { Text = "已安装列表" });
            
            _itemList = new ItemList();
            _itemList.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _itemList.ItemSelected += OnItemSelected;
            listVBox.AddChild(_itemList);

            // 右侧详情
            _detailsPanel = new VBoxContainer();
            _detailsPanel.AddThemeConstantOverride("separation", 10);
            _detailsPanel.Visible = false;
            split.AddChild(_detailsPanel);

            _nameLabel = new Label();
            _nameLabel.AddThemeFontSizeOverride("font_size", 22);
            _detailsPanel.AddChild(_nameLabel);

            _authorLabel = new Label();
            _authorLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
            _detailsPanel.AddChild(_authorLabel);

            _detailsPanel.AddChild(new HSeparator());

            _detailsPanel.AddChild(new Label { Text = "描述:" });
            _descLabel = new RichTextLabel();
            _descLabel.CustomMinimumSize = new Vector2(0, 100);
            _descLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _detailsPanel.AddChild(_descLabel);

            // 风险扫描区
            _riskContainer = new VBoxContainer();
            _riskContainer.AddThemeConstantOverride("separation", 5);
            _detailsPanel.AddChild(_riskContainer);

            _riskContainer.AddChild(new HSeparator());
            _riskWarningLabel = new Label { Text = "安全性扫描结果:" };
            _riskContainer.AddChild(_riskWarningLabel);

            _riskList = new ItemList();
            _riskList.CustomMinimumSize = new Vector2(0, 120);
            _riskContainer.AddChild(_riskList);

            // 底部操作
            _detailsPanel.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
            _activateBtn = new Button { Text = "激活", CustomMinimumSize = new Vector2(200, 50) };
            _activateBtn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            _activateBtn.Pressed += OnActivatePressed;
            _detailsPanel.AddChild(_activateBtn);
        }
    }
}
