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

            var npc = player.CurrentRoom.NPCs.FirstOrDefault(n => n.Name.Equals(argument, StringComparison.OrdinalIgnoreCase));
            if (npc != null)
            {
                await writer.WriteLineAsync($"{npc.Name} says: \"{npc.Text}\"");
            }
            else
            {
                await writer.WriteLineAsync("That person is not here.");
            }
        }
    }
}
