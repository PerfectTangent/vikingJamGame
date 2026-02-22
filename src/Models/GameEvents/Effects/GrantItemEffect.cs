using VikingJamGame.Models.Items;

namespace VikingJamGame.Models.GameEvents.Effects;

/// <summary>
/// Grants an item to the player's inventory by looking it up from the repository.
/// Does nothing if the inventory is full.
/// should make it give the player the choice to discard another item at some point.
/// </summary>
public sealed record GrantItemEffect(string ItemId) : IGameEventEffect
{
    public bool IsPositive => true;

    public void Apply(GameEventContext context)
    {
        Item item = context.ItemRepository.GetById(ItemId);
        context.PlayerInfo.Inventory.AddItem(item, context);
    }

    public string GetDisplayText(GameEventContext context)
    {
        string name = context.ItemRepository.TryGetById(ItemId, out Item item)
            ? item.Name
            : ItemId;
        return $"You got {name}";
    }
}
