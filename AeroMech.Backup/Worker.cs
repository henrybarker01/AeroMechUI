using Microsoft.Extensions.Options;

namespace AeroMech.Backup
{
    public class Worker(
        ILogger<Worker> logger,
        IDatabaseBackup databaseBackup,
        IOptions<BackupOptions> options) : BackgroundService
    {
        private readonly BackupOptions _options = options.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("SqlBackupToOneDriveWorker starting...");

            if (_options.BackupAtStartup)
            {
                logger.LogInformation("BackupAtStartup enabled - running immediately.");
                await databaseBackup.ExecuteBackupAsync(stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTimeOffset.Now;
                var nextRun = GetNextRunTime(now, _options.RunAtHour, _options.RunAtMinute);
                var delay = nextRun - now;

                logger.LogInformation("Next run at {NextRun} (in {Delay}).", nextRun, delay);

                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);

                await databaseBackup.ExecuteBackupAsync(stoppingToken);
            }
        }

        private static DateTimeOffset GetNextRunTime(DateTimeOffset now, int hour, int minute)
        {
            var candidate = new DateTimeOffset(now.Year, now.Month, now.Day, hour, minute, 0, now.Offset);
            return candidate <= now ? candidate.AddDays(1) : candidate;
        }
    }
}
