using System;
using System.Net;
using System.Net.Http;

namespace DTAClient.Domain.Multiplayer.CnCNet;

internal static class Constants
{
#if NETFRAMEWORK
    public static HttpClient CnCNetHttpClient
        => new(
            new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            },
            true)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

    public static HttpClient CnCNetNoRedirectHttpClient
        => new(
            new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = false
            },
            true)
        {
            Timeout = TimeSpan.FromSeconds(10),
        };
#else
    public static HttpClient CnCNetHttpClient
        => new(
            new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                AutomaticDecompression = DecompressionMethods.All
            },
            true)
        {
            Timeout = TimeSpan.FromSeconds(10),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };

    public static HttpClient CnCNetNoRedirectHttpClient
        => new(
            new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = false
            },
            true)
        {
            Timeout = TimeSpan.FromSeconds(10),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };
#endif
}