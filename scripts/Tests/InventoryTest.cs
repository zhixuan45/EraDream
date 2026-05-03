using Godot;
using System;
using System.Linq;
using umaEraArchive.Game;
using UmaEraArchive.Core.Extensions;

namespace umaEraArchive.Tests;

/// <summary>
/// 物品栏系统自动化测试
/// </summary>
public partial class InventoryTest : Node
{
    public override async void _Ready()
    {
        GD.Print("\n[Test] === Starting Inventory System Tests ===\n");

        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            VerifyItemRegistration();
            VerifyAddRemoveItems();
            VerifyUseConsumable();
            VerifyDurationItem();
            VerifyPermanentItem();

            GD.Print("\n[Test] === Inventory Tests Passed! ===\n");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"\n[Test] !!! Inventory Test Failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void VerifyItemRegistration()
    {
        GD.Print("[Test] Verifying Item Registration...");
        
        var registry = BehaviorRegistry.Instance;
        // 加载我们在上一步创建的测试行为包
        registry.LoadBehaviorPack("res://test_inventory.behavior.json");

        var def = registry.GetItemDefinition("stamina_potion");
        if (def == null) throw new Exception("Failed to find 'stamina_potion' definition.");
        if (def.Name != "体力药水") throw new Exception($"Item name mismatch. Expected '体力药水', got '{def.Name}'");

        GD.Print("[Test] Item Registration OK.");
    }

    private void VerifyAddRemoveItems()
    {
        GD.Print("[Test] Verifying Add/Remove Items...");
        
        var gm = GameManager.Instance;
        var state = gm.CurrentState;
        var inv = gm.Inventory;

        state.Inventory.Items.Clear();

        inv.AddItem(state, "stamina_potion", 5);
        if (!state.Inventory.Items.ContainsKey("stamina_potion") || state.Inventory.Items["stamina_potion"] != 5)
            throw new Exception("AddItem failed or count mismatch.");

        inv.RemoveItem(state, "stamina_potion", 2);
        if (state.Inventory.Items["stamina_potion"] != 3)
            throw new Exception("RemoveItem count mismatch.");

        inv.RemoveItem(state, "stamina_potion", 3);
        if (state.Inventory.Items.ContainsKey("stamina_potion"))
            throw new Exception("Item should be removed from dictionary when count is 0.");

        GD.Print("[Test] Add/Remove Items OK.");
    }

    private void VerifyUseConsumable()
    {
        GD.Print("[Test] Verifying Consumable Item Usage...");
        
        var gm = GameManager.Instance;
        var state = gm.CurrentState;
        var inv = gm.Inventory;

        state.Inventory.Items.Clear();
        inv.AddItem(state, "stamina_potion", 1);

        bool success = inv.UseItem(state, "stamina_potion");
        if (!success) throw new Exception("Failed to use consumable item.");
        if (state.Inventory.Items.ContainsKey("stamina_potion"))
            throw new Exception("Consumable item was not removed after use.");

        GD.Print("[Test] Consumable Item Usage OK (Verify log/toast for '使用了体力药水').");
    }

    private void VerifyDurationItem()
    {
        GD.Print("[Test] Verifying Duration Item...");
        
        var gm = GameManager.Instance;
        var state = gm.CurrentState;
        var inv = gm.Inventory;

        state.Inventory.Items.Clear();
        state.Inventory.ActiveEffects.Clear();
        
        inv.AddItem(state, "training_guide", 1);
        inv.UseItem(state, "training_guide");

        if (state.Inventory.ActiveEffects.Count == 0 || state.Inventory.ActiveEffects[0].ItemId != "training_guide")
            throw new Exception("Duration item did not add active effect.");

        int initialTurns = state.Inventory.ActiveEffects[0].RemainingTurns;
        if (initialTurns != 3) throw new Exception($"Expected 3 turns, got {initialTurns}");

        // 推进一回合
        inv.UpdateTurnEffects(state);
        
        if (state.Inventory.ActiveEffects[0].RemainingTurns != 2)
            throw new Exception($"Turn count did not decrement. Expected 2, got {state.Inventory.ActiveEffects[0].RemainingTurns}");

        // 推进剩余回合
        inv.UpdateTurnEffects(state);
        inv.UpdateTurnEffects(state);

        if (state.Inventory.ActiveEffects.Any(e => e.ItemId == "training_guide"))
            throw new Exception("Duration effect should be removed after expiring.");

        GD.Print("[Test] Duration Item OK.");
    }

    private void VerifyPermanentItem()
    {
        GD.Print("[Test] Verifying Permanent Item...");
        
        var gm = GameManager.Instance;
        var state = gm.CurrentState;
        var inv = gm.Inventory;

        state.Inventory.Items.Clear();
        inv.AddItem(state, "luck_charm", 1);

        // 长期持有物品不移除，每回合触发 Tick
        GD.Print("[Test] Triggering turn for permanent item tick check...");
        inv.UpdateTurnEffects(state);

        if (state.Inventory.Items["luck_charm"] != 1)
            throw new Exception("Permanent item should not be consumed.");

        GD.Print("[Test] Permanent Item OK (Verify log/toast for '护身符在发光').");
    }
}
