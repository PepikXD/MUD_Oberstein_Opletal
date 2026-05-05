using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal.Commands
{
    public class HelpCommand : ICommand
    {
        public async Task ExecuteAsync(Player player, string? argument, StreamWriter writer)
        {
            var sb = new StringBuilder();
            sb.AppendLine("--- Help ---");
            sb.AppendLine("Available commands:");
            sb.AppendLine("  go <direction> / jdi <směr>    - Move to a different room (e.g., 'go north').");
            sb.AppendLine("  look / prozkoumej              - See the description of the current room.");
            sb.AppendLine("  take <item> / vezmi <předmět>  - Pick up an item from the room.");
            sb.AppendLine("  drop <item> / poloz <předmět>  - Drop an item from your inventory.");
            sb.AppendLine("  inventory / inventar           - Check your inventory.");
            sb.AppendLine("  talk <npc> / mluv <npc>        - Talk to a character in the room.");
            sb.AppendLine("  use <item> / pouzij <předmět>  - Use an item from your inventory.");
            sb.AppendLine("  help / pomoc                   - Display this help message.");
            await writer.WriteLineAsync(sb.ToString());
        }
    }
}
