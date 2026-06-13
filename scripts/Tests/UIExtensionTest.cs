using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EraDream.Core.Extensions;
using EraDream.Game;

namespace EraDream.Tests
{
    public partial class UIExtensionTest : Node
    {
        public override void _Ready()
        {
            GD.Print("[UIExtensionTest] Starting...");
            try
            {
                TestUIInjection();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[UIExtensionTest] Error: {ex.Message}");
            }
            GD.Print("[UIExtensionTest] Finished.");
        }

        private void TestUIInjection()
        {
            string testPath = "user://test_ui_ext.behavior.json";
            var pack = new BehaviorPack
            {
                Menus = new List<UIMenuDefinition>
                {
                    new UIMenuDefinition
                    {
                        MenuId = "Training",
                        Options = new List<UIOption>
                        {
                            new UIOption
                            {
                                Id = "special_training",
                                Name = "特选训练",
                                Description = "来自扩展的特殊训练",
                                Action = new BehaviorAction { Type = "BriefStory", Path = "special_train_story" }
                            }
                        }
                    }
                }
            };

            File.WriteAllText(ProjectSettings.GlobalizePath(testPath), JsonSerializer.Serialize(pack));
            
            // Ensure Instance exists for test
            if (BehaviorRegistry.Instance == null)
            {
                GD.PrintErr("[UIExtensionTest] BehaviorRegistry.Instance is null!");
                return;
            }

            BehaviorRegistry.Instance.LoadBehaviorPack(testPath);

            var state = new GameState();
            var options = BehaviorRegistry.Instance.GetValidOptions("Training", state);
            
            if (options.Find(o => o.Id == "special_training") != null)
            {
                GD.Print("[UIExtensionTest] UI Injection success!");
            }
            else
            {
                GD.PrintErr("[UIExtensionTest] UI Injection FAILED!");
            }
        }
    }
}
