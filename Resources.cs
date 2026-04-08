namespace MUD_Oberstein_Opletal;

/// <summary>
/// Statická třída sloužící jako vnitřní Resource pro uložení konstantních stringů a lokalizovaných výpisů (A1).
/// </summary>
public static class Resources
{
    public const string WelcomeMessage = "Welcome to the game, {0}!";
    public const string AskForName = "Enter your name:";
    public const string AskForPassword = "Enter your password:";
    public const string NameAlreadyOnline = "Player with that name is already online.";
    public const string InvalidName = "Invalid name. Please try again.";
    public const string UnknownCommand = "Unknown command.";
    public const string GoWhere = "Go where?";
    public const string CannotGoThatWay = "You can't go that way.";
    public const string LoggedInMessage = "Player '{0}' logged in.";
    public const string LoggedOutMessage = "Client '{0}' disconnected.";
    public const string PlayerArrivedRoom = "[!] {0} přichází.";
    public const string PlayerLeftRoom = "[!] {0} odchází směr {1}.";
    public const string PlayerJoinedServer = "[!] {0} se připojil(a) do hry.";
    public const string PlayerLeftServer = "[!] {0} se odpojil(a) ze hry.";
}
