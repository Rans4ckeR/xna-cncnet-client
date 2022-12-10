using System;
using System.IO;
using ClientCore;
using OpenMcdf;
using Rampastring.Tools;

namespace DTAClient.Domain
{
    /// <summary>
    /// A single-player saved game.
    /// </summary>
    internal sealed class SavedGame
    {
        private const string SAVED_GAME_PATH = "Saved Games/";

        public string FileName { get; set; }
        public string GUIName { get; private set; }
        public DateTime LastModified { get; private set; }

        /// <summary>
        /// Get the saved game's name from a .sav file.
        /// </summary>
        private static string GetArchiveName(Stream file)
        {
            var cf = new CompoundFile(file);
            var archiveNameBytes = cf.RootStorage.GetStream("Scenario Description").GetData();
            var archiveName = System.Text.Encoding.Unicode.GetString(archiveNameBytes);
            archiveName = archiveName.TrimEnd(new char[] { '\0' });
            return archiveName;
        }

        /// <summary>
        /// Reads and sets the saved game's name and last modified date, and returns true if successful.
        /// </summary>
        public void ParseInfo()
        {
            FileInfo savedGameFileInfo = SafePath.GetFile(ProgramConstants.GamePath, SAVED_GAME_PATH, FileName);

            using (Stream file = savedGameFileInfo.Open(FileMode.Open, FileAccess.Read))
            {
                GUIName = GetArchiveName(file);
            }

            LastModified = savedGameFileInfo.LastWriteTime;
        }
    }
}