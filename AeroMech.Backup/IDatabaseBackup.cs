namespace AeroMech.Backup
{
    public interface IDatabaseBackup
    {
        Task ExecuteBackupAsync(CancellationToken stoppingToken);
    }
}
