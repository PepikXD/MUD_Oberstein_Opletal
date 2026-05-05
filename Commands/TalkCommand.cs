using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal.Commands
{
    public class TalkCommand : ICommand
    {
        public async Task ExecuteAsync(Player player, string? argument, StreamWriter writer)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                await writer.WriteLineAsync("Talk to whom?");
                return;
            }

            var npc = player.CurrentRoom.NPCs.FirstOrDefault(n => n.Name.StartsWith(argument, StringComparison.OrdinalIgnoreCase));
            if (npc != null)
            {
                player.ActiveDialog = new DialogSession(player, npc);
                await player.ActiveDialog.StartAsync();
            }
            else
            {
                await writer.WriteLineAsync($"No one named '{argument}' is here to talk to.");
            }
        }
    }
}
