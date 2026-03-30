namespace MUD_Oberstein_Opletal;

public class Item
{
    public string name; // Keeping lowercase as it was, but assigning it via constructor

    public Item(string itemName)
    {
        name = itemName;
    }
}