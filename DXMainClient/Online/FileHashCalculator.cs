using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using ClientCore.I18N;
using Rampastring.Tools;
using Utilities = Rampastring.Tools.Utilities;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DTAClient.Domain.Multiplayer.CnCNet;
using System.Reflection;

namespace DTAClient.Online
{
    public class FileHashCalculator
    {
        private FileHashes fh;
        private const string CONFIGNAME = "FHCConfig.ini";
        private bool calculateGameExeHash = true;

        string[] fileNamesToCheck = new string[]
        {
#if ARES
            "Ares.dll",
            "Ares.dll.inj",
            "Ares.mix",
            "Syringe.exe",
            "cncnet5.dll",
            "rulesmd.ini",
            "artmd.ini",
            "soundmd.ini",
            "aimd.ini",
            "shroud.shp",
#elif YR
            "spawner.xdp",
            "spawner2.xdp",
            "artmd.ini",
            "soundmd.ini",
            "aimd.ini",
            "shroud.shp",
            "INI/Map Code/Cooperative.ini",
            "INI/Map Code/Free For All.ini",
            "INI/Map Code/Land Rush.ini",
            "INI/Map Code/Meat Grinder.ini",
            "INI/Map Code/Megawealth.ini",
            "INI/Map Code/Naval War.ini",
            "INI/Map Code/Standard.ini",
            "INI/Map Code/Team Alliance.ini",
            "INI/Map Code/Unholy Alliance.ini",
            "INI/Game Options/Allies Allowed.ini",
            "INI/Game Options/Brutal AI.ini",
            "INI/Game Options/No Dog Engi Eat.ini",
            "INI/Game Options/No Spawn Previews.ini",
            "INI/Game Options/RA2 Classic Mode.ini",
            "INI/Map Code/GlobalCode.ini",
            "INI/Map Code/MultiplayerGlobalCode.ini",
#elif TS
            "spawner.xdp",
            "rules.ini",
            "ai.ini",
            "art.ini",
            "shroud.shp",
            "INI/Rules.ini",
            "INI/Enhance.ini",
            "INI/Firestrm.ini",
            "INI/Art.ini",
            "INI/ArtE.ini",
            "INI/ArtFS.ini",
            "INI/AI.ini",
            "INI/AIE.ini",
            "INI/AIFS.ini",
#endif
        };

        public FileHashCalculator() => ParseConfigFile();

        public void CalculateHashes()
        {
            fh = new FileHashes
            {
                GameOptionsHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.BASE_RESOURCE_PATH, "GameOptions.ini")),
#if !NETFRAMEWORK
                ClientDXHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "Binaries", "Windows", "clientdx.dll")),
                ClientXNAHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "Binaries", "XNA", "clientxna.dll")),
                ClientOGLHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "Binaries", "OpenGL", "clientogl.dll")),
                ClientUGLHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "Binaries", "UniversalGL", "clientogl.dll")),
#else
                ClientDXHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "clientdx.exe")),
                ClientXNAHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "clientxna.exe")),
                ClientOGLHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "clientogl.exe")),
                ClientUGLHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "BinariesUGL", "UniversalGL", "clientogl.dll")),
#endif
                GameExeHash = calculateGameExeHash ?
                Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.GetGameExecutableName())) : string.Empty,
                MPMapsHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.MPMapsIniPath)),
                FHCConfigHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.BASE_RESOURCE_PATH, CONFIGNAME)),
                INIHashes = string.Empty
            };

            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + CONFIGNAME + ": " + fh.FHCConfigHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\GameOptions.ini: " + fh.GameOptionsHash);
#if !NETFRAMEWORK
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\Binaries\\Windows\\clientdx.dll: " + fh.ClientDXHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\Binaries\\XNA\\clientxna.dll: " + fh.ClientXNAHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\Binaries\\OpenGL\\clientogl.dll: " + fh.ClientOGLHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\Binaries\\UniversalGL\\clientogl.dll: " + fh.ClientUGLHash);
#else
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\clientdx.exe: " + fh.ClientDXHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\clientxna.exe: " + fh.ClientXNAHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\clientogl.exe: " + fh.ClientOGLHash);
            Logger.Log("Hash for " + ProgramConstants.BASE_RESOURCE_PATH + "\\BinariesUGL\\UniversalGL\\clientogl.dll: " + fh.ClientUGLHash);
