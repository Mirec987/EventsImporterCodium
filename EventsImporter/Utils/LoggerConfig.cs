using Serilog;

namespace EventImporter.Utils
{
    public static class LoggerConfig
    {
        public static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                //.WriteTo.Console()
                .WriteTo.Async(a => a.File(Config.LogPath, rollingInterval: RollingInterval.Day))
                .CreateLogger();
        }
    }
}
