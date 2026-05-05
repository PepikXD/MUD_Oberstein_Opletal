using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal;

public class Player : Character
{
    public new string Name { get; }
    public Room CurrentRoom { get; set; }
    public int Currency { get; set; } = 100;
    
    // Uložíme si writer, abychom mohli hráči kdykoliv poslat asynchronní zprávu.
    public StreamWriter Writer { get; }
    
    // Odkaz na samotný server (pro broadcastování atd.)
    public Server Server { get; }

    private readonly List<Item> _inventory = new();
    public List<Item> Inventory => _inventory;
    public int MaxInventoryCapacity { get; } = 10;
    
    public Dictionary<string, QuestState> Quests { get; set; } = new();
    public DialogSession? ActiveDialog { get; set; }

    public QuestState GetQuestState(string questId)
    {
        if (Quests.TryGetValue(questId, out var state)) return state;
        return QuestState.NotStarted;
    }

    public void SetQuestState(string questId, QuestState state)
    {
        Quests[questId] = state;
    }

    public Player(string name, Room startingRoom, StreamWriter writer, Server server)
    {
        Name = name;
        CurrentRoom = startingRoom;
        Writer = writer;
        Server = server;
    }

    public async Task SendMessageAsync(string message)
    {
        try
        {
            if (Writer.BaseStream.CanWrite)
            {
                await Writer.WriteLineAsync(message);
            }
        }
        catch 
        {
            // Pokud spojení už neexistuje, chybu tiše spolkneme.
        }
    }

    public bool AddToInventory(Item item)
    {
        if (_inventory.Count >= MaxInventoryCapacity)
        {
            return false; // Inventory is full
        }
        _inventory.Add(item);
        return true;
    }

    public void RemoveFromInventory(Item item)
    {
        _inventory.Remove(item);
    }
    
    public List<Item> GetInventory()
    {
        return _inventory;
    }

    public string GetInventoryDescription()
    {
        var sb = new StringBuilder();
        sb.AppendLine("--- Inventory ---");
        sb.AppendLine($"Capacity: {_inventory.Count}/{MaxInventoryCapacity}");
        
        if (_inventory.Count > 0)
        {
            foreach (var item in _inventory)
            {
                sb.AppendLine($"- {item.name}");
            }
        }
        else
        {
            sb.AppendLine("Your inventory is empty.");
        }
        return sb.ToString();
    }
}
