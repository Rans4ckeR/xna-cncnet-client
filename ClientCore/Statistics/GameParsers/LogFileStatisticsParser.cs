﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Rampastring.Tools;
using ClientCore.Extensions;

namespace ClientCore.Statistics.GameParsers
{
    public sealed class LogFileStatisticsParser
    {
        private readonly ILogger logger;

        public LogFileStatisticsParser(ILogger logger)
        {
            this.logger = logger;
        }

        private MatchStatistics Statistics { get; set; }

        private string fileName = "DTA.log";
        private string economyString = "Economy"; // RA2/YR do not have economy stat, but a number of built objects.

        public void ParseStats(string gamepath, string fileName, bool isLoadedGame)
        {
            this.fileName = fileName;
            if (ClientConfiguration.Instance.UseBuiltStatistic) economyString = "Built";
            ParseStatistics(gamepath, isLoadedGame);
        }

        private void ParseStatistics(string gamepath, bool isLoadedGame)
        {
            FileInfo statisticsFileInfo = SafePath.GetFile(gamepath, fileName);

            if (!statisticsFileInfo.Exists)
            {
                logger.LogInformation("DTAStatisticsParser: Failed to read statistics: the log file does not exist.");
                return;
            }

            logger.LogInformation("Attempting to read statistics from " + fileName);

            try
            {
                using StreamReader reader = new StreamReader(statisticsFileInfo.OpenRead());

                string line;

                List<PlayerStatistics> takeoverAIs = new List<PlayerStatistics>();
                PlayerStatistics currentPlayer = null;

                bool sawCompletion = false;
                int numPlayersFound = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(": Loser"))
                    {
                        // Player found, game saw completion
                        sawCompletion = true;
                        string playerName = line[..^7];
                        currentPlayer = Statistics.GetEmptyPlayerByName(playerName);

                        if (isLoadedGame && currentPlayer == null)
                            currentPlayer = Statistics.Players.Find(p => p.Name == playerName);

                        logger.LogInformation("Found player " + playerName);
                        numPlayersFound++;

                        if (currentPlayer == null && playerName == "Computer" && numPlayersFound <= Statistics.NumberOfHumanPlayers)
                        {
                            // The player has been taken over by an AI during the match
                            logger.LogInformation("Losing take-over AI found");
                            takeoverAIs.Add(new PlayerStatistics("Computer", false, true, false, 0, 10, 255, 1));
                            currentPlayer = takeoverAIs[takeoverAIs.Count - 1];
                        }

                        if (currentPlayer != null)
                            currentPlayer.SawEnd = true;
                    }
                    else if (line.Contains(": Winner"))
                    {
                        // Player found, game saw completion
                        sawCompletion = true;
                        string playerName = line[..^8];
                        currentPlayer = Statistics.GetEmptyPlayerByName(playerName);

                        if (isLoadedGame && currentPlayer == null)
                            currentPlayer = Statistics.Players.Find(p => p.Name == playerName);

                        logger.LogInformation("Found player " + playerName);
                        numPlayersFound++;

                        if (currentPlayer == null && playerName == "Computer" && numPlayersFound <= Statistics.NumberOfHumanPlayers)
                        {
                            // The player has been taken over by an AI during the match
                            logger.LogInformation("Winning take-over AI found");
                            takeoverAIs.Add(new PlayerStatistics("Computer", false, true, false, 0, 10, 255, 1));
                            currentPlayer = takeoverAIs[takeoverAIs.Count - 1];
                        }

                        if (currentPlayer != null)
                        {
                            currentPlayer.SawEnd = true;
                            currentPlayer.Won = true;
                        }
                    }
                    else if (line.Contains("Game loop finished. Average FPS"))
                    {
                        // Game loop finished. Average FPS = <integer>
                        string fpsString = line[34..];
                        Statistics.AverageFPS = Int32.Parse(fpsString);
                    }

                    if (currentPlayer == null || line.Length < 1)
                        continue;

                    line = line[1..];

                    if (line.StartsWith("Lost = "))
                        currentPlayer.Losses = Int32.Parse(line[7..]);
                    else if (line.StartsWith("Kills = "))
                        currentPlayer.Kills = Int32.Parse(line[8..]);
                    else if (line.StartsWith("Score = "))
                        currentPlayer.Score = Int32.Parse(line[8..]);
                    else if (line.StartsWith(economyString + " = "))
                        currentPlayer.Economy = Int32.Parse(line[(economyString.Length + 2)..]);
                }

                // Check empty players for take-over by AIs
                if (takeoverAIs.Count == 1)
                {
                    PlayerStatistics ai = takeoverAIs[0];

                    PlayerStatistics ps = Statistics.GetFirstEmptyPlayer();

                    ps.Losses = ai.Losses;
                    ps.Kills = ai.Kills;
                    ps.Score = ai.Score;
                    ps.Economy = ai.Economy;
                }
                else if (takeoverAIs.Count > 1)
                {
                    // If there's multiple take-over AI players, we have no way of figuring out
                    // which AI represents which player, so let's just add the AIs into the player list
                    // (then the user viewing the statistics can figure it out themselves)
                    for (int i = 0; i < takeoverAIs.Count; i++)
                    {
                        takeoverAIs[i].SawEnd = false;
                        Statistics.AddPlayer(takeoverAIs[i]);
                    }
                }

                Statistics.SawCompletion = sawCompletion;
            }
            catch (Exception ex)
            {
                logger.LogExceptionDetails(ex, "DTAStatisticsParser: Error parsing statistics from match!");
            }
        }
    }
}