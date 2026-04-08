using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace MUD_Oberstein_Opletal;

public class Room
{
    public string Name { get; set; }
    public string Description { get; set; }

    // Use string (direction) as key and Room as value
    public Dictionary<string, Room> Exits { get; set; } = new();
    
    public List<Item> Items { get; set; } = new();
    public List<NPC> NPCs { get; set; } = new();
    
    // Players in this specific room
    public ConcurrentDictionary<string, Player> PlayersInRoom { get; set; } = new();

    public Room(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public string GetRoomDescription(Player currentPlayer)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"--- {Name} ---");
        sb.AppendLine(Description);
        
        sb.AppendLine("\nExits:");
        if (Exits.Count > 0)
        {
            sb.AppendLine(string.Join(", ", Exits.Keys));
        }
        else
        {
            sb.AppendLine("None.");
        }

        sb.AppendLine("\nItems:");
        if (Items.Count > 0)
        {
            foreach (var item in Items)
            {
                sb.AppendLine($"- {item.name}");
            }
        }
        else
        {
            sb.AppendLine("None.");
        }

        sb.AppendLine("\nNPCs:");
        if (NPCs.Count > 0)
        {
            foreach (var npc in NPCs)
            {
                sb.AppendLine($"- {npc.Name}");
            }
        }
        else
        {
            sb.AppendLine("None.");
        }

        sb.AppendLine("\nOther Players:");
        bool otherPlayersPresent = false;
        foreach (var playerKvp in PlayersInRoom)
        {
            if (playerKvp.Key != currentPlayer.Name)
            {
                sb.AppendLine($"- {playerKvp.Key}");
                otherPlayersPresent = true;
            }
        }
        if (!otherPlayersPresent)
        {
            sb.AppendLine("None.");
        }

        return sb.ToString();
    }

    public async Task BroadcastAsync(string message, Player? excludePlayer = null)
    {
        var tasks = new List<Task>();
        foreach (var p in PlayersInRoom.Values)
        {
            if (excludePlayer != null && p.Name == excludePlayer.Name)
                continue;
                
            tasks.Add(p.SendMessageAsync(message));
        }
        await Task.WhenAll(tasks);
    }
}