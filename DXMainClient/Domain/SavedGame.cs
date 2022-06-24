﻿using System;
using System.IO;
using ClientCore;
using OpenMcdf;
using Rampastring.Tools;

namespace DTAClient.Domain;

/// <summary>
/// A single-player saved game.
/// </summary>
public class SavedGame
{
    private const string SAVED_GAME_PATH = "Saved Games/";

    public SavedGame(string fileName)
    {
        FileName = fileName;
    }

    public string FileName { get; private set; }

    public string GUIName { get; private set; }

    public DateTime LastModified { get; private set; }

    /// <summary>
    /// Reads and sets the saved game's name and last modified date, and returns true if succesful.
    /// </summary>
    /// <returns>True if parsing the info was succesful, otherwise false.</returns>
    public bool ParseInfo()
    {
        try
        {
            using (Stream file = File.Open(ProgramConstants.GamePath + SAVED_GAME_PATH + FileName, FileMode.Open, FileAccess.Read))
            {
                GUIName = GetArchiveName(file);
            }

            LastModified = File.GetLastWriteTime(ProgramConstants.GamePath + SAVED_GAME_PATH + FileName);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log("An error occured while parsing saved game " + FileName + ":" +
                ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Get the saved game's name from a .sav file.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private static string GetArchiveName(Stream file)
    {
        CompoundFile cf = new(file);
        byte[] archiveNameBytes = cf.RootStorage.GetStream("Scenario Description").GetData();
        string archiveName = System.Text.Encoding.Unicode.GetString(archiveNameBytes);
        archiveName = archiveName.TrimEnd(new char[] { '\0' });
        return archiveName;
    }
}