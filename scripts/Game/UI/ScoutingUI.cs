using Godot;
using System;
using System.Collections.Generic;
using EraDream.Core;
using EraDream.Core.Models;

namespace EraDream.Game.UI
{
    /// <summary>
    /// 运动场签约面板控制器，处理马娘展示、金币刷新与签约逻辑
    /// </summary>
    public partial class ScoutingUI : CanvasLayer
    {
        // 签约成功回调信号
        public event Action ContractSigned;

        private Control _scoutPanel;
        private HBoxContainer _cardsContainer;
        private Button _btnRefresh;
        private Button _btnClose;
        private Label _lblMoney;

        // 记录当前选中的马娘卡片 ID
        private string _selectedUmaId = "";

        public override void _Ready()
        {
            // 绑定并查找子节点
            _scoutPanel = GetNode<Control>("Panel");
            _cardsContainer = GetNode<HBoxContainer>("Panel/CardsContainer");
            _btnRefresh = GetNode<Button>("Panel/BtnRefresh");
            _btnClose = GetNode<Button>("Panel/BtnClose");
            _lblMoney = GetNode<Label>("Panel/LblMoney");

            // 绑定按钮事件
            _btnRefresh.Pressed += OnRefreshPressed;
            _btnClose.Pressed += () => QueueFree();

            RefreshUI();
        }

        /// <summary>
        /// 刷新运动场界面与马娘卡片
        /// </summary>
        public void RefreshUI()
        {
            var state = GameManager.Instance.CurrentState;
            if (state == null) return;

            // 清空旧卡片
            foreach (Node child in _cardsContainer.GetChildren())
            {
                child.QueueFree();
            }

            _lblMoney.Text = $"训练员资金: {state.Player.Money} 金币";

            // 循环生成当前签约池中的马娘卡片
            foreach (string umaId in state.CurrentScoutPool)
            {
                var card = CreateUmaCard(umaId);
                _cardsContainer.AddChild(card);
            }
        }

        /// <summary>
        /// 动态为每个马娘构建详情展示卡片
        /// </summary>
        private PanelContainer CreateUmaCard(string umaId)
        {
            var container = new PanelContainer {
                CustomMinimumSize = new Vector2(240, 360),
                Name = $"Card_{umaId}"
            };

            var vbox = new VBoxContainer {
                Alignment = BoxContainer.AlignmentMode.Center
            };
            vbox.AddThemeConstantOverride("separation", 15);
            container.AddChild(vbox);

            // 获取马娘表现配置与养成配置
            var actor = CharacterManager.GetActor(umaId);
            var sim = CharacterManager.LoadUmaSimulationData(umaId);
            string name = actor != null ? actor.DisplayName : "未知马娘";
            string personality = sim != null ? sim.Identity.PersonalityId : "普通";
            
            // 1. 马娘姓名 Label
            var lblName = new Label {
                Text = name,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            lblName.AddThemeFontSizeOverride("font_size", 22);
            vbox.AddChild(lblName);

            // 2. 马娘预览图 (立绘)
            var textureRect = new TextureRect {
                CustomMinimumSize = new Vector2(180, 200),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            };
            
            // 动态加载立绘材质，支持扩展包的相对路径解析
            string defaultSprite = actor?.Visuals.DefaultSprite;
            if (!string.IsNullOrEmpty(defaultSprite))
            {
                // 使用重载方法直接加载，自动识别并载入扩展包绝对物理路径下的立绘。
                ImageTexture texture = ResourceProxy.LoadSpriteTexture(defaultSprite, umaId);
                if (texture != null) textureRect.Texture = texture;
            }
            vbox.AddChild(textureRect);

            // 3. 马娘性格信息
            var lblDesc = new Label {
                Text = $"性格: {personality}",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            vbox.AddChild(lblDesc);

            // 4. 签约 Button
            var btnContract = new Button {
                Text = "进行签约 (Contract)",
                CustomMinimumSize = new Vector2(160, 40)
            };
            btnContract.Pressed += () => OnContractPressed(umaId);
            vbox.AddChild(btnContract);

            return container;
        }

        private void OnRefreshPressed()
        {
            // 每次刷新消耗 500 金币
            if (GameManager.Instance.RefreshScoutPoolWithCost(500))
            {
                RefreshUI();
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("成功消耗 500 金币刷新马娘池！");
            }
            else
            {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("资金不足，无法刷新！");
            }
        }

        private void OnContractPressed(string umaId)
        {
            // 触发 GameManager 签约逻辑
            if (GameManager.Instance.ContractUma(umaId))
            {
                ContractSigned?.Invoke();
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"签约成功！马娘 {umaId} 已加入训练组。");
                QueueFree();
            }
            else
            {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("签约失败！");
            }
        }
    }
}
