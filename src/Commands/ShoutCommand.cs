using System.IO;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal.Commands;

public class ShoutCommand : ICommand
{
    public async Task ExecuteAsync(Player player, string? argument, StreamWriter writer)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            await writer.WriteLineAsync("Shout what?");
            return;
        }

        foreach (var p in player.Server.ClientPlayer.Values)
        {
            if (p != player)
            {
                await p.Writer.WriteLineAsync($"[{player.Name} KŘIČÍ na celý svět]: {argument}");
            }
        }
        await writer.WriteLineAsync($"You shout: \"{argument}\"");
    }
}
