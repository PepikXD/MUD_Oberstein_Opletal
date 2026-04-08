namespace MUD_Oberstein_Opletal;

public class NPC : Character
{
    public string Id { get; set; } = string.Empty;
    
    private string _name = string.Empty;
    private string _text = string.Empty;
    public new string Name => _name;

    public string Text => _text;

    public NPC() { }

    public NPC(string name, string text)
    {
        _name = name;
        _text = text;
    }
}