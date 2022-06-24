namespace DTAClient.Online;

/// <summary>
/// A struct containing information on an IRC server.
/// </summary>
public struct Server
{
    public string Host;

    public Server(string host, string name, int[] ports)
    {
        Host = host;
        Name = name;
        Ports = ports;
    }

    public string Name;
    public int[] Ports;
}