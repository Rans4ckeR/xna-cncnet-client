using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Extensions;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.Replays;

internal sealed class ReplayHandler : IAsyncDisposable
{
    private readonly Dictionary<uint, FileStream> replayFileStreams = [];

    private DateTimeOffset startTimestamp;
    private DirectoryInfo replayDirectory;
    private bool gameStarted;
    private int replayId;
    private uint gameLocalPlayerId;

    public void SetupRecording(int replayId, uint gameLocalPlayerId)
    {
        this.replayId = replayId;
        this.gameLocalPlayerId = gameLocalPlayerId;
        startTimestamp = DateTimeOffset.Now;
        replayDirectory = SafePath.GetDirectory(ProgramConstants.GamePath, ProgramConstants.REPLAYS_DIRECTORY, replayId.ToString(CultureInfo.InvariantCulture));
        gameStarted = false;

        replayDirectory.Create();
        replayFileStreams.Add(gameLocalPlayerId, CreateReplayFileStream());
    }

    public async ValueTask StopRecordingAsync(List<uint> gamePlayerIds, List<PlayerInfo> playerInfos, List<V3GameTunnelHandler> v3GameTunnelHandlers)
    {
        foreach (V3GameTunnelHandler v3GameTunnelHandler in v3GameTunnelHandlers)
        {
            v3GameTunnelHandler.RaiseRemoteHostDataReceivedEvent -= RemoteHostConnection_DataReceivedAsync;
            v3GameTunnelHandler.RaiseLocalGameDataReceivedEvent -= LocalGameConnection_DataReceivedAsync;
        }

        if (!(replayDirectory?.Exists ?? false))
            return;

        FileInfo spawnFile = SafePath.GetFile(replayDirectory.FullName, ProgramConstants.SPAWNER_SETTINGS);
        string settings = null;
        Dictionary<uint, string> playerMappings = [];

        if (spawnFile.Exists)
        {
#if NETFRAMEWORK
            settings = File.ReadAllText(spawnFile.FullName);
#else
            settings = await File.ReadAllTextAsync(spawnFile.FullName, CancellationToken.None).ConfigureAwait(false);
#endif
            var spawnIni = new IniFile(spawnFile.FullName);
            IniSection settingsSection = spawnIni.GetSection("Settings");
            string playerName = settingsSection.GetStringValue("Name", null);
            uint playerId = gamePlayerIds[playerInfos.Single(q => q.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase)).Index];

            playerMappings.Add(playerId, playerName);

            for (int i = 1; i < settingsSection.GetIntValue("PlayerCount", 0); i++)
            {
                IniSection otherPlayerSection = spawnIni.GetSection($"Other{i}");

                if (otherPlayerSection is not null)
                {
                    playerName = otherPlayerSection.GetStringValue("Name", null);
                    playerId = gamePlayerIds[playerInfos.Single(q => q.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase)).Index];

                    playerMappings.Add(playerId, playerName);
                }
            }
        }

        List<ReplayData> replayDataList = await GenerateReplayDataAsync().ConfigureAwait(false);
        var replay = new Replay(replayId, settings, startTimestamp, gameLocalPlayerId, playerMappings, replayDataList.OrderBy(q => q.TimestampOffset).ToList());
        var tempReplayFileStream = new MemoryStream();

#if NETFRAMEWORK
        using (tempReplayFileStream)
#else
        await using (tempReplayFileStream.ConfigureAwait(false))
