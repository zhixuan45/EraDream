using Godot;
using System.Collections.Generic;
using EraDream.Core.Extensions;

namespace EraDream.Game;

/// <summary>
/// 处理商店购买逻辑
/// </summary>
public partial class ShopModule : Node
{
    /// <summary>
    /// 购买道具
    /// </summary>
    public bool BuyItem(GameState state, string itemId)
    {
        var def = BehaviorRegistry.Instance.GetItemDefinition(itemId);
        if (def == null) return false;

        int price = def.Price;
        if (state.Player.Money >= price)
        {
            state.Player.ConsumeMoney(price);
            GameManager.Instance.Inventory.AddItem(state, itemId, 1);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 使用道具 (重定向到 InventoryModule)
    /// </summary>
    public bool UseItem(GameState state, string itemId)
    {
        return GameManager.Instance.Inventory.UseItem(state, itemId);
    }
}
