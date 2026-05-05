using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal.Commands;

public class BuyCommand : ICommand
{
    public async Task ExecuteAsync(Player player, string? argument, StreamWriter writer)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            await writer.WriteLineAsync("Buy what?");
            return;
        }

        var merchant = player.CurrentRoom.NPCs.FirstOrDefault(n => n.IsMerchant);
        if (merchant == null)
        {
            await writer.WriteLineAsync("There is no merchant here.");
            return;
        }

        // Check if merchant sells this item. We match by prefix or full name using world definition, 
        // since we only store IDs in ItemsForSale. We need to create it to check its name.
        Item? itemToBuy = null;
        foreach (var id in merchant.ItemsForSale)
        {
            var item = player.Server.World.CreateItem(id);
            if (item != null && argument.StartsWith(item.name, StringComparison.OrdinalIgnoreCase))
            {
                itemToBuy = item;
                break;
            }
        }

        if (itemToBuy == null)
        {
            await writer.WriteLineAsync("The merchant doesn't sell that.");
            return;
        }

        if (player.Currency < itemToBuy.Price)
        {
            await writer.WriteLineAsync($"You don't have enough money ({itemToBuy.Price}) to buy {itemToBuy.name}.");
            return;
        }

        if (player.AddToInventory(itemToBuy))
        {
            player.Currency -= itemToBuy.Price;
            await writer.WriteLineAsync($"You bought {itemToBuy.name} from {merchant.Name} for {itemToBuy.Price} coins.");
        }
        else
        {
            await writer.WriteLineAsync("Your inventory is full.");
        }
    }
}
