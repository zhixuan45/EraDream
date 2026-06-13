using Godot;
using System;
using System.Linq;
using UmaEraArchive.Core;
using UmaEraArchive.Core.Extensions;
using umaEraArchive.Game.Models;

namespace umaEraArchive.Game.UI;

/// <summary>
/// 背包界面控制器 - 支持响应式布局与弹窗详情
/// </summary>
public partial class InventoryUI : Control
{
    private GridContainer _itemGrid;
    private Control _detailModal;
    private TextureRect _detailIcon;
    private Label _detailName;
    private Label _detailDesc;
    private Label _detailStats;
    private Button _useButton;
    private Button _cancelButton;
    private Button _closeInventoryButton;
    private PanelContainer _mainPanel;

    private string _selectedItemId = null;

    public override void _Ready()
    {
        // 强制设置全屏锚点，防止动态加载时失效
        SetAnchorsPreset(LayoutPreset.FullRect);
        
        // 绑定 UI 组件
        _itemGrid = GetNode<GridContainer>("%ItemGrid");
        _detailModal = GetNode<Control>("%DetailModal");
        _detailIcon = GetNode<TextureRect>("%DetailIcon");
        _detailName = GetNode<Label>("%DetailName");
        _detailDesc = GetNode<Label>("%DetailDesc");
        _detailStats = GetNode<Label>("%DetailStats");
        _useButton = GetNode<Button>("%UseButton");
        _cancelButton = GetNode<Button>("%CancelButton");
        _closeInventoryButton = GetNode<Button>("%CloseInventoryButton");
        _mainPanel = GetNode<PanelContainer>("SafeArea/CenterContainer/MainPanel");

        // 绑定事件
        _useButton.Pressed += OnUseButtonPressed;
        _cancelButton.Pressed += () => _detailModal.Visible = false;
        _closeInventoryButton.Pressed += OnCloseInventoryPressed;
        
        // 背景遮罩点击关闭 (如果是 ColorRect 可以通过 GuiInput 监听)
        var mask = GetNode<Control>("Mask");
        mask.GuiInput += (ev) => {
            if (ev is InputEventMouseButton btn && btn.Pressed && btn.ButtonIndex == MouseButton.Left)
            {
                OnCloseInventoryPressed();
            }
        };

        // 监听响应式布局变化
        if (ResponsiveManager.Instance != null)
        {
            ResponsiveManager.Instance.OnOrientationChanged += UpdateLayout;
            // 延迟调用以确保视口尺寸已刷新
            CallDeferred(nameof(UpdateLayout), ResponsiveManager.Instance.CurrentOrientation == ScreenOrientation.Landscape);
        }

        // 初始化
        _detailModal.Visible = false;
        RefreshUI();
    }

    public override void _ExitTree()
    {
        if (ResponsiveManager.Instance != null)
        {
            ResponsiveManager.Instance.OnOrientationChanged -= UpdateLayout;
        }
    }

    /// <summary>
    /// 根据屏幕方向调整布局
    /// </summary>
    private void UpdateLayout(bool isLandscape)
    {
        Vector2 screenSize = GetViewportRect().Size;
        
        if (isLandscape)
        {
            _mainPanel.CustomMinimumSize = new Vector2(Math.Min(800, screenSize.X * 0.8f), Math.Min(600, screenSize.Y * 0.8f));
            _itemGrid.Columns = 6;
        }
        else
        {
            _mainPanel.CustomMinimumSize = new Vector2(screenSize.X * 0.9f, screenSize.Y * 0.7f);
            _itemGrid.Columns = 3;
        }
    }

    private Vector2 CurrentScreenSize => GetViewportRect().Size;

