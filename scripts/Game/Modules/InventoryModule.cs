using Godot;
using System;
using System.Linq;
using EraDream.Core.Extensions;
using EraDream.Game.Models;

namespace EraDream.Game;

/// <summary>
/// 负责处理训练员背包逻辑，包括物品使用、添加、移除及持续效果更新。
/// </summary>
public partial class InventoryModule : Node
{
    /// <summary>
    /// 向背包添加物品，返回实际添加的物品数量
    /// </summary>
    public int AddItem(GameState state, string itemId, int count = 1)
    {
        var def = BehaviorRegistry.Instance.GetItemDefinition(itemId);
        if (def == null) return 0;

        int current = state.Inventory.Items.ContainsKey(itemId) ? state.Inventory.Items[itemId] : 0;
        int target = Math.Min(current + count, def.MaxStack);
        int added = target - current;

        if (added > 0)
        {
            state.Inventory.Items[itemId] = target;
        }
        return added;
    }

    /// <summary>
    /// 从背包移除物品
    /// </summary>
    public bool RemoveItem(GameState state, string itemId, int count = 1)
    {
        if (!state.Inventory.Items.ContainsKey(itemId) || state.Inventory.Items[itemId] < count)
        {
            return false;
        }

        state.Inventory.Items[itemId] -= count;
        if (state.Inventory.Items[itemId] <= 0)
        {
            state.Inventory.Items.Remove(itemId);
        }
        return true;
    }

    /// <summary>
    /// 使用物品
    /// </summary>
    public bool UseItem(GameState state, string itemId)
    {
        var def = BehaviorRegistry.Instance.GetItemDefinition(itemId);
        if (def == null) return false;

        // 检查持有量
        if (!state.Inventory.Items.ContainsKey(itemId) || state.Inventory.Items[itemId] <= 0)
        {
            return false;
        }

        // 处理不同类型的物品逻辑
        switch (def.Type)
        {
            case ItemType.Consumable:
                // 消耗一个物品
                RemoveItem(state, itemId, 1);
                // 触发一次性 Hook: OnItemUsed_ID
                BehaviorRegistry.Instance.TriggerHook($"OnItemUsed_{itemId}", state);
                break;

            case ItemType.Duration:
                // 消耗物品并添加持续效果
                RemoveItem(state, itemId, 1);
                AddActiveEffect(state, itemId, def.DurationTurns);
                // 触发初次使用 Hook
                BehaviorRegistry.Instance.TriggerHook($"OnItemUsed_{itemId}", state);
                break;

            case ItemType.Permanent:
                // 长期持有物品通常不需要主动“使用”，或者点击仅用于触发剧情/信息
                BehaviorRegistry.Instance.TriggerHook($"OnItemUsed_{itemId}", state);
                break;
        }

        return true;
    }

    /// <summary>
    /// 添加持续性效果，防止 turns 为负数
    /// </summary>
    private void AddActiveEffect(GameState state, string itemId, int turns)
    {
        if (turns <= 0) return;
        // 如果已经存在同类效果，则重置回合数（取大值，不叠加）
        var existing = state.Inventory.ActiveEffects.FirstOrDefault(e => e.ItemId == itemId);
        if (existing != null)
        {
            existing.RemainingTurns = Math.Max(existing.RemainingTurns, turns);
        }
        else
        {
            state.Inventory.ActiveEffects.Add(new ActiveEffect { ItemId = itemId, RemainingTurns = turns });
        }
    }

    /// <summary>
    /// 每回合更新：扣减持续效果回合，并触发 Tick Hook
    /// </summary>
    public void UpdateTurnEffects(GameState state)
    {
        // 1. 处理持续性物品 (Duration)
        for (int i = state.Inventory.ActiveEffects.Count - 1; i >= 0; i--)
        {
            var effect = state.Inventory.ActiveEffects[i];
            
            // 触发每回合 Hook: OnItemTick_ID
            BehaviorRegistry.Instance.TriggerHook($"OnItemTick_{effect.ItemId}", state);

            effect.RemainingTurns--;
            if (effect.RemainingTurns <= 0)
            {
                state.Inventory.ActiveEffects.RemoveAt(i);
                // 触发失效 Hook: OnItemExpired_ID
                BehaviorRegistry.Instance.TriggerHook($"OnItemExpired_{effect.ItemId}", state);
            }
        }

        // 2. 处理长期持有物品 (Permanent)
        // 只要在背包里就触发 Tick，通过 ToList 避免 Hook 修改背包导致的集合遍历异常
        var itemIds = state.Inventory.Items.Keys.ToList();
        foreach (var itemId in itemIds)
        {
            var def = BehaviorRegistry.Instance.GetItemDefinition(itemId);
            if (def?.Type == ItemType.Permanent)
            {
                BehaviorRegistry.Instance.TriggerHook($"OnItemTick_{itemId}", state);
            }
        }
    }
}
