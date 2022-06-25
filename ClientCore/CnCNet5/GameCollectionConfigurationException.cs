using System;

namespace ClientCore.CnCNet5;

/// <summary>
/// An exception that is thrown when configuration for a game to add to game collection
/// contains invalid or unexpected settings / data or required settings / data are missing.
/// </summary>
internal class GameCollectionConfigurationException : Exception
{
    public GameCollectionConfigurationException(string message)
        : base(message)
    {
    }
}