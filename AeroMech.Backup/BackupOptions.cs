namespace AeroMech.Backup
{
    public sealed class BackupOptions
    {
        public string ConnectionString { get; set; } = "";
        public string DatabaseName { get; set; } = "";
        public string SqlServerBackupPath { get; set; } = "";

        public string LocalBackupFolder { get; set; } = "";
        public string BackupFileExtension { get; set; } = "dump";
        public string PgDumpPath { get; set; } = "";
        public string PgRestorePath { get; set; } = "";
        public bool VerifyBackup { get; set; } = true;
        public int CompressionLevel { get; set; } = 9;

        public string OneDriveTargetFolder { get; set; } = "";
        public int RetentionDays { get; set; } = 14;
        public int RunAtHour { get; set; } = 2;
        public int RunAtMinute { get; set; } = 0;
        public bool BackupAtStartup { get; set; } = false;
    }
}
