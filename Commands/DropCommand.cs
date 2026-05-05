using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal.Commands
{
    public class DropCommand : ICommand
    {
        public async Task ExecuteAsync(Player player, string? argument, StreamWriter writer)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                await writer.WriteLineAsync("Drop what?");
                return;
            }

            var item = player.GetInventory().FirstOrDefault(i => i.name.StartsWith(argument, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                player.RemoveFromInventory(item);
                player.CurrentRoom.Items.Add(item);
                await writer.WriteLineAsync($"You dropped the {item.name}.");
            }
            else
            {
                await writer.WriteLineAsync("You don't have that item.");
            }
        }
    }
}
