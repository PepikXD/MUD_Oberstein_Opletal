using System.Collections.Generic;

namespace MUD_Oberstein_Opletal;

public class World
{
    public Room StartingRoom { get; private set; }

    public World()
    {
        // Create Rooms
        var entrance = new Room("Entrance Hall", "A grand hall with a dusty chandelier. The main door is sealed shut.");
        var library = new Room("Library", "Rows of ancient books line the walls. A faint smell of vanilla hangs in the air.");
        var garden = new Room("Garden", "A once beautiful garden, now overgrown with thorny vines.");

        // Create NPCs
        var oldMan = new NPC("Old Man", "Greetings, traveler. This castle has been empty for a long time... or has it?");
        var ghost = new NPC("Ghost", "Woooo... who dares disturb my slumber?");

        // Create Items
        var key = new Item("key");
        var book = new Item("book");
        var flower = new Item("flower");

        // Place NPCs and Items in Rooms
        entrance.NPCs.Add(oldMan);
        entrance.Items.Add(key);
        library.Items.Add(book);
        garden.NPCs.Add(ghost);
        garden.Items.Add(flower);

        // Link Rooms with Exits
        entrance.Exits["north"] = library;
        library.Exits["south"] = entrance;
        entrance.Exits["east"] = garden;
        garden.Exits["west"] = entrance;

        // Set the starting room
        StartingRoom = entrance;
    }
}
