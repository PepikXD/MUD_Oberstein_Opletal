using System.Collections.Generic;
using System.Text;

namespace MUD_Oberstein_Opletal;

public class Player : Character
{
    public string Name { get; }
    public Room CurrentRoom { get; set; }

    private readonly List<Item> _inventory = new();
    public int MaxInventoryCapacity { get; } = 10;

    public Player(string name, Room startingRoom)
    {
        Name = name;
        CurrentRoom = startingRoom;
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
