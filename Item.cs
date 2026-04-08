namespace MUD_Oberstein_Opletal;

public class Item
{
    public string Id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;

    public Item() { }

    public Item(string itemName)
    {
        name = itemName;
    }
}