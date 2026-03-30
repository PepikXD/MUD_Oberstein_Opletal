using System.IO;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal.Commands
{
    public interface ICommand
    {
        Task ExecuteAsync(Player player, string? argument, StreamWriter writer);
    }
}
