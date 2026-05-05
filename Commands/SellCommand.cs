using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal.Commands;

public class SellCommand : ICommand
{
    public async Task ExecuteAsync(Player player, string? argument, StreamWriter writer)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            await writer.WriteLineAsync("Sell what?");
            return;
        }

        var merchant = player.CurrentRoom.NPCs.FirstOrDefault(n => n.IsMerchant);
        if (merchant == null)
        {
            await writer.WriteLineAsync("There is no merchant here.");
            return;
        }

        var item = player.Inventory.FirstOrDefault(i => argument.StartsWith(i.name, StringComparison.OrdinalIgnoreCase));
        if (item == null)
        {
            await writer.WriteLineAsync("You don't have that item.");
            return;
        }

        // For simplicity, merchants buy items at half price
        int sellPrice = item.Price / 2;
        if (sellPrice <= 0) sellPrice = 1;

        player.RemoveFromInventory(item);
        player.Currency += sellPrice;

        await writer.WriteLineAsync($"You sold {item.name} to {merchant.Name} for {sellPrice} coins.");
    }
}
