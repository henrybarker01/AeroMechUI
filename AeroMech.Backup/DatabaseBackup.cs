using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AeroMech.Backup
{
    public sealed class DatabaseBackup : IDatabaseBackup
    {
        private readonly ILogger<Worker> _logger;
        private readonly BackupOptions _options;

        public DatabaseBackup(ILogger<Worker> logger, IOptions<BackupOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async Task ExecuteBackupAsync(CancellationToken cancellationToken)
        {
            try
            {
                var backupFileName = $"{_options.DatabaseName}_{DateTimeOffset.Now:yyyy-MM-dd_HHmmss}.bak";
                var sqlServerFullPath = Path.Combine(_options.SqlServerBackupPath, backupFileName);

                await BackupDatabaseAsync(sqlServerFullPath, cancellationToken);
                await VerifyBackupAsync(sqlServerFullPath, cancellationToken);

                var uncSourcePath = Path.Combine(_options.SqlServerBackupPath, backupFileName);
                await CopyToOneDriveAsync(uncSourcePath, backupFileName, cancellationToken);

                CleanupOldFiles(_options.SqlServerBackupPath, $"{_options.DatabaseName}_*.bak", _options.RetentionDays);
                CleanupOldFiles(_options.OneDriveTargetFolder, $"{_options.DatabaseName}_*.bak", _options.RetentionDays);

                _logger.LogInformation("Backup run finished successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup run FAILED.");
            }
        }

        private async Task BackupDatabaseAsync(string sqlServerBackupPath, CancellationToken ct)
        {
            _logger.LogInformation("Starting backup of {Db} to {Path}", _options.DatabaseName, sqlServerBackupPath);

            await using var conn = new SqlConnection(_options.ConnectionString);
            await conn.OpenAsync(ct);

            var isExpress = await IsExpressEditionAsync(conn, ct);

            var compressionClause = isExpress ? "" : "    COMPRESSION,\n";

            if (isExpress)
                _logger.LogInformation("SQL Server edition detected as Express. Backup compression will be disabled.");
            else
                _logger.LogInformation("SQL Server edition is not Express. Backup compression will be enabled.");

            var sql = $@"
BACKUP DATABASE [{_options.DatabaseName}]
TO DISK = @BackupPath
WITH
    INIT,
{compressionClause}    CHECKSUM,
    STATS = 10;";

            await using var cmd = new SqlCommand(sql, conn)
            {
                CommandTimeout = 60 * 10
            };

            cmd.Parameters.AddWithValue("@BackupPath", sqlServerBackupPath);
            await cmd.ExecuteNonQueryAsync(ct);

            _logger.LogInformation("Backup completed: {Path}", sqlServerBackupPath);
        }

        private async Task<bool> IsExpressEditionAsync(SqlConnection conn, CancellationToken ct)
        {
            const string sql = "SELECT CAST(SERVERPROPERTY('Edition') AS nvarchar(128));";

            await using var cmd = new SqlCommand(sql, conn)
            {
                CommandTimeout = 30
            };

            var editionObj = await cmd.ExecuteScalarAsync(ct);
            var edition = editionObj?.ToString() ?? string.Empty;

            _logger.LogDebug("SQL Server Edition: {Edition}", edition);

            return edition.Contains("Express", StringComparison.OrdinalIgnoreCase);
        }

        private async Task VerifyBackupAsync(string sqlServerBackupPath, CancellationToken ct)
        {
            _logger.LogInformation("Verifying backup: {Path}", sqlServerBackupPath);

            var sql = @"
RESTORE VERIFYONLY
FROM DISK = @BackupPath
WITH CHECKSUM;";

            await using var conn = new SqlConnection(_options.ConnectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn)
            {
                CommandTimeout = 60 * 5
            };

            cmd.Parameters.AddWithValue("@BackupPath", sqlServerBackupPath);
            await cmd.ExecuteNonQueryAsync(ct);

            _logger.LogInformation("Backup verified successfully.");
        }

        private async Task CopyToOneDriveAsync(string sourceUncPath, string backupFileName, CancellationToken ct)
        {
            Directory.CreateDirectory(_options.OneDriveTargetFolder);

            await WaitForFileStableAsync(sourceUncPath, ct);

            var finalTargetPath = Path.Combine(_options.OneDriveTargetFolder, backupFileName);
            var tempTargetPath = finalTargetPath + ".tmp";

            _logger.LogInformation("Copying {Source} -> {Target}", sourceUncPath, finalTargetPath);

            File.Copy(sourceUncPath, tempTargetPath, overwrite: true);

            if (File.Exists(finalTargetPath))
                File.Delete(finalTargetPath);

            File.Move(tempTargetPath, finalTargetPath);

            _logger.LogInformation("Copied to OneDrive folder successfully.");
        }

        private async Task WaitForFileStableAsync(string path, CancellationToken ct)
        {
            const int attempts = 10;
            const int delayMs = 500;

            long? lastSize = null;

            for (var i = 0; i < attempts; i++)
            {
                ct.ThrowIfCancellationRequested();

                if (!File.Exists(path))
                    throw new FileNotFoundException("Backup file not found on UNC share.", path);

                var size = new FileInfo(path).Length;

                if (lastSize.HasValue && size == lastSize.Value)
                {
                    if (CanOpenRead(path))
                        return;
                }

                lastSize = size;
                await Task.Delay(delayMs, ct);
            }

            throw new IOException($"Backup file never stabilized: {path}");
        }

        private static bool CanOpenRead(string path)
        {
            try
            {
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void CleanupOldFiles(string folder, string pattern, int retentionDays)
        {
            try
            {
                if (!Directory.Exists(folder))
                    return;

                var cutoffUtc = DateTime.UtcNow.AddDays(-retentionDays);

                foreach (var file in Directory.EnumerateFiles(folder, pattern))
                {
                    var fi = new FileInfo(file);
                    if (fi.LastWriteTimeUtc < cutoffUtc)
                    {
                        _logger.LogInformation("Deleting old backup: {File}", fi.FullName);
                        fi.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cleanup failed for folder {Folder}", folder);
            }
        }
    }
}
