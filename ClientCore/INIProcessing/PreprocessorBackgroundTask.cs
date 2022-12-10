using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClientCore.Extensions;
using Microsoft.Extensions.Logging;
using Rampastring.Tools;

namespace ClientCore.INIProcessing
{
    /// <summary>
    /// Background task for pre-processing INI files.
    /// Singleton.
    /// </summary>
    public sealed class PreprocessorBackgroundTask
    {
        private readonly ILogger logger;

        public PreprocessorBackgroundTask(ILogger logger)
        {
            this.logger = logger;
        }

        private Task task;

        public bool IsRunning => !task.IsCompleted;

        public void Run()
        {
            task = Task.Run(CheckFiles).HandleTask();
        }

        private void CheckFiles()
        {
            logger.LogInformation("Starting background processing of INI files.");

            DirectoryInfo iniFolder = SafePath.GetDirectory(ProgramConstants.GamePath, "INI", "Base");

            if (!iniFolder.Exists)
            {
                logger.LogInformation("/INI/Base does not exist, skipping background processing of INI files.");
                return;
            }

            IniPreprocessInfoStore infoStore = new IniPreprocessInfoStore();
            string errorKey = infoStore.Load();

            if (!string.IsNullOrEmpty(errorKey))
                logger.LogInformation("Failed to parse preprocessed INI info, key " + errorKey);

            IniPreprocessor processor = new IniPreprocessor();

            IEnumerable<FileInfo> iniFiles = iniFolder.EnumerateFiles("*.ini", SearchOption.TopDirectoryOnly);

            int processedCount = 0;

            foreach (FileInfo iniFile in iniFiles)
            {
                if (!infoStore.IsIniUpToDate(iniFile.Name))
                {
                    logger.LogInformation("INI file " + iniFile.Name + " is not processed or outdated, re-processing it.");

                    string sourcePath = iniFile.FullName;
                    string destinationPath = SafePath.CombineFilePath(ProgramConstants.GamePath, "INI", iniFile.Name);

                    processor.ProcessIni(sourcePath, destinationPath);

                    string sourceHash = Utilities.CalculateSHA1ForFile(sourcePath);
                    string destinationHash = Utilities.CalculateSHA1ForFile(destinationPath);
                    infoStore.UpsertRecord(iniFile.Name, sourceHash, destinationHash);
                    processedCount++;
                }
                else
                {
                    logger.LogInformation("INI file " + iniFile.Name + " is up to date.");
                }
            }

            if (processedCount > 0)
            {
                logger.LogInformation("Writing preprocessed INI info store.");
                infoStore.Write();
            }

            logger.LogInformation("Ended background processing of INI files.");
        }
    }
}