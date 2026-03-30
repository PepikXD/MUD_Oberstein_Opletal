using System.IO;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal.Commands
{
    public class InventoryCommand : ICommand
    {
        public async Task ExecuteAsync(Player player, string? argument, StreamWriter writer)
        {
            await writer.WriteLineAsync(player.GetInventoryDescription());
        }
    }
}
