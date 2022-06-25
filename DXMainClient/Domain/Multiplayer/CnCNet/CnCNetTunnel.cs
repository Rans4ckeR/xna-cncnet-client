﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// A CnCNet tunnel server.
/// </summary>
public class CnCNetTunnel
{
    private const int PING_TIMEOUT = 1000;
    private const int REQUEST_TIMEOUT = 10000; // In milliseconds

    public CnCNetTunnel()
    {
    }

    public string Address { get; private set; }

    public int Clients { get; private set; }

    public string Country { get; private set; }

    public string CountryCode { get; private set; }

    public double Distance { get; private set; }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public int MaxClients { get; private set; }

    public string Name { get; private set; }

    public bool Official { get; private set; }

    public int PingInMs { get; set; } = -1;

    public int Port { get; private set; }

    public bool Recommended { get; private set; }

    public bool RequiresPassword { get; private set; }

    public int Version { get; private set; }

    /// <summary>
    /// Parses a formatted string that contains the tunnel server's information into a CnCNetTunnel instance.
    /// </summary>
    /// <param name="str">The string that contains the tunnel server's information.</param>
    /// <returns>A CnCNetTunnel instance parsed from the given string.</returns>
    public static CnCNetTunnel Parse(string str)
    {
        // For the format, check http://cncnet.org/master-list
        try
        {
            CnCNetTunnel tunnel = new();
            string[] parts = str.Split(';');

            string address = parts[0];
            string[] detailedAddress = address.Split(new char[] { ':' });

            tunnel.Address = detailedAddress[0];
            tunnel.Port = int.Parse(detailedAddress[1]);
            tunnel.Country = parts[1];
            tunnel.CountryCode = parts[2];
            tunnel.Name = parts[3];
            tunnel.RequiresPassword = parts[4] != "0";
            tunnel.Clients = int.Parse(parts[5]);
            tunnel.MaxClients = int.Parse(parts[6]);
            int status = int.Parse(parts[7]);
            tunnel.Official = status == 2;
            if (!tunnel.Official)
                tunnel.Recommended = status == 1;

            CultureInfo cultureInfo = CultureInfo.InvariantCulture;

            tunnel.Latitude = double.Parse(parts[8], cultureInfo);
            tunnel.Longitude = double.Parse(parts[9], cultureInfo);
            tunnel.Version = int.Parse(parts[10]);
            tunnel.Distance = double.Parse(parts[11], cultureInfo);

            return tunnel;
        }
        catch (Exception ex)
        {
            if (ex is FormatException or OverflowException or IndexOutOfRangeException)
            {
                Logger.Log("Parsing tunnel information failed: " + ex.Message + Environment.NewLine + "Parsed string: " + str);
                return null;
            }

            throw;
        }
    }

    /// <summary>
    /// Gets a list of player ports to use from a specific tunnel server.
    /// </summary>
    /// <param name="playerCount">player count.</param>
    /// <returns>A list of player ports to use.</returns>
    public List<int> GetPlayerPortInfo(int playerCount)
    {
        try
        {
            Logger.Log($"Contacting tunnel at {Address}:{Port}");

            string addressString = $"http://{Address}:{Port}/request?clients={playerCount}";
            Logger.Log($"Downloading from {addressString}");

            using ExtendedWebClient client = new(REQUEST_TIMEOUT);
            string data = client.DownloadString(addressString);

            data = data.Replace("[", string.Empty);
            data = data.Replace("]", string.Empty);

            string[] portIDs = data.Split(',');
            List<int> playerPorts = new();

            foreach (string port in portIDs)
            {
                playerPorts.Add(Convert.ToInt32(port));
                Logger.Log($"Added port {port}");
            }

            return playerPorts;
        }
        catch (Exception ex)
        {
            Logger.Log("Unable to connect to the specified tunnel server. Returned error message: " + ex.Message);
        }

        return new List<int>();
    }

    public void UpdatePing()
    {
        using Ping p = new();
        try
        {
            PingReply reply = p.Send(IPAddress.Parse(Address), PING_TIMEOUT);
            if (reply.Status == IPStatus.Success)
                PingInMs = Convert.ToInt32(reply.RoundtripTime);
        }
        catch (PingException ex)
        {
            Logger.Log($"Caught an exception when pinging {Name} tunnel server: {ex.Message}");
        }
    }
}