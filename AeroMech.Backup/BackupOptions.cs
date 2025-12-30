namespace AeroMech.Backup
{
    public sealed class BackupOptions
    {
        public string ConnectionString { get; set; } = "";
        public string DatabaseName { get; set; } = "";
        public string SqlServerBackupPath { get; set; } = "";
        public string OneDriveTargetFolder { get; set; } = "";
        public int RetentionDays { get; set; } = 14;
        public int RunAtHour { get; set; } = 2;
        public int RunAtMinute { get; set; } = 0;
        public bool BackupAtStartup { get; set; } = false;
    }
}
