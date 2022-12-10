using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClientCore.Extensions;
using Microsoft.Extensions.Logging;
using Rampastring.Tools;

namespace ClientCore
{
    /// <summary>
    /// A class for handling saved multiplayer games.
    /// </summary>
    public sealed class SavedGameManager
    {
        private static bool saveRenameInProgress;

        private readonly ILogger logger;

        public SavedGameManager(ILogger logger)
        {
            this.logger = logger;
        }

        public static int GetSaveGameCount()
        {
            string saveGameDirectory = GetSaveGameDirectoryPath();

            if (!AreSavedGamesAvailable())
                return 0;

            for (int i = 0; i < 1000; i++)
            {
                if (!SafePath.GetFile(saveGameDirectory, string.Format("SVGM_{0}.NET", i.ToString("D3"))).Exists)
                {
                    return i;
                }
            }

            return 1000;
        }

        public static List<string> GetSaveGameTimestamps()
        {
            int saveGameCount = GetSaveGameCount();

            List<string> timestamps = new List<string>();

            string saveGameDirectory = GetSaveGameDirectoryPath();

            for (int i = 0; i < saveGameCount; i++)
            {
                FileInfo sgFile = SafePath.GetFile(saveGameDirectory, string.Format("SVGM_{0}.NET", i.ToString("D3")));

                DateTime dt = sgFile.LastWriteTime;

                timestamps.Add(dt.ToString());
            }

            return timestamps;
        }

        public static bool AreSavedGamesAvailable()
        {
            if (Directory.Exists(GetSaveGameDirectoryPath()))
                return true;

            return false;
        }

        private static string GetSaveGameDirectoryPath()
        {
            return SafePath.CombineDirectoryPath(ProgramConstants.GamePath, ProgramConstants.SAVED_GAMES_DIRECTORY);
        }

        /// <summary>
        /// Initializes saved MP games for a match.
        /// </summary>
        public bool InitSavedGames()
        {
            bool success = EraseSavedGames();

            if (!success)
                return false;

            try
            {
                logger.LogInformation("Writing spawn.ini for saved game.");
                SafePath.DeleteFileIfExists(ProgramConstants.GamePath, ProgramConstants.SAVED_GAME_SPAWN_INI);
                File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS), SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SAVED_GAME_SPAWN_INI));
            }
            catch (Exception ex)
            {
                logger.LogExceptionDetails(ex, "Writing spawn.ini for saved game failed!");
                return false;
            }

            return true;
        }

        public async ValueTask RenameSavedGameAsync()
        {
            logger.LogInformation("Renaming saved game.");

            if (saveRenameInProgress)
            {
                logger.LogInformation("Save renaming in progress!");
                return;
            }

            string saveGameDirectory = GetSaveGameDirectoryPath();

            if (!SafePath.GetFile(saveGameDirectory, "SAVEGAME.NET").Exists)
            {
                logger.LogInformation("SAVEGAME.NET doesn't exist!");
                return;
            }

            saveRenameInProgress = true;

            int saveGameId = 0;

            for (int i = 0; i < 1000; i++)
            {
                if (!SafePath.GetFile(saveGameDirectory, string.Format("SVGM_{0}.NET", i.ToString("D3"))).Exists)
                {
                    saveGameId = i;
                    break;
                }
            }

            if (saveGameId == 999)
            {
                if (SafePath.GetFile(saveGameDirectory, "SVGM_999.NET").Exists)
                    logger.LogInformation("1000 saved games exceeded! Overwriting previous MP save.");
            }

            string sgPath = SafePath.CombineFilePath(saveGameDirectory, string.Format("SVGM_{0}.NET", saveGameId.ToString("D3")));

            int tryCount = 0;

            while (true)
            {
                try
                {
                    File.Move(SafePath.CombineFilePath(saveGameDirectory, "SAVEGAME.NET"), sgPath);
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogExceptionDetails(ex, "Renaming saved game failed!");
                }

                tryCount++;

                if (tryCount > 40)
                {
                    logger.LogInformation("Renaming saved game failed 40 times! Aborting.");
                    return;
                }

                await Task.Delay(250);
            }

            saveRenameInProgress = false;

            logger.LogInformation("Saved game SAVEGAME.NET succesfully renamed to " + Path.GetFileName(sgPath));
        }

        private bool EraseSavedGames()
        {
            logger.LogInformation("Erasing previous MP saved games.");

            try
            {
                for (int i = 0; i < 1000; i++)
                {
                    SafePath.DeleteFileIfExists(GetSaveGameDirectoryPath(), string.Format("SVGM_{0}.NET", i.ToString("D3")));
                }
            }
            catch (Exception ex)
            {
                logger.LogExceptionDetails(ex, "Erasing previous MP saved games failed!");
                return false;
            }

            logger.LogInformation("MP saved games succesfully erased.");
            return true;
        }
    }
}