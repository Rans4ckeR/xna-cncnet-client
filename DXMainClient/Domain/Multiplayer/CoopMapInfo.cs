using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer;

public class CoopMapInfo
{
    [JsonProperty]
    public List<CoopHouseInfo> AllyHouses { get; set; } = new();

    [JsonProperty]
    public List<int> DisallowedPlayerColors { get; set; } = new();

    [JsonProperty]
    public List<int> DisallowedPlayerSides { get; set; } = new();

    [JsonProperty]
    public List<CoopHouseInfo> EnemyHouses { get; set; } = new();

    public void SetHouseInfos(IniSection iniSection)
    {
        EnemyHouses = GetGenericHouseInfo(iniSection, "EnemyHouse");
        AllyHouses = GetGenericHouseInfo(iniSection, "AllyHouse");
    }

    private static List<CoopHouseInfo> GetGenericHouseInfo(IniSection iniSection, string keyName)
    {
        List<CoopHouseInfo> houseList = new();

        for (int i = 0; ; i++)
        {
            string[] houseInfo = iniSection.GetStringValue(keyName + i, string.Empty).Split(
                new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (houseInfo.Length == 0)
                break;

            int[] info = Conversions.IntArrayFromStringArray(houseInfo);
            _ = new CoopHouseInfo(info[0], info[1], info[2]);

            houseList.Add(new CoopHouseInfo(info[0], info[1], info[2]));
        }

        return houseList;
    }
}