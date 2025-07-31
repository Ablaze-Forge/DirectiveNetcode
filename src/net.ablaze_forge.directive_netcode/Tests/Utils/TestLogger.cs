using System;
using System.Collections.Generic;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.Tests.Utils
{
    /// <summary>
    /// Provides a test implementation of <see cref="ILogger"/> for capturing and managing log messages within a testing environment. This logger allows for verification of logged output during unit tests.
    /// </summary>
    public class TestLogger : ILogger
    {
        private LoggerEnvironment m_Environment;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestLogger"/> class.
        /// A default logging environment is opened upon instantiation.
        /// </summary>
        public TestLogger(bool debugLogEnvironment = false)
        {
            _ = OpenEnvironment(debugLogEnvironment);
        }

        /// <summary>
        /// Gets the log handler associated with this logger. In this implementation, the logger itself acts as its own log handler.
        /// </summary>
        public ILogHandler logHandler
        {
            get => this;
            set { }
        }

        /// <summary>
        /// Gets or sets a value indicating whether logging is enabled.
        /// Logging is enabled by default.
        /// </summary>
        public bool logEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the filter log type. Log messages with a severity lower than this type will not be processed.
        /// By default, all log types are allowed.
        /// </summary>
        public LogType filterLogType { get; set; } = LogType.Log;

        /// <summary>
        /// Opens a new logging environment and returns it.
        /// The returned <see cref="LoggerEnvironment"/> should be disposed of to register its collected logs.
        /// If an existing environment is open, it will be disposed of before opening a new one.
        /// </summary>
        /// <returns>A new <see cref="LoggerEnvironment"/> instance.</returns>
        public LoggerEnvironment OpenEnvironment(bool debugLogEnvironment = false)
        {
            if (!m_Environment?.IsDisposed ?? false)
            {
                m_Environment.Dispose();
            }

            LoggerEnvironment environment = new(debugLogEnvironment);

            m_Environment = environment;

            environment.OnDisposal += ClearEnvironment;

            return environment;
        }

        /// <summary>
        /// Clears the current logging environment when it is disposed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> object that contains no event data.</param>
        private void ClearEnvironment(object sender, EventArgs e)
        {
            m_Environment.OnDisposal -= ClearEnvironment;

            m_Environment = null;
        }

        /// <summary>
        /// Determines whether the current logging environment meets the specified criteria for log, error, exception, and warning counts.
        /// </summary>
        /// <param name="logCount">The expected number of general log messages.</param>
        /// <param name="errorCount">The expected number of error messages.</param>
        /// <param name="exceptionCount">The expected number of exception messages.</param>
        /// <param name="warningCount">The expected number of warning messages.</param>
        /// <returns><see langword="true"/> if all counts match the environment's current counts; otherwise, <see langword="false"/>.</returns>
        public bool MeetsCriteria(int logCount, int errorCount, int exceptionCount, int warningCount)
        {
            return m_Environment.LogCount == logCount &&
                   m_Environment.ErrorCount == errorCount &&
                   m_Environment.ExceptionCount == exceptionCount &&
                   m_Environment.WarningCount == warningCount;
        }

        /// <summary>
        /// Checks if any log message in the current environment contains the specified text.
        /// </summary>
        /// <param name="textToSearch">The text to search for within log messages.</param>
        /// <returns><see langword="true"/> if any log message contains the text; otherwise, <see langword="false"/>.</returns>
        public bool HasLogContaining(string textToSearch)
        {
            return m_Environment.Logs.Exists(log => log.Contains(textToSearch));
        }

        /// <summary>
        /// Checks if any error message in the current environment contains the specified text.
        /// </summary>
        /// <param name="textToSearch">The text to search for within error messages.</param>
        /// <returns><see langword="true"/> if any error message contains the text; otherwise, <see langword="false"/>.</returns>
        public bool HasErrorContaining(string textToSearch)
        {
            return m_Environment.Errors.Exists(error => error.Contains(textToSearch));
        }

        /// <summary>
        /// Checks if any exception message in the current environment contains the specified text.
        /// </summary>
        /// <param name="textToSearch">The text to search for within exception messages.</param>
        /// <returns><see langword="true"/> if any exception message contains the text; otherwise, <see langword="false"/>.</returns>
        public bool HasExceptionContaining(string textToSearch)
        {
            return m_Environment.Exceptions.Exists(exception => exception.Contains(textToSearch));
        }

        /// <summary>
        /// Checks if any warning message in the current environment contains the specified text.
        /// </summary>
        /// <param name="textToSearch">The text to search for within warning messages.</param>
        /// <returns><see langword="true"/> if any warning message contains the text; otherwise, <see langword="false"/>.</returns>
        public bool HasWarningContaining(string textToSearch)
        {
            return m_Environment.Warnings.Exists(warning => warning.Contains(textToSearch));
        }

        /// <summary>
        /// Determines whether a given <see cref="LogType"/> is allowed based on the current <see cref="filterLogType"/> and <see cref="logEnabled"/> status.
        /// </summary>
        /// <param name="logType">The type of log message to check.</param>
        /// <returns><see langword="true"/> if the log type is allowed; otherwise, <see langword="false"/>.</returns>
        public bool IsLogTypeAllowed(LogType logType)
        {
            if (!logEnabled)
            {
                return false;
            }

            return logType < filterLogType;
        }

        /// <summary>
        /// Logs a message with a specified log type.
        /// </summary>
        /// <param name="logType">The type of log message.</param>
        /// <param name="message">The message to log.</param>
        public void Log(LogType logType, object message)
        {
            HandleLog(logType, message, null);
        }

        /// <summary>
        /// Logs a message with a specified log type and an associated Unity context object.
        /// </summary>
        /// <param name="logType">The type of log message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="context">The Unity object associated with this log message.</param>
        public void Log(LogType logType, object message, UnityEngine.Object context)
        {
            HandleLog(logType, message, context);
        }

        /// <summary>
        /// Logs a message with a specified log type and a tag.
        /// </summary>
        /// <param name="logType">The type of log message.</param>
        /// <param name="tag">The tag associated with the log message.</param>
        /// <param name="message">The message to log.</param>
        public void Log(LogType logType, string tag, object message)
        {
            HandleLog(logType, $"{tag}: {message}", null);
        }

        /// <summary>
        /// Logs a message with a specified log type, tag, and an associated Unity context object.
        /// </summary>
        /// <param name="logType">The type of log message.</param>
        /// <param name="tag">The tag associated with the log message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="context">The Unity object associated with this log message.</param>
        public void Log(LogType logType, string tag, object message, UnityEngine.Object context)
        {
            HandleLog(logType, $"{tag}: {message}", context);
        }

        /// <summary>
        /// Logs a general message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Log(object message)
        {
            HandleLog(LogType.Log, message, null);
        }

        /// <summary>
        /// Logs a general message with a specified tag.
        /// </summary>
        /// <param name="tag">The tag associated with the log message.</param>
        /// <param name="message">The message to log.</param>
        public void Log(string tag, object message)
        {
            HandleLog(LogType.Log, $"{tag}: {message}", null);
        }

        /// <summary>
        /// Logs a general message with a specified tag and an associated Unity context object.
        /// </summary>
        /// <param name="tag">The tag associated with the log message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="context">The Unity object associated with this log message.</param>
        public void Log(string tag, object message, UnityEngine.Object context)
        {
            HandleLog(LogType.Log, $"{tag}: {message}", context);
        }

        /// <summary>
        /// Logs a warning message with a specified tag.
        /// </summary>
        /// <param name="tag">The tag associated with the warning message.</param>
        /// <param name="message">The message to log as a warning.</param>
        public void LogWarning(string tag, object message)
        {
            HandleLog(LogType.Warning, $"{tag}: {message}", null);
        }

        /// <summary>
        /// Logs a warning message with a specified tag and an associated Unity context object.
        /// </summary>
        /// <param name="tag">The tag associated with the warning message.</param>
        /// <param name="message">The message to log as a warning.</param>
        /// <param name="context">The Unity object associated with this warning message.</param>
        public void LogWarning(string tag, object message, UnityEngine.Object context)
        {
            HandleLog(LogType.Warning, $"{tag}: {message}", context);
        }

        /// <summary>
        /// Logs an error message with a specified tag.
        /// </summary>
        /// <param name="tag">The tag associated with the error message.</param>
        /// <param name="message">The message to log as an error.</param>
        public void LogError(string tag, object message)
        {
            HandleLog(LogType.Error, $"{tag}: {message}", null);
        }

        /// <summary>
        /// Logs an error message with a specified tag and an associated Unity context object.
        /// </summary>
        /// <param name="tag">The tag associated with the error message.</param>
        /// <param name="message">The message to log as an error.</param>
        /// <param name="context">The Unity object associated with this error message.</param>
        public void LogError(string tag, object message, UnityEngine.Object context)
        {
            HandleLog(LogType.Error, $"{tag}: {message}", context);
        }

        /// <summary>
        /// Logs a formatted message with a specified log type.
        /// </summary>
        /// <param name="logType">The type of log message.</param>
        /// <param name="format">The format string for the message.</param>
        /// <param name="args">An array of objects to format.</param>
        public void LogFormat(LogType logType, string format, params object[] args)
        {
            HandleLog(logType, string.Format(format, args), null);
        }

        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        public void LogException(Exception exception)
        {
            HandleLog(LogType.Exception, exception.ToString(), null);
        }

        /// <summary>
        /// Logs a formatted message with a specified log type and an associated Unity context object.
        /// </summary>
        /// <param name="logType">The type of log message.</param>
        /// <param name="context">The Unity object associated with this log message.</param>
        /// <param name="format">The format string for the message.</param>
        /// <param name="args">An array of objects to format.</param>
        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            HandleLog(logType, string.Format(format, args), context);
        }

        /// <summary>
        /// Logs an exception with an associated Unity context object.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="context">The Unity object associated with this exception.</param>
        public void LogException(Exception exception, UnityEngine.Object context)
        {
            HandleLog(LogType.Exception, exception.ToString(), context);
        }

        /// <summary>
        /// Handles the logging of a message by categorizing it and adding it to the current environment's respective log list if the log type is allowed.
        /// </summary>
        /// <param name="type">The type of log message.</param>
        /// <param name="message">The message content.</param>
        /// <param name="context">The Unity object context (can be null).</param>
        private void HandleLog(LogType type, object message, UnityEngine.Object context)
        {
            if (!IsLogTypeAllowed(type))
            {
                return;
            }

            string logMessage = message.ToString();
            string formattedMessage = "";

            switch (type)
            {
                case LogType.Log:
                    formattedMessage = $"[LOG] {logMessage}";
                    m_Environment.Logs.Add(formattedMessage);
                    break;
                case LogType.Error:
                    formattedMessage = $"[ERR] {logMessage}";
                    m_Environment.Errors.Add(formattedMessage);
                    break;
                case LogType.Exception:
                    formattedMessage = $"[EXC] {logMessage}";
                    m_Environment.Exceptions.Add(formattedMessage);
                    break;
                case LogType.Warning:
                    formattedMessage = $"[WARN] {logMessage}";
                    m_Environment.Warnings.Add(formattedMessage);
                    break;
                case LogType.Assert:
                    formattedMessage = $"[ASRT] {logMessage}";
                    m_Environment.Errors.Add(formattedMessage);
                    break;
            }
        }

        /// <summary>
        /// Represents a logging environment that collects logs, errors, exceptions, and warnings.
        /// This class implements <see cref="IDisposable"/> to ensure collected logs are registered with Unity's Debug.Log when the environment is no longer needed.
        /// </summary>
        public class LoggerEnvironment : IDisposable
        {
            internal EventHandler OnDisposal;

            private bool m_DebugLogOnDisposal;

            /// <summary>
            /// Gets a value indicating whether this environment has been disposed.
            /// </summary>
            public bool IsDisposed = false;

            /// <summary>
            /// Gets a list of general log messages collected in this environment.
            /// </summary>
            public List<string> Logs { get; } = new();
            /// <summary>
            /// Gets a list of error messages collected in this environment.
            /// </summary>
            public List<string> Errors { get; } = new();
            /// <summary>
            /// Gets a list of exception messages collected in this environment.
            /// </summary>
            public List<string> Exceptions { get; } = new();
            /// <summary>
            /// Gets a list of warning messages collected in this environment.
            /// </summary>
            public List<string> Warnings { get; } = new();

            /// <summary>
            /// Gets the total count of general log messages.
            /// </summary>
            public int LogCount => Logs.Count;
            /// <summary>
            /// Gets the total count of error messages.
            /// </summary>
            public int ErrorCount => Errors.Count;
            /// <summary>
            /// Gets the total count of exception messages.
            /// </summary>
            public int ExceptionCount => Exceptions.Count;
            /// <summary>
            /// Gets the total count of warning messages.
            /// </summary>
            public int WarningCount => Warnings.Count;

            internal LoggerEnvironment(bool debugLogEnvironment)
            {
                m_DebugLogOnDisposal = debugLogEnvironment;
            }

            /// <summary>
            /// Registers all collected logs (general logs, errors, exceptions, and warnings) with Unity's <see cref="Debug.Log"/>.
            /// This method is automatically called when the <see cref="LoggerEnvironment"/> instance is disposed.
            /// </summary>
            public void RegisterLogs()
            {
                var logList = new List<string>();
                logList.AddRange(Logs);
                logList.AddRange(Errors);
                logList.AddRange(Exceptions);
                logList.AddRange(Warnings);

                foreach (var log in logList)
                {
                    Debug.Log(log);
                }
            }

            /// <summary>
            /// Disposes of the logging environment, which triggers the registration of all collected logs if <see cref="m_DebugLogOnDisposal"/> is <c>true</c>.
            /// After disposal, the <see cref="IsDisposed"/> flag is set to <see langword="true"/> and the
            /// <see cref="OnDisposal"/> event is invoked.
            /// </summary>
            public void Dispose()
            {
                if (m_DebugLogOnDisposal)
                {
                    RegisterLogs();
                }

                IsDisposed = true;
                OnDisposal?.Invoke(this, null);
            }
        }
    }
}