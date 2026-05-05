using System.IO;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal.Commands;

public class SayCommand : ICommand
{
    public async Task ExecuteAsync(Player player, string? argument, StreamWriter writer)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            await writer.WriteLineAsync("Say what?");
            return;
        }

        await player.CurrentRoom.BroadcastAsync($"[{player.Name} říká]: {argument}", player);
        await writer.WriteLineAsync($"You say: \"{argument}\"");
    }
}
