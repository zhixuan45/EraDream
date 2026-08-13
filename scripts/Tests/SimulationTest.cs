using Godot;
using System;
using System.Collections.Generic;
using EraDream.Game;

namespace EraDream.Tests;

/// <summary>
/// 养成系统自动化测试用例
/// 验证 GameManager 初始化、数值默认值、回合推进以及存档读档功能。
/// </summary>
public partial class SimulationTest : Node
{
    public override async void _Ready()
    {
        GD.Print("\n[Test] === Starting Simulation System Tests ===\n");

        try 
        {
            // 等待一帧确保 Autoload 已就绪
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            // 测试必须从全新状态开始，避免自动存档或同场景其他测试污染默认值断言。
            GameManager.Instance.StartNewGame(new List<string>());

            VerifyInitialization();
            VerifyDefaultStats();
            VerifyAdvanceTurn();
            VerifyTrainingAndSaveLoad();
            
            // 新增重构模块验证
            VerifyWorkModule();
            VerifyOutingModule();
            VerifyShopModule();
            VerifyBehaviorRegistry();

            GD.Print("\n[Test] === All Tests Passed Successfully! ===\n");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"\n[Test] !!! Test Failed: {ex.Message}\n{ex.StackTrace}");
        }
        
        // 测试完成后退出，方便自动化流程
        // GetTree().Quit(); 
    }

    /// <summary>
    /// 验证 GameManager 是否正常初始化
    /// </summary>
    private void VerifyInitialization()
    {
        GD.Print("[Test] Verifying GameManager Initialization...");
        
        if (GameManager.Instance == null)
            throw new Exception("GameManager Instance is null.");
            
        if (GameManager.Instance.CurrentState == null)
            throw new Exception("GameState is null after initialization.");

        if (GameManager.Instance.Training == null)
            throw new Exception("TrainingModule is not initialized.");

        GD.Print("[Test] Initialization OK.");
    }

    /// <summary>
    /// 检查初始数值是否符合 GameState/PlayerStats 默认设置
    /// </summary>
    private void VerifyDefaultStats()
    {
        GD.Print("[Test] Verifying Default Stats...");
        
        var state = GameManager.Instance.CurrentState;
        
        // 验证玩家初始数值
        if (state.Player.Money != 0)
            throw new Exception($"Expected Money 0, but got {state.Player.Money}");
            
        if (state.Player.Stamina != 100)
            throw new Exception($"Expected Stamina 100, but got {state.Player.Stamina}");

        if (state.Player.Energy != 100)
            throw new Exception($"Expected Energy 100, but got {state.Player.Energy}");

        // 验证回合数
        if (state.CurrentTurn != 1)
            throw new Exception($"Expected CurrentTurn 1, but got {state.CurrentTurn}");

        GD.Print("[Test] Default Stats OK.");
    }

    /// <summary>
    /// 模拟回合推进并验证
    /// </summary>
    private void VerifyAdvanceTurn()
    {
        GD.Print("[Test] Verifying AdvanceTurn...");
        
        int initialTurn = GameManager.Instance.CurrentState.CurrentTurn;
        GameManager.Instance.AdvanceTurn();
        
        if (GameManager.Instance.CurrentState.CurrentTurn != initialTurn + 1)
            throw new Exception($"Turn count did not increment. Expected {initialTurn + 1}, got {GameManager.Instance.CurrentState.CurrentTurn}");

        // 检查自动存档文件是否存在
        if (!FileAccess.FileExists(GameManager.AutoSavePath))
            throw new Exception("AutoSave file was not created after AdvanceTurn.");

        GD.Print("[Test] AdvanceTurn OK.");
    }

    /// <summary>
    /// 验证训练逻辑以及存档读档的持久性
    /// </summary>
    private void VerifyTrainingAndSaveLoad()
    {
        GD.Print("[Test] Verifying Training and Save/Load...");
        
        string testSavePath = "user://test_save.sav";
        var state = GameManager.Instance.CurrentState;

        // 1. 手动修改属性并验证 (模拟训练或事件)
        state.Player.Money = 500;
        state.Uma.Speed = 50;
        state.Uma.SkillPoints = 10;
        
        // 2. 执行存档
        GameManager.Instance.SaveGame(testSavePath);
        if (!FileAccess.FileExists(testSavePath))
            throw new Exception("Test save file was not created.");

        // 3. 修改内存中的值
        state.Player.Money = 9999;
        state.Uma.Speed = 999;

        // 4. 执行读档
        GameManager.Instance.LoadGame(testSavePath);
        
        // 5. 验证恢复的值
        var newState = GameManager.Instance.CurrentState;
        if (newState.Player.Money != 500)
            throw new Exception($"Save/Load mismatch for Money. Expected 500, got {newState.Player.Money}");
            
        if (newState.Uma.Speed != 50)
            throw new Exception($"Save/Load mismatch for Speed. Expected 50, got {newState.Uma.Speed}");

        if (newState.Uma.SkillPoints != 10)
            throw new Exception($"Save/Load mismatch for SkillPoints. Expected 10, got {newState.Uma.SkillPoints}");

        GD.Print("[Test] Training and Save/Load OK.");
    }

    private void VerifyWorkModule()
    {
        GD.Print("[Test] Verifying WorkModule...");
        var state = GameManager.Instance.CurrentState;
        state.Player.Stamina = 100;
        state.Player.Energy = 100;
        int initialMoney = state.Player.Money;

        var work = GameManager.Instance.Work;
        if (work == null) throw new Exception("WorkModule is null in GameManager.");
        
        bool success = work.ExecuteWork(state);

        if (!success) throw new Exception("Work execution failed with full resources.");
        if (state.Player.Money <= initialMoney) throw new Exception("Money did not increase after work.");
        if (state.Player.Stamina >= 100) throw new Exception("Stamina was not consumed by work.");

        GD.Print("[Test] WorkModule OK.");
    }

    private void VerifyOutingModule()
    {
        GD.Print("[Test] Verifying OutingModule...");
        var state = GameManager.Instance.CurrentState;
        state.Uma.Mood = 50;
        state.Player.Stamina = 100;
        state.Player.Energy = 100;

        var outing = GameManager.Instance.Outing;
        if (outing == null) throw new Exception("OutingModule is null in GameManager.");

        bool success = outing.ExecuteOuting(state);

        if (!success) throw new Exception("Outing execution failed.");
        if (state.Uma.Mood <= 50) throw new Exception("Mood did not increase after outing.");

        GD.Print("[Test] OutingModule OK.");
    }

    private void VerifyShopModule()
    {
        GD.Print("[Test] Verifying ShopModule...");
        var state = GameManager.Instance.CurrentState;
        state.Player.Money = 1000;
        state.Inventory.Items.Clear();

        // 确保物品已注册
        var registry = EraDream.Core.Extensions.BehaviorRegistry.Instance;
        registry.LoadBehaviorPack("res://test_inventory.behavior.json");

        var shop = GameManager.Instance.Shop;
        if (shop == null) throw new Exception("ShopModule is null in GameManager.");

        // 使用测试行为包中的 stamina_potion
        bool success = shop.BuyItem(state, "stamina_potion");

        if (!success) throw new Exception("Failed to buy stamina_potion with enough money.");
        if (!state.Inventory.Items.ContainsKey("stamina_potion")) throw new Exception("Inventory does not contain purchased item.");
        if (state.Player.Money != 500) throw new Exception($"Incorrect money after purchase. Expected 500, got {state.Player.Money}");

        // Use item
        bool useSuccess = shop.UseItem(state, "stamina_potion");
        if (!useSuccess) throw new Exception("Failed to use item from inventory.");
        if (state.Inventory.Items.ContainsKey("stamina_potion")) throw new Exception("Item count should be 0/removed after using consumable.");

        GD.Print("[Test] ShopModule OK.");
    }

    private void VerifyBehaviorRegistry()
    {
        GD.Print("[Test] Verifying BehaviorRegistry Hook System...");
        var state = GameManager.Instance.CurrentState;
        
        // 模拟加载一个测试行为包
        string testBehaviorPath = "user://test_behavior.json";
        string behaviorJson = @"
        {
            ""rules"": [
                {
                    ""id"": ""test_hook_01"",
                    ""hook"": ""OnTestHook"",
                    ""probability"": 1.0,
                    ""conditions"": [],
                    ""action"": {
                        ""type"": ""BriefStory"",
                        ""path"": ""Test Success!""
                    }
                }
            ]
        }";
        
        using (var file = FileAccess.Open(testBehaviorPath, FileAccess.ModeFlags.Write))
        {
            file.StoreString(behaviorJson);
        }

        var registry = EraDream.Core.Extensions.BehaviorRegistry.Instance;
        registry.LoadBehaviorPack(testBehaviorPath);
        
        // 触发 Hook 并验证 (通过观察日志或状态变更，目前 BriefStory 只是显示 Toast)
        GD.Print("[Test] Triggering OnTestHook...");
        registry.TriggerHook("OnTestHook", state);

        GD.Print("[Test] BehaviorRegistry OK (Visual check of logs/toast required for full verification).");
    }
}
