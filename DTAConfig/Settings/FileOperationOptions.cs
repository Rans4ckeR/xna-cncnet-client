namespace DTAConfig.Settings;

/// <summary>
/// Defines the expected behavior of file operations performed with
/// <see cref="FileSourceDestinationInfo"/>.
/// </summary>
public enum FileOperationOptions
{
    AlwaysOverwrite,
    OverwriteOnMismatch,
    DontOverwrite,
    KeepChanges
}