#endif
        {
            await JsonSerializer.SerializeAsync(tempReplayFileStream, replay, cancellationToken: CancellationToken.None).ConfigureAwait(false);

            tempReplayFileStream.Position = 0L;

            FileStream replayFileStream = new(
                SafePath.CombineFilePath(replayDirectory.Parent.FullName, FormattableString.Invariant($"{replayId}.cnc")),
#if NETFRAMEWORK
                FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
#else
                new FileStreamOptions
                {
                    Access = FileAccess.Write,
                    Mode = FileMode.CreateNew,
                    Options = FileOptions.Asynchronous
                });
#endif

#if NETFRAMEWORK
            using (replayFileStream)
#else
            await using (replayFileStream.ConfigureAwait(false))
#endif
            {
                var compressionStream = new GZipStream(replayFileStream, CompressionMode.Compress);

#if NETFRAMEWORK
                using (compressionStream)
                {
                    await tempReplayFileStream.CopyToAsync(compressionStream, 81920, CancellationToken.None).ConfigureAwait(false);
                }
#else
                await using (compressionStream.ConfigureAwait(false))
                {
                    await tempReplayFileStream.CopyToAsync(compressionStream, CancellationToken.None).ConfigureAwait(false);
                }
#endif
            }
        }

        SafePath.DeleteFileIfExists(spawnFile.FullName);
    }

    public async ValueTask DisposeAsync()
    {
        foreach ((_, FileStream fileStream) in replayFileStreams)
#if NETFRAMEWORK
        {
            await default(ValueTask).ConfigureAwait(false);
            fileStream.Dispose();
        }
#else
            await fileStream.DisposeAsync().ConfigureAwait(false);
#endif

        replayFileStreams.Clear();
        replayDirectory?.Refresh();

        if (replayDirectory?.Exists ?? false)
            SafePath.DeleteDirectoryIfExists(true, replayDirectory.FullName);
    }

    public void RemoteHostConnection_DataReceivedAsync(object sender, DataReceivedEventArgs e)
        => SaveReplayDataAsync(((V3RemotePlayerConnection)sender).PlayerId, e).HandleTask();

    public void LocalGameConnection_DataReceivedAsync(object sender, DataReceivedEventArgs e)
    {
        if (!gameStarted)
        {
            gameStarted = true;

            FileInfo spawnFileInfo = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);

            spawnFileInfo.CopyTo(SafePath.CombineFilePath(replayDirectory.FullName, spawnFileInfo.Name));
        }

        SaveReplayDataAsync(((V3LocalPlayerConnection)sender).PlayerId, e).HandleTask();
    }

    private async ValueTask<List<ReplayData>> GenerateReplayDataAsync()
    {
        var replayDataList = new List<ReplayData>();

        foreach (FileStream fileStream in replayFileStreams.Values.Where(q => q.Length > 0L))
        {
            await fileStream.WriteAsync(new UTF8Encoding().GetBytes(new[] { ']' })).ConfigureAwait(false);

            fileStream.Position = 0L;

            replayDataList.AddRange(await JsonSerializer.DeserializeAsync<List<ReplayData>>(
                fileStream, new JsonSerializerOptions { AllowTrailingCommas = true }, cancellationToken: CancellationToken.None).ConfigureAwait(false));
        }

        return replayDataList;
    }

    private async ValueTask SaveReplayDataAsync(uint playerId, DataReceivedEventArgs e)
    {
        if (!replayFileStreams.TryGetValue(playerId, out FileStream fileStream))
        {
            fileStream = CreateReplayFileStream();

#if NETFRAMEWORK
            if (!replayFileStreams.ContainsKey(playerId))
            {
                replayFileStreams.Add(playerId, fileStream);
            }
            else
            {
                fileStream.Dispose();
            }
#else
            if (!replayFileStreams.TryAdd(playerId, fileStream))
                await fileStream.DisposeAsync().ConfigureAwait(false);
#endif

            replayFileStreams.TryGetValue(playerId, out fileStream);
        }

        if (fileStream.Position is 0L)
            await fileStream.WriteAsync(new UTF8Encoding().GetBytes(new[] { '[' })).ConfigureAwait(false);

        var replayData = new ReplayData(e.Timestamp - startTimestamp, playerId, e.GameData);
        var tempStream = new MemoryStream();

#if NETFRAMEWORK
        using (tempStream)
#else
        await using (tempStream.ConfigureAwait(false))
#endif
        {
            await JsonSerializer.SerializeAsync(tempStream, replayData, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            await tempStream.WriteAsync(new UTF8Encoding().GetBytes(new[] { ',' })).ConfigureAwait(false);

            tempStream.Position = 0L;

            await tempStream.CopyToAsync(fileStream).ConfigureAwait(false);
        }
    }

    private FileStream CreateReplayFileStream()
        => new(
            SafePath.CombineFilePath(replayDirectory.FullName, Guid.NewGuid().ToString()),
#if NETFRAMEWORK
            FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
#else
            new FileStreamOptions
            {
                Access = FileAccess.ReadWrite,
                Mode = FileMode.CreateNew,
                Options = FileOptions.Asynchronous | FileOptions.DeleteOnClose
            });
#endif
}