namespace MUD_Oberstein_Opletal;

public class NPC : Character
{
    private string _name;
    private string _text;
    public string Name => _name;

    public string Text => _text;

    public NPC(string name, string text)
    {
        _name = name;
        _text = text;
    }
}