using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal.Commands
{
    public class CommandHandler
    {
        private readonly Dictionary<string, ICommand> _commands = new();

        public CommandHandler()
        {
            RegisterCommand("go", new GoCommand());
            RegisterCommand("jdi", new GoCommand());
            RegisterCommand("look", new LookCommand());
            RegisterCommand("prozkoumej", new LookCommand());
            RegisterCommand("take", new TakeCommand());
            RegisterCommand("vezmi", new TakeCommand());
            RegisterCommand("drop", new DropCommand());
            RegisterCommand("poloz", new DropCommand());
            RegisterCommand("inventory", new InventoryCommand());
            RegisterCommand("inventar", new InventoryCommand());
            RegisterCommand("talk", new TalkCommand());
            RegisterCommand("mluv", new TalkCommand());
            RegisterCommand("help", new HelpCommand());
            RegisterCommand("pomoc", new HelpCommand());
            RegisterCommand("use", new UseCommand());
            RegisterCommand("pouzij", new UseCommand());
            RegisterCommand("say", new SayCommand());
            RegisterCommand("rekni", new SayCommand());
            RegisterCommand("shout", new ShoutCommand());
            RegisterCommand("krik", new ShoutCommand());
            RegisterCommand("buy", new BuyCommand());
            RegisterCommand("nakup", new BuyCommand());
            RegisterCommand("sell", new SellCommand());
            RegisterCommand("prodej", new SellCommand());
        }

        private void RegisterCommand(string name, ICommand command)
        {
            _commands[name.ToLower()] = command;
        }

        public async Task ExecuteCommandAsync(string commandText, Player player, StreamWriter writer)
        {
            string[] parts = commandText.ToLower().Split(' ', 2);
            string commandName = parts[0];
            string? argument = parts.Length > 1 ? parts[1] : null;

            if (_commands.TryGetValue(commandName, out ICommand? command))
            {
                await command.ExecuteAsync(player, argument, writer);
            }
            else
            {
                await writer.WriteLineAsync("Unknown command.");
            }
        }
    }
}
