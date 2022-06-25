using System;

namespace DTAClient.DXGUI;

/// <summary>
/// An exception that is thrown when initializing display / graphics mode fails.
/// </summary>
internal class GraphicsModeInitializationException : Exception
{
    public GraphicsModeInitializationException(string message)
        : base(message)
    {
    }
}