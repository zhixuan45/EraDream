using Godot;
using System;

namespace EraDream.Core.Extensions
{
    // 扩展包管理器界面UI布局组件
    public partial class ExtensionManagerUI
    {
        private Button _importBtn;
        private ItemList _itemList;
        private Control _detailsPanel;
        private Label _nameLabel;
        private Label _authorLabel;
        private RichTextLabel _descLabel;
        private VBoxContainer _riskContainer;
        private Label _riskWarningLabel;
        private ItemList _riskList;
        private Button _activateBtn;
        private Tree _configTree;

        private void InitUI()
        {
            // 填满屏幕的 Control 根容器
            var rootControl = new Control();
            rootControl.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            rootControl.GrowHorizontal = Control.GrowDirection.Both;
            rootControl.GrowVertical = Control.GrowDirection.Both;
            AddChild(rootControl);

            // 遮罩背景
            var bgOverlay = new ColorRect();
            bgOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            bgOverlay.Color = new Color(0, 0, 0, 0.6f);
            rootControl.AddChild(bgOverlay);

            // 居中面板容器
            var mainPanel = new PanelContainer();
            mainPanel.CustomMinimumSize = new Vector2(950, 650);
            mainPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
            mainPanel.GrowHorizontal = Control.GrowDirection.Both;
            mainPanel.GrowVertical = Control.GrowDirection.Both;
            mainPanel.PivotOffset = new Vector2(475, 325);
            rootControl.AddChild(mainPanel);

            // 高端深色圆角 StyleBox
            var styleBox = new StyleBoxFlat();
            styleBox.BgColor = new Color(0.1f, 0.1f, 0.12f, 0.98f);
            styleBox.BorderWidthLeft = 2;
            styleBox.BorderWidthTop = 2;
            styleBox.BorderWidthRight = 2;
            styleBox.BorderWidthBottom = 2;
            styleBox.BorderColor = new Color(0.2f, 0.3f, 0.55f, 0.9f);
            styleBox.SetCornerRadiusAll(16);
            styleBox.ContentMarginLeft = 25;
            styleBox.ContentMarginRight = 25;
            styleBox.ContentMarginTop = 20;
            styleBox.ContentMarginBottom = 20;
            styleBox.ShadowColor = new Color(0, 0, 0, 0.5f);
            styleBox.ShadowSize = 12;
            styleBox.ShadowOffset = new Vector2(0, 6);
            mainPanel.AddThemeStyleboxOverride("panel", styleBox);

            var layout = new VBoxContainer();
            layout.AddThemeConstantOverride("separation", 15);
            mainPanel.AddChild(layout);

            // 头部标题栏
            var header = new HBoxContainer();
            layout.AddChild(header);

            var title = new Label();
            title.Text = "扩展包与模组中心";
            title.AddThemeFontSizeOverride("font_size", 26);
            header.AddChild(title);

            header.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

            var closeBtn = new Button { Text = " 关闭 ", CustomMinimumSize = new Vector2(80, 0) };
            closeBtn.Pressed += HideUI;
            header.AddChild(closeBtn);

            // 左右分割面板
            var split = new HSplitContainer();
            split.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            split.SplitOffsets = new int[] { 320 };
            layout.AddChild(split);

            // 左侧面板：包含导入按钮与ItemList
            var leftVBox = new VBoxContainer();
            leftVBox.AddThemeConstantOverride("separation", 10);
            leftVBox.CustomMinimumSize = new Vector2(300, 0);
            split.AddChild(leftVBox);

            _importBtn = new Button { 
                Text = " 导入扩展包 (.umaext) ", 
                CustomMinimumSize = new Vector2(0, 40) 
            };
            _importBtn.Pressed += OnImportPressed;
            leftVBox.AddChild(_importBtn);

            leftVBox.AddChild(new Label { Text = "已安装的扩展包:" });

            _itemList = new ItemList();
            _itemList.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _itemList.ItemSelected += OnItemSelected;
            leftVBox.AddChild(_itemList);

            // 右侧面板：包信息与Tab配置浏览
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

            // Tab容器：切换“详情”和“配置内容”
            var tabs = new TabContainer();
            tabs.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _detailsPanel.AddChild(tabs);

            // Tab 1: 详情面板
            var detailsTab = new VBoxContainer();
            detailsTab.Name = " 基本信息 ";
            detailsTab.AddThemeConstantOverride("separation", 12);
            tabs.AddChild(detailsTab);

            detailsTab.AddChild(new Label { Text = "扩展包描述:" });
            _descLabel = new RichTextLabel();
            _descLabel.CustomMinimumSize = new Vector2(0, 100);
            _descLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            detailsTab.AddChild(_descLabel);

            _riskContainer = new VBoxContainer();
            _riskContainer.AddThemeConstantOverride("separation", 5);
            detailsTab.AddChild(_riskContainer);

            _riskWarningLabel = new Label { Text = "安全性扫描结果:" };
            _riskContainer.AddChild(_riskWarningLabel);

            _riskList = new ItemList();
            _riskList.CustomMinimumSize = new Vector2(0, 100);
            _riskContainer.AddChild(_riskList);

            _activateBtn = new Button { Text = "激活", CustomMinimumSize = new Vector2(200, 45) };
            _activateBtn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            _activateBtn.Pressed += OnActivatePressed;
            detailsTab.AddChild(_activateBtn);

            // Tab 2: 行为配置与物品浏览面板
            var configTab = new VBoxContainer();
            configTab.Name = " 配置内容 ";
            configTab.AddThemeConstantOverride("separation", 10);
            tabs.AddChild(configTab);

            configTab.AddChild(new Label { Text = "声明配置预览 (从 behavior.json 自动提取):" });

            _configTree = new Tree();
            _configTree.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _configTree.HideRoot = true;
            configTab.AddChild(_configTree);
        }
    }
}
