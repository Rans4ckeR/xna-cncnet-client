using System;

namespace DTAConfig;

/// <summary>
/// An exception that is thrown when configuration for DirectDraw wrapper contains
/// invalid or unexpected settings / data or required settings / data are missing.
/// </summary>
internal class DirectDrawWrapperConfigurationException : Exception
{
    public DirectDrawWrapperConfigurationException(string message)
        : base(message)
    {
    }
}