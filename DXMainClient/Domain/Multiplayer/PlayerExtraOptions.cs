﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Localization;

namespace DTAClient.Domain.Multiplayer;

public class PlayerExtraOptions
{
    public const string CNCNETMESSAGEKEY = "PEO";
    public const string LANMESSAGEKEY = "PEOPTS";
    private const char MESSAGE_SEPARATOR = ';';

    public bool IsForceRandomColors { get; set; }

    public bool IsForceRandomSides { get; set; }

    public bool IsForceRandomStarts { get; set; }

    public bool IsForceRandomTeams { get; set; }

    public bool IsUseTeamStartMappings { get; set; }

    public List<TeamStartMapping> TeamStartMappings { get; set; }

    protected static string MULTIPLEMAPPINGSASSIGNEDTOSAMESTART => MAPPING_ERROR_PREFIX + " " + "Multiple mappings assigned to the same start location.".L10N("UI:Main:MultipleMappingsAssigned");

    protected static string NOTALLMAPPINGSASSIGNED => MAPPING_ERROR_PREFIX + " " + "You must have all mappings assigned.".L10N("UI:Main:NotAllMappingsAssigned");

    protected static string ONLYONETEAM => MAPPING_ERROR_PREFIX + " " + "You must have more than one team assigned.".L10N("UI:Main:OnlyOneTeam");

    private static string INVALID_OPTIONS_MESSAGE => "Invalid player extra options message".L10N("UI:Main:InvalidPlayerExtraOptionsMessage");

    private static string MAPPING_ERROR_PREFIX => "Auto Allying:".L10N("UI:Main:AutoAllyingPrefix");

    public static PlayerExtraOptions FromMessage(string message)
    {
        string[] parts = message.Split(MESSAGE_SEPARATOR);
        if (parts.Length < 2)
            throw new InvalidOperationException(INVALID_OPTIONS_MESSAGE);

        char[] boolParts = parts[0].ToCharArray();
        if (boolParts.Length < 5)
            throw new InvalidOperationException(INVALID_OPTIONS_MESSAGE);

        return new PlayerExtraOptions
        {
            IsForceRandomSides = boolParts[0] == '1',
            IsForceRandomColors = boolParts[1] == '1',
            IsForceRandomTeams = boolParts[2] == '1',
            IsForceRandomStarts = boolParts[3] == '1',
            IsUseTeamStartMappings = boolParts[4] == '1',
            TeamStartMappings = TeamStartMapping.FromListString(parts[1])
        };
    }

    public string GetTeamMappingsError()
    {
        if (!IsUseTeamStartMappings)
            return null;

        IEnumerable<int> distinctStartLocations = TeamStartMappings.Select(m => m.Start).Distinct();
        if (distinctStartLocations.Count() != TeamStartMappings.Count)
            return MULTIPLEMAPPINGSASSIGNEDTOSAMESTART; // multiple mappings are using the same spawn location

        IEnumerable<string> distinctTeams = TeamStartMappings.Select(m => m.Team).Distinct();
        if (distinctTeams.Count() < 2)
            return ONLYONETEAM; // must have more than one team assigned

        return null;
    }

    public bool IsDefault()
    {
        PlayerExtraOptions defaultPLayerExtraOptions = new();
        return IsForceRandomColors == defaultPLayerExtraOptions.IsForceRandomColors &&
               IsForceRandomStarts == defaultPLayerExtraOptions.IsForceRandomStarts &&
               IsForceRandomTeams == defaultPLayerExtraOptions.IsForceRandomTeams &&
               IsForceRandomSides == defaultPLayerExtraOptions.IsForceRandomSides &&
               IsUseTeamStartMappings == defaultPLayerExtraOptions.IsUseTeamStartMappings;
    }

    public string ToCncnetMessage() => $"{CNCNETMESSAGEKEY} {ToString()}";

    public string ToLanMessage() => $"{LANMESSAGEKEY} {ToString()}";

    public override string ToString()
    {
        StringBuilder stringBuilder = new();
        _ = stringBuilder.Append(IsForceRandomSides ? "1" : "0");
        _ = stringBuilder.Append(IsForceRandomColors ? "1" : "0");
        _ = stringBuilder.Append(IsForceRandomTeams ? "1" : "0");
        _ = stringBuilder.Append(IsForceRandomStarts ? "1" : "0");
        _ = stringBuilder.Append(IsUseTeamStartMappings ? "1" : "0");
        _ = stringBuilder.Append(MESSAGE_SEPARATOR);
        _ = stringBuilder.Append(TeamStartMapping.ToListString(TeamStartMappings));

        return stringBuilder.ToString();
    }
}