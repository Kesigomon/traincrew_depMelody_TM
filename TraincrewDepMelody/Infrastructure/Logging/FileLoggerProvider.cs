using System.IO;
using Microsoft.Extensions.Logging;

namespace TraincrewDepMelody.Infrastructure.Logging
{
    /// <summary>
    /// ファイルロガープロバイダー
    /// </summary>
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logDirectory = "log";
        private readonly string _logFileName;

        public FileLoggerProvider()
        {
            _logFileName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";

            // ログディレクトリが存在しない場合は作成
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, Path.Combine(_logDirectory, _logFileName));
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// ファイルロガー
    /// </summary>
    internal class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();

        public FileLogger(string categoryName, string logFilePath)
        {
            _categoryName = categoryName;
            _logFilePath = logFilePath;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Information;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var level = logLevel.ToString().ToUpper();
                var message = formatter(state, exception);
                var logMessage = $"[{timestamp}] [{level}] [{_categoryName}] {message}";

                if (exception != null)
                {
                    logMessage += $"\n{exception}";
                }

                lock (_lockObject)
                {
                    File.AppendAllText(_logFilePath, logMessage + "\n");
                }
            }
            catch
            {
                // ログ出力に失敗しても例外を投げない
            }
        }
    }
}
