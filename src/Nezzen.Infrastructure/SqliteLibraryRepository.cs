using System.Text.Json;
using Microsoft.Data.Sqlite;
using Nezzen.Application;
using Nezzen.Core;

namespace Nezzen.Infrastructure;

public sealed class SqliteLibraryRepository : ILibraryRepository
{
    private readonly string connectionString;
    public SqliteLibraryRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        connectionString = new SqliteConnectionStringBuilder { DataSource = Path.GetFullPath(databasePath), ForeignKeys = true, Pooling = false }.ToString();
    }
    private async Task<SqliteConnection> OpenAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var connection = new SqliteConnection(connectionString);
        try { await connection.OpenAsync(token); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }
    private static SqliteCommand Command(SqliteConnection c, SqliteTransaction? tx, string sql)
    {
        var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql; return cmd;
    }
    private static async Task ExecuteAsync(SqliteConnection c, SqliteTransaction tx, string sql, CancellationToken token)
    {
        using var cmd = Command(c, tx, sql); await cmd.ExecuteNonQueryAsync(token);
    }
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        using var version = Command(connection, transaction, "PRAGMA user_version");
        var current = Convert.ToInt32(await version.ExecuteScalarAsync(cancellationToken));
        if (current > 2) throw new InvalidOperationException("Database schema is newer than this application.");
        if (current == 0)
            await ExecuteAsync(connection, transaction, """
                CREATE TABLE music_folders (id TEXT PRIMARY KEY, path TEXT NOT NULL UNIQUE);
                CREATE TABLE tracks (id TEXT PRIMARY KEY, folder_id TEXT NOT NULL REFERENCES music_folders(id), path TEXT NOT NULL UNIQUE, payload TEXT NOT NULL);
                CREATE INDEX ix_tracks_folder ON tracks(folder_id);
                """, cancellationToken);
        if (current < 2)
        {
            await ExecuteAsync(connection, transaction, """
                ALTER TABLE music_folders ADD COLUMN enabled INTEGER NOT NULL DEFAULT 1;
                ALTER TABLE music_folders ADD COLUMN path_key TEXT;
                ALTER TABLE tracks ADD COLUMN path_key TEXT;
                ALTER TABLE tracks ADD COLUMN content_hash TEXT;
                CREATE TABLE library_state (id INTEGER PRIMARY KEY CHECK(id=1), revision INTEGER NOT NULL);
                INSERT INTO library_state VALUES(1,0);
                CREATE TABLE scan_sessions (id TEXT PRIMARY KEY, started_utc TEXT NOT NULL, payload TEXT NOT NULL, result TEXT NOT NULL);
                CREATE TABLE scan_entries (session_id TEXT NOT NULL REFERENCES scan_sessions(id), ordinal INTEGER NOT NULL, payload TEXT NOT NULL, PRIMARY KEY(session_id,ordinal));
                CREATE INDEX ix_sessions_started ON scan_sessions(started_utc DESC);
                """, cancellationToken);
            var folders = await ReadFoldersAsync(connection, transaction, cancellationToken);
            foreach (var folder in folders)
            {
                using var cmd = Command(connection, transaction, "UPDATE music_folders SET path_key=$key WHERE id=$id");
                cmd.Parameters.AddWithValue("$key", LibraryPath.Key(folder.Path)); cmd.Parameters.AddWithValue("$id", folder.Id.ToString());
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            var tracks = await ReadTracksAsync(connection, transaction, null, cancellationToken);
            foreach (var track in tracks)
            {
                using var cmd = Command(connection, transaction, "UPDATE tracks SET path_key=$key,content_hash=$hash WHERE id=$id");
                cmd.Parameters.AddWithValue("$key", LibraryPath.Key(track.Path)); cmd.Parameters.AddWithValue("$hash", (object?)track.Fingerprint.ContentHash ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$id", track.Id.ToString()); await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            await ExecuteAsync(connection, transaction, """
                CREATE UNIQUE INDEX ux_folder_path_key ON music_folders(path_key);
                CREATE UNIQUE INDEX ux_track_path_key ON tracks(path_key);
                CREATE INDEX ix_tracks_hash ON tracks(content_hash);
                PRAGMA user_version = 2;
                """, cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested(); transaction.Commit();
    }
    private static async Task<long> RevisionAsync(SqliteConnection c, SqliteTransaction? tx, CancellationToken token)
    {
        using var cmd = Command(c, tx, "SELECT revision FROM library_state WHERE id=1");
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(token));
    }
    private static Task BumpAsync(SqliteConnection c, SqliteTransaction tx, CancellationToken token) => ExecuteAsync(c, tx, "UPDATE library_state SET revision=revision+1 WHERE id=1", token);
    public async Task SaveFolderAsync(MusicFolder folder, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(cancellationToken); using var tx = c.BeginTransaction();
        var existing = await ReadFoldersAsync(c, tx, cancellationToken);
        if (existing.Any(f => f.Id != folder.Id && (LibraryPath.Contains(f.Path, folder.Path) || LibraryPath.Contains(folder.Path, f.Path))))
            throw new ArgumentException("La carpeta se solapa con una raíz registrada.");
        if (existing.Any(f => f.Id == folder.Id && !LibraryPath.Comparer.Equals(f.Path, folder.Path)))
            throw new ArgumentException("Cambiar la ruta de una raíz requiere un flujo de migración explícito.");
        using var cmd = Command(c, tx, """
            INSERT INTO music_folders(id,path,path_key,enabled) VALUES($id,$path,$key,$enabled)
            ON CONFLICT(id) DO UPDATE SET enabled=excluded.enabled
            """);
        cmd.Parameters.AddWithValue("$id", folder.Id.ToString()); cmd.Parameters.AddWithValue("$path", folder.Path);
        cmd.Parameters.AddWithValue("$key", LibraryPath.Key(folder.Path)); cmd.Parameters.AddWithValue("$enabled", folder.Enabled ? 1 : 0);
        await cmd.ExecuteNonQueryAsync(cancellationToken); await BumpAsync(c, tx, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested(); tx.Commit();
    }
    public async Task<IReadOnlyList<MusicFolder>> GetFoldersAsync(CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(cancellationToken); return await ReadFoldersAsync(c, null, cancellationToken);
    }
    private static async Task<List<MusicFolder>> ReadFoldersAsync(SqliteConnection c, SqliteTransaction? tx, CancellationToken token)
    {
        using var cmd = Command(c, tx, "SELECT id,path,enabled FROM music_folders ORDER BY path");
        using var reader = await cmd.ExecuteReaderAsync(token); var result = new List<MusicFolder>();
        while (await reader.ReadAsync(token)) result.Add(new(Guid.Parse(reader.GetString(0)), reader.GetString(1)) { Enabled = reader.GetInt32(2) != 0 });
        return result;
    }
    private static SqliteCommand TrackCommand(SqliteConnection c, SqliteTransaction tx)
    {
        var cmd = Command(c, tx, """
            INSERT INTO tracks(id,folder_id,path,path_key,content_hash,payload) VALUES($id,$folder,$path,$key,$hash,$payload)
            ON CONFLICT(id) DO UPDATE SET folder_id=excluded.folder_id,path=excluded.path,path_key=excluded.path_key,content_hash=excluded.content_hash,payload=excluded.payload
            """);
        foreach (var parameter in new[] { "$id", "$folder", "$path", "$key", "$hash", "$payload" }) cmd.Parameters.Add(parameter, SqliteType.Text);
        return cmd;
    }
    private static async Task WriteTrackAsync(SqliteCommand cmd, Track track, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        cmd.Parameters["$id"].Value = track.Id.ToString(); cmd.Parameters["$folder"].Value = track.FolderId.ToString();
        cmd.Parameters["$path"].Value = track.Path; cmd.Parameters["$key"].Value = LibraryPath.Key(track.Path);
        cmd.Parameters["$hash"].Value = (object?)track.Fingerprint.ContentHash ?? DBNull.Value;
        cmd.Parameters["$payload"].Value = JsonSerializer.Serialize(track); await cmd.ExecuteNonQueryAsync(token);
    }
    public async Task SaveTrackAsync(Track track, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(cancellationToken); using var tx = c.BeginTransaction(); using var cmd = TrackCommand(c, tx);
        await WriteTrackAsync(cmd, track, cancellationToken); await BumpAsync(c, tx, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested(); tx.Commit();
    }
    public async Task<IReadOnlyList<Track>> GetTracksAsync(Guid folderId, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(cancellationToken); return await ReadTracksAsync(c, null, folderId, cancellationToken);
    }
    private static async Task<List<Track>> ReadTracksAsync(SqliteConnection c, SqliteTransaction? tx, Guid? folderId, CancellationToken token)
    {
        using var cmd = Command(c, tx, "SELECT payload FROM tracks" + (folderId.HasValue ? " WHERE folder_id=$folder" : "") + " ORDER BY path");
        if (folderId.HasValue) cmd.Parameters.AddWithValue("$folder", folderId.Value.ToString());
        using var reader = await cmd.ExecuteReaderAsync(token); var result = new List<Track>();
        while (await reader.ReadAsync(token)) result.Add(JsonSerializer.Deserialize<Track>(reader.GetString(0)) ?? throw new InvalidDataException("Invalid track payload."));
        return result;
    }
    public async Task<LibrarySnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(cancellationToken); using var tx = c.BeginTransaction(deferred: true);
        var revision = await RevisionAsync(c, tx, cancellationToken);
        var folders = await ReadFoldersAsync(c, tx, cancellationToken); var tracks = await ReadTracksAsync(c, tx, null, cancellationToken);
        tx.Commit(); return new(revision, folders.AsReadOnly(), tracks.AsReadOnly());
    }
    public async Task CommitScanAsync(long expectedRevision, IReadOnlyList<Track> tracks, ScanSession session,
        ScanResult result, IReadOnlyList<ScanEntry> entries, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(cancellationToken); using var tx = c.BeginTransaction();
        if (await RevisionAsync(c, tx, cancellationToken) != expectedRevision) throw new StaleScanPreviewException("La biblioteca cambió antes de confirmar.");
        using var trackCmd = TrackCommand(c, tx);
        foreach (var track in tracks) await WriteTrackAsync(trackCmd, track, cancellationToken);
        using var sessionCmd = Command(c, tx, "INSERT INTO scan_sessions(id,started_utc,payload,result) VALUES($id,$started,$payload,$result)");
        sessionCmd.Parameters.AddWithValue("$id", session.Id.ToString()); sessionCmd.Parameters.AddWithValue("$started", session.StartedUtc.ToString("O"));
        sessionCmd.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(session)); sessionCmd.Parameters.AddWithValue("$result", JsonSerializer.Serialize(result));
        await sessionCmd.ExecuteNonQueryAsync(cancellationToken);
        using var entryCmd = Command(c, tx, "INSERT INTO scan_entries(session_id,ordinal,payload) VALUES($session,$ordinal,$payload)");
        entryCmd.Parameters.AddWithValue("$session", session.Id.ToString()); entryCmd.Parameters.Add("$ordinal", SqliteType.Integer); entryCmd.Parameters.Add("$payload", SqliteType.Text);
        for (var i = 0; i < entries.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested(); entryCmd.Parameters["$ordinal"].Value = i;
            entryCmd.Parameters["$payload"].Value = JsonSerializer.Serialize(entries[i]); await entryCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        await BumpAsync(c, tx, cancellationToken); cancellationToken.ThrowIfCancellationRequested(); tx.Commit();
    }
    public async Task<IReadOnlyList<ScanSession>> GetScanHistoryAsync(CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(cancellationToken); using var cmd = Command(c, null, "SELECT payload FROM scan_sessions ORDER BY started_utc DESC");
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken); var result = new List<ScanSession>();
        while (await reader.ReadAsync(cancellationToken)) result.Add(JsonSerializer.Deserialize<ScanSession>(reader.GetString(0))!);
        return result;
    }
    public async Task<ScanResult?> GetScanResultAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(cancellationToken); using var cmd = Command(c, null, "SELECT result FROM scan_sessions WHERE id=$id");
        cmd.Parameters.AddWithValue("$id", sessionId.ToString()); var payload = await cmd.ExecuteScalarAsync(cancellationToken);
        return payload is string json ? JsonSerializer.Deserialize<ScanResult>(json) : null;
    }
    public async Task<IReadOnlyList<ScanEntry>> GetScanEntriesAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using var c = await OpenAsync(cancellationToken); using var cmd = Command(c, null, "SELECT payload FROM scan_entries WHERE session_id=$id ORDER BY ordinal");
        cmd.Parameters.AddWithValue("$id", sessionId.ToString()); using var reader = await cmd.ExecuteReaderAsync(cancellationToken); var result = new List<ScanEntry>();
        while (await reader.ReadAsync(cancellationToken)) result.Add(JsonSerializer.Deserialize<ScanEntry>(reader.GetString(0))!);
        return result;
    }
}
