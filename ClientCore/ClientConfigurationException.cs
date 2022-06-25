using System;

namespace ClientCore;

/// <summary>
/// An exception that is thrown when a client configuration file contains invalid or
/// unexpected settings / data or required settings / data are missing.
/// </summary>
public class ClientConfigurationException : Exception
{
    public ClientConfigurationException(string message)
        : base(message)
    {
    }
}