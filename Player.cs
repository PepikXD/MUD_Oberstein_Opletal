using System.Net.Sockets;

namespace MUD_Oberstein_Opletal;

public class Player : Character
{
    private string _name;
    public string Name => _name;
    private List<Item> _inventory;
    
    public Player(string name)
    {
        _name = name;
    }
}