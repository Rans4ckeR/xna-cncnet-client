﻿using Rampastring.Tools;
using System;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// A player in the game lobby.
    /// </summary>
    public class PlayerInfo
    {
        public PlayerInfo() { }

        public PlayerInfo(string name)
        {
            Name = name;
        }

        public PlayerInfo(string name, int sideId, int startingLocation, int colorId, int teamId)
        {
            Name = name;
            SideId = sideId;
            StartingLocation = startingLocation;
            ColorId = colorId;
            TeamId = teamId;
        }

        public string Name { get; set; }
        public int SideId { get; set; }
        public int StartingLocation { get; set; }
        public int ColorId { get; set; }
        public int TeamId { get; set; }
        public bool Ready { get; set; }
        public bool AutoReady { get; set; }
        public bool IsAI { get; set; }

        public bool IsInGame { get; set; }
        public virtual string IPAddress { get; set; } = System.Net.IPAddress.Any.ToString();
        public int Port { get; set; }
        public bool Verified { get; set; }

        public int Index { get; set; }

        public int Ping { get; set; } = -1;

        /// <summary>
        /// Returns the "reversed" AI level ("how it was in Tiberian Sun UI") of the AI.
        /// 2 = Hard, 1 = Medium, 0 = Easy.
        /// </summary>
        public int ReversedAILevel
        {
            get { return Math.Abs(AILevel - 2); }
        }

        /// <summary>
        /// The AI level of the AI for the [HouseHandicaps] section in spawn.ini.
        /// 2 = Easy, 1 = Medium, 0 = Hard.
        /// </summary>
        public int AILevel { get; set; }

        public override string ToString()
        {
            var sb = new ExtendedStringBuilder(true, ',');
            sb.Append(Name);
            sb.Append(SideId);
            sb.Append(StartingLocation);
            sb.Append(ColorId);
            sb.Append(TeamId);
            sb.Append(AILevel);
            sb.Append(IsAI.ToString());
            sb.Append(Index);
            return sb.ToString();
        }

        /// <summary>
        /// Creates a PlayerInfo instance from a string in a format that matches the 
        /// string given by the ToString() method.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>A PlayerInfo instance, or null if the string format was invalid.</returns>
        public static PlayerInfo FromString(string str)
        {
            string[] values = str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (values.Length != 8)
                return null;

            return new PlayerInfo
            {
                Name = values[0],
                SideId = Conversions.IntFromString(values[1], 0),
                StartingLocation = Conversions.IntFromString(values[2], 0),
                ColorId = Conversions.IntFromString(values[3], 0),
                TeamId = Conversions.IntFromString(values[4], 0),
                AILevel = Conversions.IntFromString(values[5], 0),
                IsAI = Conversions.BooleanFromString(values[6], true),
                Index = Conversions.IntFromString(values[7], 0)
            };
        }
    }
}