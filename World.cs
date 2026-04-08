using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MUD_Oberstein_Opletal;

public class WorldData
{
    public List<RoomData> Rooms { get; set; } = new();
    public List<ItemData> Items { get; set; } = new();
    public List<NPCData> NPCs { get; set; } = new();
    public string StartingRoomId { get; set; } = string.Empty;
}

public class RoomData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Exits { get; set; } = new();
    public List<string> Items { get; set; } = new();
    public List<string> NPCs { get; set; } = new();
}

public class ItemData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}

public class NPCData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Dialog { get; set; } = string.Empty;
}

public class World
{
    public Room StartingRoom { get; private set; } = null!;
    public Dictionary<string, Room> Rooms { get; } = new();
    private readonly Dictionary<string, ItemData> _itemDefinitions = new();

    public World(string dataPath)
    {
        InitializeFromJson(dataPath);
    }

    public Item? CreateItem(string id)
    {
        if (_itemDefinitions.TryGetValue(id, out var data))
        {
            return new Item(data.Name) { Id = data.Id, Action = data.Action };
        }
        return null;
    }

    private void InitializeFromJson(string dataPath)
    {
        if (!File.Exists(dataPath))
        {
            throw new FileNotFoundException($"World data file not found at {dataPath}");
        }

        string jsonString = File.ReadAllText(dataPath);
        var worldData = JsonSerializer.Deserialize<WorldData>(jsonString);

        if (worldData == null)
            throw new InvalidOperationException("Failed to deserialize world data.");

        // 1. Create Items
        var itemsDict = new Dictionary<string, Item>();
        foreach (var itemData in worldData.Items)
        {
            _itemDefinitions[itemData.Id] = itemData;
            var item = new Item(itemData.Name) { Id = itemData.Id, Action = itemData.Action };
            itemsDict[itemData.Id] = item;
        }

        // 2. Create NPCs
        var npcsDict = new Dictionary<string, NPC>();
        foreach (var npcData in worldData.NPCs)
        {
            var npc = new NPC(npcData.Name, npcData.Dialog) { Id = npcData.Id };
            npcsDict[npcData.Id] = npc;
        }

        // 3. Create Rooms
        var rawRooms = new Dictionary<string, RoomData>();
        foreach (var roomData in worldData.Rooms)
        {
            var room = new Room(roomData.Name, roomData.Description) { Id = roomData.Id };
            
            // Link Items & NPCs
            foreach (var itemId in roomData.Items)
            {
                if (itemsDict.TryGetValue(itemId, out var mappedItem))
                    room.Items.Add(mappedItem);
            }
            foreach (var npcId in roomData.NPCs)
            {
                if (npcsDict.TryGetValue(npcId, out var mappedNpc))
                    room.NPCs.Add(mappedNpc);
            }

            Rooms[room.Id] = room;
            rawRooms[room.Id] = roomData;
        }

        // 4. Link Exits
        foreach (var roomKvp in Rooms)
        {
            var room = roomKvp.Value;
            var roomData = rawRooms[room.Id];

            foreach (var exit in roomData.Exits)
            {
                if (Rooms.TryGetValue(exit.Value, out var targetRoom))
                {
                    room.Exits[exit.Key] = targetRoom;
                }
            }
        }

        if (!Rooms.TryGetValue(worldData.StartingRoomId, out var startingRoom))
        {
            throw new InvalidOperationException("Starting room ID not found in generated rooms.");
        }
        
        StartingRoom = startingRoom;
    }
}

