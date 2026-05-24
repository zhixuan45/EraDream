using Godot;
using System;
using System.Collections.Generic;
using umaEraArchive.Game;
using UmaEraArchive.Core.Extensions;

namespace UmaEraArchive.Game.UI
{
    /// <summary>
    /// 独立的训练菜单 UI 场景控制逻辑
    /// </summary>
    public partial class TrainingMenuUI : Control
    {
        [Signal] public delegate void TrainingSelectedEventHandler(int type);
        [Signal] public delegate void DynamicOptionSelectedEventHandler(string menuId, string optionId);
        [Signal] public delegate void CloseRequestedEventHandler();

        private VBoxContainer _listContainer;
        private AnimationPlayer _animPlayer;
        private Button _btnClose;

        public override void _Ready()
        {
            _listContainer = GetNode<VBoxContainer>("PanelContainer/MarginContainer/VBoxContainer/ScrollContainer/ListContainer");
            _animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
            _btnClose = GetNode<Button>("PanelContainer/MarginContainer/VBoxContainer/Header/BtnClose");

            _btnClose.Pressed += () => EmitSignal(SignalName.CloseRequested);

            SetupTrainingButtons();
            
            // 播放入场动画
            _animPlayer.Play("fade_in");
        }

        private void SetupTrainingButtons()
        {
            // 清理旧项
            foreach (Node child in _listContainer.GetChildren()) child.QueueFree();

            // 1. 加载默认项
            AddTrainingItem("速度训练", "提升速度与力量", (int)TrainingType.Speed, "res://icon.svg");
            AddTrainingItem("耐力训练", "提升耐力与根性", (int)TrainingType.Stamina, "res://icon.svg");
            AddTrainingItem("力量训练", "提升力量与耐力", (int)TrainingType.Power, "res://icon.svg");
            AddTrainingItem("根性训练", "提升根性、速度与力量", (int)TrainingType.Guts, "res://icon.svg");
            AddTrainingItem("智力训练", "提升智力、速度并恢复精力", (int)TrainingType.Intelligence, "res://icon.svg");

            // 2. 加载来自 BehaviorRegistry 的动态项
            if (BehaviorRegistry.Instance != null && GameManager.Instance?.CurrentState != null)
            {
                var options = BehaviorRegistry.Instance.GetValidOptions("Training", GameManager.Instance.CurrentState);
                foreach (var option in options)
                {
                    AddDynamicItem(option, "Training");
                }
            }
        }

        private void AddTrainingItem(string title, string desc, int type, string iconPath)
        {
            var btn = CreateBaseButton(title, desc, iconPath);
            btn.Pressed += () => {
                EmitSignal(SignalName.TrainingSelected, type);
                EmitSignal(SignalName.CloseRequested);
            };
            _listContainer.AddChild(btn);
        }

        private void AddDynamicItem(UIOption option, string menuId)
        {
            var btn = CreateBaseButton(option.Name, option.Description, option.Icon);
            btn.Pressed += () => {
                EmitSignal(SignalName.DynamicOptionSelected, menuId, option.Id);
                EmitSignal(SignalName.CloseRequested);
            };
            _listContainer.AddChild(btn);
        }

        private Button CreateBaseButton(string title, string desc, string iconPath)
        {
            var btn = new Button {
                CustomMinimumSize = new Vector2(0, 80),
                MouseDefaultCursorShape = CursorShape.PointingHand
            };
            
            var hbox = new HBoxContainer();
            hbox.SetAnchorsPreset(LayoutPreset.FullRect);
            hbox.MouseFilter = MouseFilterEnum.Ignore;
            btn.AddChild(hbox);

            var icon = new TextureRect {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(64, 64)
            };

            if (!string.IsNullOrEmpty(iconPath) && ResourceLoader.Exists(iconPath))
            {
                icon.Texture = GD.Load<Texture2D>(iconPath);
            }
            else
            {
                icon.Texture = GD.Load<Texture2D>("res://icon.svg");
            }
            hbox.AddChild(icon);

            var vbox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
            hbox.AddChild(vbox);

            vbox.AddChild(new Label { Text = title, ThemeTypeVariation = "HeaderLarge" });
            vbox.AddChild(new Label { Text = desc, Modulate = new Color(0.8f, 0.8f, 0.8f) });

            return btn;
        }

        public void Close()
        {
            _animPlayer.PlayBackwards("fade_in");
            _animPlayer.AnimationFinished += (animName) => QueueFree();
        }
    }
}
