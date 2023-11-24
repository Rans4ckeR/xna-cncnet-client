namespace DTAClient.Online
{
    /// <summary>
    /// A struct containing information on an IRC server.
    /// </summary>
    public struct Server
    {
        public Server(string host, string name, int[] ports, int[] securePorts)
        {
            Host = host;
            Name = name;
            Ports = ports;
            SecurePorts = securePorts;
        }

        public string Host;
        public string Name;
        public int[] Ports;
        public int[] SecurePorts;
    }
}