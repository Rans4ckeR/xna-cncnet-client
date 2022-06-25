using System;

namespace DTAClient.Online;

internal struct FileHashes
{
    public string GameOptionsHash { get; set; }

    public string ClientDXHash { get; set; }

    public string ClientXNAHash { get; set; }

    public string ClientOGLHash { get; set; }

    public string INIHashes { get; set; }

    public string MPMapsHash { get; set; }

    public string GameExeHash { get; set; }

    public string LauncherExeHash { get; set; }

    public string FHCConfigHash { get; set; }

    public override string ToString()
    {
        return "GameOptions Hash: " + GameOptionsHash + Environment.NewLine +
            "ClientDXHash: " + ClientDXHash + Environment.NewLine +
            "ClientXNAHash: " + ClientXNAHash + Environment.NewLine +
            "ClientOGLHash: " + ClientOGLHash + Environment.NewLine +
            "INI Hashes: " + INIHashes + Environment.NewLine +
            "MPMaps Hash: " + MPMapsHash + Environment.NewLine +
            "MainExe Hash: " + GameExeHash + Environment.NewLine +
            "LauncherExe Hash: " + LauncherExeHash + Environment.NewLine +
            "FHCConfig Hash: " + FHCConfigHash;
    }
}