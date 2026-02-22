using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AeroMech.Backup
{
    public sealed class DatabaseBackup : IDatabaseBackup
    {
        private readonly ILogger<DatabaseBackup> _logger;
        private readonly BackupOptions _options;

        public DatabaseBackup(ILogger<DatabaseBackup> logger, IOptions<BackupOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async Task ExecuteBackupAsync(CancellationToken cancellationToken)
        {
            try
            {
                var dbName = ResolveDatabaseName();
                var ext = NormalizeExtension(_options.BackupFileExtension);

                var backupFileName = $"{dbName}_{DateTimeOffset.UtcNow:yyyy-MM-dd_HHmmss}.{ext}";
                var localBackupFolder = ResolveLocalBackupFolder();
                Directory.CreateDirectory(localBackupFolder);

                var localFullPath = Path.Combine(localBackupFolder, backupFileName);

                await BackupDatabaseAsync(localFullPath, dbName, cancellationToken);

                if (_options.VerifyBackup)
                    await VerifyBackupAsync(localFullPath, cancellationToken);

                if (!string.IsNullOrWhiteSpace(_options.OneDriveTargetFolder))
                    await CopyToOneDriveAsync(localFullPath, backupFileName, cancellationToken);

                CleanupOldFiles(localBackupFolder, $"{dbName}_*.{ext}", _options.RetentionDays);

                if (!string.IsNullOrWhiteSpace(_options.OneDriveTargetFolder))
                    CleanupOldFiles(_options.OneDriveTargetFolder, $"{dbName}_*.{ext}", _options.RetentionDays);

                _logger.LogInformation("Backup run finished successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup run FAILED.");
            }
        }

        private async Task BackupDatabaseAsync(string outputPath, string dbName, CancellationToken ct)
        {
            _logger.LogInformation("Starting PostgreSQL backup of {Db} to {Path}", dbName, outputPath);

            var builder = new NpgsqlConnectionStringBuilder(_options.ConnectionString);

            var pgDumpExe = ResolvePgToolPath(_options.PgDumpPath, "pg_dump", "pg_dump.exe");
            var args = BuildPgDumpArguments(builder, dbName, outputPath);

            try
            {
                await RunProcessAsync(
                    exePath: pgDumpExe,
                    arguments: args,
                    environment: env =>
                    {
                        if (!string.IsNullOrWhiteSpace(builder.Password) && !env.ContainsKey("PGPASSWORD"))
                            env["PGPASSWORD"] = builder.Password;
                    },
                    ct);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
            {
                throw new InvalidOperationException(
                    "pg_dump was not found. Install PostgreSQL client tools or configure Backup:PgDumpPath to the full path of pg_dump.exe. " +
                    "Note: Windows Services often do not inherit your user PATH.",
                    ex);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
            {
                throw new InvalidOperationException(
                    "Failed to start pg_dump (Access denied). If you configured Backup:PgDumpPath, ensure it points to the executable (e.g. ...\\bin\\pg_dump.exe) " +
                    "and that the service account has permission to execute it.",
                    ex);
            }

            _logger.LogInformation("Backup completed: {Path}", outputPath);
        }

        private async Task VerifyBackupAsync(string outputPath, CancellationToken ct)
        {
            var pgRestoreExe = ResolvePgToolPath(_options.PgRestorePath, "pg_restore", "pg_restore.exe");

            try
            {
                _logger.LogInformation("Verifying backup (pg_restore --list): {Path}", outputPath);

                await RunProcessAsync(
                    exePath: pgRestoreExe,
                    arguments: new[] { "--list", outputPath },
                    environment: null,
                    ct);

                _logger.LogInformation("Backup verified successfully.");
            }
            catch (Win32Exception ex)
            {
                _logger.LogWarning(ex, "pg_restore was not found. Skipping verification.");
            }
        }

        private string ResolvePgToolPath(string? configuredPath, string toolName, string windowsExeName)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                configuredPath = configuredPath.Trim().Trim('"');

                if (Directory.Exists(configuredPath))
                {
                    var exe = OperatingSystem.IsWindows() ? windowsExeName : toolName;

                    var candidate1 = Path.Combine(configuredPath, exe);
                    if (File.Exists(candidate1))
                        return candidate1;

                    var candidate2 = Path.Combine(configuredPath, "bin", exe);
                    if (File.Exists(candidate2))
                        return candidate2;
                }

                return configuredPath;
            }

            if (!OperatingSystem.IsWindows())
                return toolName;

            // On Windows (especially when running as a service), PATH may not include PostgreSQL tools.
            // Try common installation locations.
            var candidates = FindPostgresToolCandidates(windowsExeName);
            var resolved = candidates.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                _logger.LogInformation("Resolved {Tool} to {Path}", toolName, resolved);
                return resolved;
            }

            return toolName;
        }

        private static IEnumerable<string> FindPostgresToolCandidates(string exeName)
        {
            static IEnumerable<string> FromBase(string baseDir, string exe)
            {
                if (!Directory.Exists(baseDir))
                    yield break;

                // Typical layout: C:\Program Files\PostgreSQL\16\bin\pg_dump.exe
                foreach (var versionDir in Directory.EnumerateDirectories(baseDir))
                {
                    var candidate = Path.Combine(versionDir, "bin", exe);
                    if (File.Exists(candidate))
                        yield return candidate;
                }
            }

            foreach (var p in FromBase(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL"), exeName))
                yield return p;

            foreach (var p in FromBase(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "PostgreSQL"), exeName))
                yield return p;
        }

        private async Task CopyToOneDriveAsync(string sourcePath, string backupFileName, CancellationToken ct)
        {
            Directory.CreateDirectory(_options.OneDriveTargetFolder);

            await WaitForFileStableAsync(sourcePath, ct);

            var finalTargetPath = Path.Combine(_options.OneDriveTargetFolder, backupFileName);
            var tempTargetPath = finalTargetPath + ".tmp";

            _logger.LogInformation("Copying {Source} -> {Target}", sourcePath, finalTargetPath);

            File.Copy(sourcePath, tempTargetPath, overwrite: true);

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
                    throw new FileNotFoundException("Backup file not found.", path);

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

        private static string NormalizeExtension(string extension)
        {
            extension = extension.Trim();
            if (extension.StartsWith('.'))
                extension = extension[1..];

            return string.IsNullOrWhiteSpace(extension) ? "dump" : extension;
        }

        private string ResolveLocalBackupFolder()
        {
            if (!string.IsNullOrWhiteSpace(_options.LocalBackupFolder))
                return _options.LocalBackupFolder;

            if (!string.IsNullOrWhiteSpace(_options.SqlServerBackupPath))
                return _options.SqlServerBackupPath;

            throw new InvalidOperationException("Backup output folder not configured. Set Backup:LocalBackupFolder (or legacy Backup:SqlServerBackupPath).");
        }

        private string ResolveDatabaseName()
        {
            if (!string.IsNullOrWhiteSpace(_options.DatabaseName))
                return _options.DatabaseName;

            var builder = new NpgsqlConnectionStringBuilder(_options.ConnectionString);
            if (!string.IsNullOrWhiteSpace(builder.Database))
                return builder.Database;

            throw new InvalidOperationException("Database name not configured. Set Backup:DatabaseName or include Database in the connection string.");
        }

        private IReadOnlyList<string> BuildPgDumpArguments(NpgsqlConnectionStringBuilder builder, string dbName, string outputPath)
        {
            var args = new List<string>
            {
                "--format=custom",
                "--no-owner",
                "--no-acl",
                $"--file={outputPath}",
            };

            if (_options.CompressionLevel is >= 0 and <= 9)
                args.Add($"--compress={_options.CompressionLevel}");

            if (!string.IsNullOrWhiteSpace(builder.Host))
                args.Add($"--host={builder.Host}");

            if (builder.Port != 0)
                args.Add($"--port={builder.Port}");

            if (!string.IsNullOrWhiteSpace(builder.Username))
                args.Add($"--username={builder.Username}");

            args.Add($"--dbname={dbName}");

            return args;
        }

        private async Task RunProcessAsync(
            string exePath,
            IReadOnlyList<string> arguments,
            Action<IDictionary<string, string?>>? environment,
            CancellationToken ct)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            foreach (var arg in arguments)
                startInfo.ArgumentList.Add(arg);

            environment?.Invoke(startInfo.Environment);

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            try
            {
                if (!process.Start())
                    throw new InvalidOperationException($"Failed to start process '{exePath}'.");
            }
            catch (Win32Exception)
            {
                throw;
            }

            var stdOutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stdErrTask = process.StandardError.ReadToEndAsync(ct);

            try
            {
                await process.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                TryKillProcess(process);
                throw;
            }

            var stdout = await stdOutTask;
            var stderr = await stdErrTask;

            if (!string.IsNullOrWhiteSpace(stdout))
                _logger.LogDebug("{Exe} stdout: {Stdout}", Path.GetFileName(exePath), stdout.Trim());

            if (!string.IsNullOrWhiteSpace(stderr))
                _logger.LogDebug("{Exe} stderr: {Stderr}", Path.GetFileName(exePath), stderr.Trim());

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"'{exePath}' exited with code {process.ExitCode}. stderr: {stderr}");
        }

        private static void TryKillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignore
            }
        }
    }
}