    /// <summary>
    /// 刷新背包列表
    /// </summary>
    public void RefreshUI()
    {
        // 清空现有格子
        foreach (Node child in _itemGrid.GetChildren())
        {
            child.QueueFree();
        }

        var state = GameManager.Instance?.CurrentState;
        if (state == null) return;

        // 填充物品
        foreach (var itemPair in state.Inventory.Items)
        {
            string itemId = itemPair.Key;
            int count = itemPair.Value;
            var def = BehaviorRegistry.Instance.GetItemDefinition(itemId);
            if (def == null) continue;

            // 创建物品按钮
            var btn = new Button
            {
                Text = $"{def.Name}\nx{count}",
                CustomMinimumSize = new Vector2(100, 100),
                ClipText = true,
                // 显式指定 Theme Type 以确保样式一致
                ThemeTypeVariation = "InventoryItemButton" 
            };

            // 如果有图标则显示图标
            if (!string.IsNullOrEmpty(def.IconPath))
            {
                var tex = ResourceProxy.LoadImageTexture(ProjectSettings.GlobalizePath(def.IconPath));
                if (tex != null)
                {
                    btn.Icon = tex;
                    btn.ExpandIcon = true;
                    btn.IconAlignment = HorizontalAlignment.Center;
                    btn.VerticalIconAlignment = VerticalAlignment.Top;
                }
            }

            btn.Pressed += () => SelectItem(itemId);
            _itemGrid.AddChild(btn);
        }

        // 如果选中的物品不再存在，隐藏详情弹窗
        if (_selectedItemId != null && !state.Inventory.Items.ContainsKey(_selectedItemId))
        {
            _detailModal.Visible = false;
            _selectedItemId = null;
        }
    }

    private void SelectItem(string itemId)
    {
        _selectedItemId = itemId;
        var state = GameManager.Instance.CurrentState;
        var def = BehaviorRegistry.Instance.GetItemDefinition(itemId);
        
        if (!state.Inventory.Items.TryGetValue(itemId, out int count))
        {
            _detailModal.Visible = false;
            return;
        }

        // 更新详情内容
        _detailName.Text = def.Name;
        _detailDesc.Text = def.Description;
        
        string stats = $"持有数量: {count} / {def.MaxStack}\n类型: {GetTypeName(def.Type)}";
        if (def.Type == ItemType.Duration)
        {
            stats += $"\n持续回合: {def.DurationTurns}";
            var active = state.Inventory.ActiveEffects.FirstOrDefault(e => e.ItemId == itemId);
            if (active != null)
            {
                stats += $"\n(当前生效中: 剩余 {active.RemainingTurns} 回合)";
            }
        }
        _detailStats.Text = stats;

        // 加载图标
        if (!string.IsNullOrEmpty(def.IconPath))
        {
            _detailIcon.Texture = ResourceProxy.LoadImageTexture(ProjectSettings.GlobalizePath(def.IconPath));
        }
        else
        {
            _detailIcon.Texture = null;
        }

        // 设置按钮状态
        _useButton.Disabled = def.Type == ItemType.Permanent;
        
        // 显示详情弹窗
        _detailModal.Visible = true;
    }

    private string GetTypeName(ItemType type) => type switch
    {
        ItemType.Consumable => "消耗品",
        ItemType.Duration => "持续物品",
        ItemType.Permanent => "长期持有",
        _ => "未知"
    };

    private void OnUseButtonPressed()
    {
        if (string.IsNullOrEmpty(_selectedItemId)) return;

        var state = GameManager.Instance?.CurrentState;
        if (state == null) return;

        if (GameManager.Instance.Inventory.UseItem(state, _selectedItemId))
        {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"使用了物品: {BehaviorRegistry.Instance.GetItemDefinition(_selectedItemId).Name}");
            RefreshUI();
            
            // 如果物品用完了，关闭弹窗
            if (!state.Inventory.Items.ContainsKey(_selectedItemId))
            {
                _detailModal.Visible = false;
            }
            else
            {
                // 否则刷新弹窗内的数值
                SelectItem(_selectedItemId);
            }
        }
    }

    private void OnCloseInventoryPressed()
    {
        GD.Print("[InventoryUI] Closing inventory...");
        _detailModal.Visible = false;
        Visible = false;
        
        // 触发一个刷新信号或确保状态同步
        if (GameManager.Instance != null)
        {
            // 如果养成界面需要感应背包关闭（例如刷新金钱），可以在这里处理
        }
    }
}
