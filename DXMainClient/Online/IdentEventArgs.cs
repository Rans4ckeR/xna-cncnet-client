using System;

namespace DTAClient.Online;

public class IdentEventArgs : EventArgs
{
    public IdentEventArgs(string ident)
    {
        Ident = ident;
    }

    public string Ident { get; private set; }
}