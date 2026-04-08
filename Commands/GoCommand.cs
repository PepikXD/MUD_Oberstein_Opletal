using System.IO;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal.Commands
{
    public class GoCommand : ICommand
    {
        public async Task ExecuteAsync(Player player, string? argument, StreamWriter writer)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                await writer.WriteLineAsync("Go where?");
                return;
            }

            if (player.CurrentRoom.Exits.TryGetValue(argument, out Room? nextRoom))
            {
                await player.CurrentRoom.BroadcastAsync($"[!] {player.Name} odchází směr {argument}.", player);
                player.CurrentRoom.PlayersInRoom.TryRemove(player.Name, out _);
                
                player.CurrentRoom = nextRoom;
                
                player.CurrentRoom.PlayersInRoom[player.Name] = player;
                await player.CurrentRoom.BroadcastAsync($"[!] {player.Name} přichází.", player);

                await writer.WriteLineAsync($"You go {argument}.");
                await writer.WriteLineAsync(player.CurrentRoom.GetRoomDescription(player));
            }
            else
            {
                await writer.WriteLineAsync("You can't go that way.");
            }
        }
    }
}
