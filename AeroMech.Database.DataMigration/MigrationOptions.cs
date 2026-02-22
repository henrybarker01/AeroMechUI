namespace AeroMech.Database.DataMigration
{
    public class MigrationOptions
    {
        public int BatchSize { get; set; } = 1000;
        public bool SkipIdentityTables { get; set; } = true;
    }
}
