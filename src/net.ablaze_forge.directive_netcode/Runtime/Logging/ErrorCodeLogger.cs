using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.Logging
{
    /// <summary>
    /// Abstract base class for error code-based logging implementations. Provides a foundation for logging with structured error and warning codes.
    /// </summary>
    public abstract class ErrorCodeLoggerBase
    {
        /// <summary>
        /// The underlying Unity <see cref="ILogger"/> instance used for actual logging operations.
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorCodeLoggerBase"/> class with the specified logger.
        /// </summary>
        /// <param name="logger">The Unity <see cref="ILogger"/> instance to use for logging. If null, defaults to <see cref="Debug.unityLogger"/>.</param>
        public ErrorCodeLoggerBase(ILogger logger)
        {
            Logger = logger ?? Debug.unityLogger;
        }

        /// <summary>
        /// Logs an error message with the specified error code and tag.
        /// </summary>
        /// <param name="tag">The tag to associate with this log entry.</param>
        /// <param name="error">The error code to include in the log message.</param>
        /// <param name="message">The message to log.</param>
        public abstract void LogError(string tag, ErrorCodes error, object message);

        /// <summary>
        /// Logs a warning message with the specified warning code and tag.
        /// </summary>
        /// <param name="tag">The tag to associate with this log entry.</param>
        /// <param name="warning">The warning code to include in the log message.</param>
        /// <param name="message">The message to log.</param>
        public abstract void LogWarning(string tag, WarningCodes warning, object message);

        /// <summary>
        /// Logs a standard message with the specified tag.
        /// </summary>
        /// <param name="tag">The tag to associate with this log entry.</param>
        /// <param name="message">The message to log.</param>
        public abstract void Log(string tag, object message);

        /// <summary>
        /// Logs an error message with the specified error code and tag derived from an object.
        /// </summary>
        /// <param name="tag">The object whose type name will be used as the tag.</param>
        /// <param name="error">The error code to include in the log message.</param>
        /// <param name="message">The message to log.</param>
        public abstract void LogError(object tag, ErrorCodes error, object message);

        /// <summary>
        /// Logs a warning message with the specified warning code and tag derived from an object.
        /// </summary>
        /// <param name="tag">The object whose type name will be used as the tag.</param>
        /// <param name="warning">The warning code to include in the log message.</param>
        /// <param name="message">The message to log.</param>
        public abstract void LogWarning(object tag, WarningCodes warning, object message);

        /// <summary>
        /// Logs a standard message with the tag derived from an object.
        /// </summary>
        /// <param name="tag">The object whose type name will be used as the tag.</param>
        /// <param name="message">The message to log.</param>
        public abstract void Log(object tag, object message);

        /// <summary>
        /// Formats a warning message with its associated warning code.
        /// </summary>
        /// <param name="warning">The warning code to include in the formatted message.</param>
        /// <param name="message">The message to format.</param>
        /// <returns>A formatted string containing the warning code and message.</returns>
        protected string Format(WarningCodes warning, object message)
        {
            return $"[WAR -{(int)warning}-]: {message}";
        }

        /// <summary>
        /// Formats an error message with its associated error code.
        /// </summary>
        /// <param name="error">The error code to include in the formatted message.</param>
        /// <param name="message">The message to format.</param>
        /// <returns>A formatted string containing the error code and message.</returns>
        protected string Format(ErrorCodes error, object message)
        {
            return $"[ERR -{(int)error}-]: {message}";
        }
    }

    /// <summary>
    /// A concrete implementation of <see cref="ErrorCodeLoggerBase"/> that forwards log messages to an underlying Unity <see cref="ILogger"/> without additional processing.
    /// </summary>
    public sealed class ErrorCodeLogger : ErrorCodeLoggerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorCodeLogger"/> class with the specified logger.
        /// </summary>
        /// <param name="logger">The Unity <see cref="ILogger"/> instance to use for logging.</param>
        public ErrorCodeLogger(ILogger logger) : base(logger)
        {
        }

        /// <summary>
        /// Logs an error message with the specified error code and tag.
        /// </summary>
        /// <param name="tag">The tag to associate with this log entry.</param>
        /// <param name="error">The error code to include in the log message.</param>
        /// <param name="message">The message to log.</param>
        public override void LogError(string tag, ErrorCodes error, object message)
        {
            Logger.LogError(tag, Format(error, message));
        }

        /// <summary>
        /// Logs a warning message with the specified warning code and tag.
        /// </summary>
        /// <param name="tag">The tag to associate with this log entry.</param>
        /// <param name="warning">The warning code to include in the log message.</param>
        /// <param name="message">The message to log.</param>
        public override void LogWarning(string tag, WarningCodes warning, object message)
        {
            Logger.LogWarning(tag, Format(warning, message));
        }

        /// <summary>
        /// Logs a standard message with the specified tag.
        /// </summary>
        /// <param name="tag">The tag to associate with this log entry.</param>
        /// <param name="message">The message to log.</param>
        public override void Log(string tag, object message)
        {
            Logger.Log(tag, message);
        }

        /// <summary>
        /// Logs an error message with the specified error code and tag derived from an object.
        /// </summary>
        /// <param name="tag">The object whose type name will be used as the tag.</param>
        /// <param name="error">The error code to include in the log message.</param>
        /// <param name="message">The message to log.</param>
        public override void LogError(object tag, ErrorCodes error, object message)
        {
            LogError(tag.GetType().Name, error, message);
        }

        /// <summary>
        /// Logs a warning message with the specified warning code and tag derived from an object.
        /// </summary>
        /// <param name="tag">The object whose type name will be used as the tag.</param>
        /// <param name="warning">The warning code to include in the log message.</param>
        /// <param name="message">The message to log.</param>
        public override void LogWarning(object tag, WarningCodes warning, object message)
        {
            LogWarning(tag.GetType().Name, warning, message);
        }

        /// <summary>
        /// Logs a standard message with the tag derived from an object.
        /// </summary>
        /// <param name="tag">The object whose type name will be used as the tag.</param>
        /// <param name="message">The message to log.</param>
        public override void Log(object tag, object message)
        {
            Log(tag.GetType().Name, message);
        }
    }

    /// <summary>
    /// An implementation of <see cref="ErrorCodeLoggerBase"/> that tracks the frequency of logged errors and warnings in addition to forwarding them to an underlying Unity <see cref="ILogger"/>.
    /// </summary>
    public sealed class ErrorCodeTracker : ErrorCodeLoggerBase
    {
        private readonly ConcurrentDictionary<ErrorCodes, ushort> m_ErrorCount = new();
        private readonly ConcurrentDictionary<WarningCodes, ushort> m_WarningCount = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorCodeTracker"/> class with the specified logger.
        /// </summary>
        /// <param name="logger">The Unity <see cref="ILogger"/> instance to use for logging.</param>
        public ErrorCodeTracker(ILogger logger) : base(logger)
        {
        }

        /// <summary>
        /// Logs an error message with the specified error code and tag, and increments the error count for that code.
        /// </summary>
        /// <param name="tag">The tag to associate with this log entry.</param>
        /// <param name="error">The error code to include in the log message.</param>
        /// <param name="message">The message to log.</param>
        public override void LogError(string tag, ErrorCodes error, object message)
        {
            m_ErrorCount.AddOrUpdate(error, 1, (key, value) => (ushort)(value + 1));

            Logger.LogError(tag, Format(error, message));
        }

        /// <summary>
        /// Logs a warning message with the specified warning code and tag, and increments the warning count for that code.
        /// </summary>
        /// <param name="tag">The tag to associate with this log entry.</param>
        /// <param name="warning">The warning code to include in the log message.</param>
        /// <param name="message">The message to log.</param>
        public override void LogWarning(string tag, WarningCodes warning, object message)
        {
            m_WarningCount.AddOrUpdate(warning, 1, (key, value) => (ushort)(value + 1));

            Logger.LogWarning(tag, Format(warning, message));
        }

        /// <summary>
        /// Logs a standard message with the specified tag.
        /// </summary>
        /// <param name="tag">The tag to associate with this log entry.</param>
        /// <param name="message">The message to log.</param>
        public override void Log(string tag, object message)
        {
            Logger.Log(tag, message);
        }

        /// <summary>
        /// Logs an error message with the specified error code and tag derived from an object, and increments the error count for that code.
        /// </summary>
        /// <param name="tag">The object whose type name will be used as the tag.</param>
        /// <param name="error">The error code to include in the log message.</param>
        /// <param name="message">The message to log.</param>
        public override void LogError(object tag, ErrorCodes error, object message)
        {
            LogError(tag.GetType().Name, error, message);
        }

        /// <summary>
        /// Logs a warning message with the specified warning code and tag derived from an object, and increments the warning count for that code.
        /// </summary>
        /// <param name="tag">The object whose type name will be used as the tag.</param>
        /// <param name="warning">The warning code to include in the log message.</param>
        /// <param name="message">The message to log.</param>
        public override void LogWarning(object tag, WarningCodes warning, object message)
        {
            LogWarning(tag.GetType().Name, warning, message);
        }

        /// <summary>
        /// Logs a standard message with the tag derived from an object.
        /// </summary>
        /// <param name="tag">The object whose type name will be used as the tag.</param>
        /// <param name="message">The message to log.</param>
        public override void Log(object tag, object message)
        {
            Log(tag.GetType().Name, message);
        }

        /// <summary>
        /// Gets the total count of all logged errors.
        /// </summary>
        /// <returns>The total number of errors logged since this instance was created.</returns>
        public int GetErrorCount()
        {
            if (m_ErrorCount.Count == 0)
            {
                return 0;
            }

            return m_ErrorCount.Values.Sum(v => v);
        }

        /// <summary>
        /// Gets the count of occurrences for a specific error code.
        /// </summary>
        /// <param name="error">The error code to get the count for.</param>
        /// <returns>The number of times the specified error code has been logged.</returns>
        public int GetErrorCount(ErrorCodes error)
        {
            if (m_ErrorCount.TryGetValue(error, out ushort count))
            {
                return count;
            }

            return 0;
        }

        /// <summary>
        /// Gets the total count of all logged warnings.
        /// </summary>
        /// <returns>The total number of warnings logged since this instance was created.</returns>
        public int GetWarningCount()
        {
            if (m_WarningCount.Count == 0)
            {
                return 0;
            }

            return m_WarningCount.Values.Sum(v => v);
        }

        /// <summary>
        /// Gets the count of occurrences for a specific warning code.
        /// </summary>
        /// <param name="warning">The warning code to get the count for.</param>
        /// <returns>The number of times the specified warning code has been logged.</returns>
        public int GetWarningCount(WarningCodes warning)
        {
            if (m_WarningCount.TryGetValue(warning, out ushort count))
            {
                return count;
            }

            return 0;
        }
    }
}
