namespace EventImporter
{
    public static class Config
    {
        // Path to the source file
        public static readonly string FilePath = "FilePathPlaceholder";

        // Connection string for the database
        public static readonly string ConnectionString = "ConnectionStringPlaceholder";

        // Path to log files
        public static readonly string LogPath = "LogPathPlaceholder";

        // Batch size for event processing
        public static readonly int BatchSize = 100;

        // Maximum delay for API call simulation
        public static readonly int MaxApiDelay = 10000;
    }
}