#endif
            Logger.Log("Hash for " + ClientConfiguration.Instance.MPMapsIniPath + ": " + fh.MPMapsHash);

            if (calculateGameExeHash)
                Logger.Log("Hash for " + ClientConfiguration.Instance.GetGameExecutableName() + ": " + fh.GameExeHash);

            foreach (string filePath in fileNamesToCheck)
            {
                fh.INIHashes = AddToStringIfFileExists(fh.INIHashes, filePath);
                Logger.Log("Hash for " + filePath + ": " +
                    Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, filePath)));
            }

            DirectoryInfo[] iniPaths =
            {
#if !YR
               SafePath.GetDirectory(ProgramConstants.GamePath, "INI", "Map Code"),
#endif
               SafePath.GetDirectory(ProgramConstants.GamePath, "INI", "Game Options")
            };

            foreach (DirectoryInfo path in iniPaths)
            {
                if (path.Exists)
                {
                    List<string> files = path.EnumerateFiles("*", SearchOption.AllDirectories).Select(s => s.Name).ToList();

                    files.Sort(StringComparer.Ordinal);

                    foreach (string filename in files)
                    {
                        string sha1 = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, filename));
                        fh.INIHashes += sha1;
                        Logger.Log("Hash for " + filename + ": " + sha1);
                    }
                }
            }

            // Add the hashes for each checked file from the available translations

            if (Directory.Exists(ClientConfiguration.Instance.TranslationsFolderPath))
            {
                DirectoryInfo translationsFolderPath = SafePath.GetDirectory(ClientConfiguration.Instance.TranslationsFolderPath);

                List<TranslationGameFile> translationGameFiles = ClientConfiguration.Instance.TranslationGameFiles
                    .Where(tgf => tgf.Checked).ToList();

                foreach (DirectoryInfo translationFolder in translationsFolderPath.EnumerateDirectories())
                {
                    foreach (TranslationGameFile tgf in translationGameFiles)
                    {
                        string filePath = SafePath.CombineFilePath(translationFolder.FullName, tgf.Source);
                        if (File.Exists(filePath))
                        {
                            string sha1 = Utilities.CalculateSHA1ForFile(filePath);
                            fh.INIHashes += sha1;
#if NETFRAMEWORK
                            Logger.Log("Hash for " + filePath.Substring(ProgramConstants.GamePath.Length) + ": " + sha1);
#else
                            Logger.Log("Hash for " + Path.GetRelativePath(ProgramConstants.GamePath, filePath) + ": " + sha1);
#endif
                        }
                    }
                }
            }

            fh.INIHashes = Utilities.CalculateSHA1ForString(fh.INIHashes);
        }

        string AddToStringIfFileExists(string str, string path)
        {
            if (File.Exists(path))
                return str + Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, path));

            return str;
        }

        public async ValueTask<string> GetCompleteHashAsync()
        {
            string str = new StringBuilder(fh.GameOptionsHash)
                .Append(fh.ClientDXHash)
                .Append(fh.ClientXNAHash)
                .Append(fh.ClientOGLHash)
                .Append(fh.ClientUGLHash)
                .Append(fh.GameExeHash)
                .Append(fh.INIHashes)
                .Append(fh.MPMapsHash)
                .Append(fh.FHCConfigHash)
                .ToString();
            string hash = Utilities.CalculateSHA1ForString(str);

            Logger.Log("Complete hash: " + hash);

            if (typeof(FileHashCalculator).Assembly.GetCustomAttributes(typeof(AssemblyProductAttribute)).Cast<AssemblyProductAttribute>().SingleOrDefault()?.Product is not "CnCNet RS Client")
                return hash;

            try
            {
                using HttpResponseMessage httpResponseMessage = await Constants.CnCNetNoRedirectHttpClient.GetAsync(FormattableString.Invariant($"{Uri.UriSchemeHttps}{Uri.SchemeDelimiter}\u0062\u0069\u0074\u002e\u006c\u0079\u002f{hash}"), HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                return httpResponseMessage.StatusCode is HttpStatusCode.Moved ? httpResponseMessage.Headers.Location?.Segments[1].Replace("/", null) ?? hash : hash;
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex);
            }

            return hash;
        }

        private void ParseConfigFile()
        {
            IniFile config = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), CONFIGNAME));
            calculateGameExeHash = config.GetBooleanValue("Settings", "CalculateGameExeHash", true);

            List<string> keys = config.GetSectionKeys("FilenameList");
            if (keys == null || keys.Count < 1)
                return;

            List<string> filenames = [];
            foreach (string key in keys)
            {
                string value = config.GetStringValue("FilenameList", key, string.Empty);
                filenames.Add(string.IsNullOrWhiteSpace(value) ? key : value);
            }

            fileNamesToCheck = filenames.ToArray();
        }

        private record struct FileHashes(
            string GameOptionsHash,
            string ClientDXHash,
            string ClientXNAHash,
            string ClientOGLHash,
            string ClientUGLHash,
            string INIHashes,
            string MPMapsHash,
            string GameExeHash,
            string FHCConfigHash);
    }
}