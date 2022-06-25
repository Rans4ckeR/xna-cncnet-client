using System;

namespace DTAClient.Online;

public class IndexEventArgs : EventArgs
{
    public IndexEventArgs(int index)
    {
        Index = index;
    }

    public int Index { get; private set; }
}