using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal.Commands;

public class UseCommand : ICommand
{
    public string Name => "use";
    public string Description => "Uses an item from your inventory.";

    public async Task ExecuteAsync(Player player, string? argument, StreamWriter writer)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            await writer.WriteLineAsync("Use what?");
            return;
        }

        string itemName = argument.ToLower();
        var item = player.Inventory.FirstOrDefault(i => i.name.StartsWith(itemName, System.StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            await writer.WriteLineAsync($"You don't have '{itemName}' in your inventory.");
            return;
        }

        if (item.Action == "win_game")
        {
            await player.CurrentRoom.BroadcastAsync($"[!] {player.Name} used {item.name} and unlocked the secret of the castle! {player.Name} has won the game!");
            await writer.WriteLineAsync("Congratulations! You have completed your adventure!");
            
            // P1: Výsledek dokončení se uloží do statistik
            string statsPath = "Data/statistics.txt";
            Directory.CreateDirectory("Data");
            File.AppendAllText(statsPath, $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] Player {player.Name} has won the game!" + System.Environment.NewLine);

            // To properly exit the client from the server, we can drop connection gracefully 
            // by closing the stream, or letting the main loop know.
            writer.BaseStream.Close(); // This kicks the player and ends their HandleClientAsync loop
            return;
        }

        await writer.WriteLineAsync($"You used {item.name}, but nothing happened.");
    }
}
