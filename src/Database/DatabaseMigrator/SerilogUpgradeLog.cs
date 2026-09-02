using DbUp.Engine.Output;
using Serilog;

namespace DatabaseMigrator
{
    internal class SerilogUpgradeLog : IUpgradeLog
    {
        private readonly ILogger _logger;

        public SerilogUpgradeLog(ILogger logger)
        {
            _logger = logger;
        }

        public void LogTrace(string format, params object[] args)
        {
            _logger.Verbose(format, args);
        }

        public void LogDebug(string format, params object[] args)
        {
            _logger.Debug(format, args);
        }

        public void LogInformation(string format, params object[] args)
        {
            _logger.Information(format, args);
        }

        public void LogWarning(string format, params object[] args)
        {
            _logger.Warning(format, args);
        }

        public void LogError(string format, params object[] args)
        {
            _logger.Error(format, args);
        }

        public void LogError(Exception exception, string format, params object[] args)
        {
            _logger.Error(exception, format, args);
        }
    }
}
