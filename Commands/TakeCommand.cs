using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal.Commands
{
    public class TakeCommand : ICommand
    {
        public async Task ExecuteAsync(Player player, string? argument, StreamWriter writer)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                await writer.WriteLineAsync("Take what?");
                return;
            }

            var item = player.CurrentRoom.Items.FirstOrDefault(i => i.name.Equals(argument, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                if (player.AddToInventory(item))
                {
                    player.CurrentRoom.Items.Remove(item);
                    await writer.WriteLineAsync($"You took the {item.name}.");
                }
                else
                {
                    await writer.WriteLineAsync("Your inventory is full.");
                }
            }
            else
            {
                await writer.WriteLineAsync("That item is not here.");
            }
        }
    }
}
