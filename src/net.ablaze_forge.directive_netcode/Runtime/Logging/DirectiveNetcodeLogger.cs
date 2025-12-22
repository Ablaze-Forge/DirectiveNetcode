using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.Logging
{
    /// <summary>
    /// Defines the interface for logging errors, warnings, and general messages within the DirectiveNetcode system.
    /// </summary>
    public interface IDirectiveNetcodeLogger
    {
        /// <summary>
        /// Logs an error message using the default log tag.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="error">The specific <see cref="ErrorCodes"/>.</param>
        /// <param name="message">The message or object to log.</param>
        void LogError(UnityEngine.Object context, ErrorCodes error, object message);

        /// <summary>
        /// Logs an error message with a specific custom tag.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="tag">A custom tag string used to categorize the log message.</param>
        /// <param name="error">The specific <see cref="ErrorCodes"/>.</param>
        /// <param name="message">The message or object to log.</param>
        void LogError(UnityEngine.Object context, string tag, ErrorCodes error, object message);

        /// <summary>
        /// Logs an error message without a <see cref="UnityEngine.Object"/> context.
        /// <param name="tag">A custom tag string used to categorize the log message.</param>
        /// <param name="error">The specific <see cref="ErrorCodes"/>.</param>
        /// <param name="message">The message or object to log.</param>
        void LogError(string tag, ErrorCodes error, object message);

        /// <summary>
        /// Logs a warning message using the default log tag.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="warning">The specific <see cref="WarningCodes"/>.</param>
        /// <param name="message">The message or object to log.</param>
        void LogWarning(UnityEngine.Object context, WarningCodes warning, object message);

        /// <summary>
        /// Logs a warning message with a specific custom tag.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="tag">A custom tag string used to categorize the log message.</param>
        /// <param name="warning">The specific <see cref="WarningCodes"/>.</param>
        /// <param name="message">The message or object to log.</param>
        void LogWarning(UnityEngine.Object context, string tag, WarningCodes warning, object message);

        /// <summary>
        /// Logs a warning message without a <see cref="UnityEngine.Object"/> context.
        /// <param name="tag">A custom tag string used to categorize the log message.</param>
        /// <param name="warning">The specific <see cref="WarningCodes"/>.</param>
        /// <param name="message">The message or object to log.</param>
        void LogWarning(string tag, WarningCodes warning, object message);

        /// <summary>
        /// Logs a general message. This method is conditional and may be stripped from Release builds.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="message">The message or object to log.</param>
        void Log(UnityEngine.Object context, object message);

        /// <summary>
        /// Logs a general message with a specific custom tag. This method is conditional and may be stripped from Release builds.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="tag">A custom tag string used to categorize the log message.</param>
        /// <param name="message">The message or object to log.</param>
        void Log(UnityEngine.Object context, string tag, object message);

        /// <summary>
        /// Logs a general message without a <see cref="UnityEngine.Object"/> context. This method is conditional and may be stripped from Release builds.
        /// <param name="tag">A custom tag string used to categorize the log message.</param>
        /// <param name="message">The message or object to log.</param>
        void Log(string tag, object message);

        /// <summary>
        /// Logs a critical, unconditional message that will always be included regardless of build configuration.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="message">The message or object to log.</param>
        void LogAlways(UnityEngine.Object context, object message);

        /// <summary>
        /// Logs a critical, unconditional message with a specific tag that will always be included regardless of build configuration.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="tag">A custom tag string used to categorize the log message.</param>
        /// <param name="message">The message or object to log.</param>
        void LogAlways(UnityEngine.Object context, string tag, object message);

        /// <summary>
        /// Logs a critical, unconditional message without a <see cref="UnityEngine.Object"/> context that will always be included regardless of build configuration.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        void LogAlways(string tag, object message);
    }

    /// <summary>
    /// An implementation of <see cref="IDirectiveNetcodeLogger"/> that wraps an existing <see cref="ILogger"/> (like Unity's internal logger) to provide standardized, structured logging for DirectiveNetcode, including error/warning codes and conditional logging.
    /// </summary>
    public sealed class DirectiveNetcodeLogger : IDirectiveNetcodeLogger
    {
        private readonly ILogger m_Logger;
        private const string DefaultTag = "DIRECTIVE_NETCODE";

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectiveNetcodeLogger"/> class.
        /// </summary>
        /// <param name="logger">The underlying <see cref="ILogger"/> implementation to use. If null, it defaults to <see cref="Debug.unityLogger"/>.</param>
        public DirectiveNetcodeLogger(ILogger logger)
        {
            m_Logger = logger ?? UnityEngine.Debug.unityLogger;
        }

        /// <summary>
        /// Logs an error message using the default log tag.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="error">The specific <see cref="ErrorCodes"/>.</param>
        /// <param name="message">The message or object to log.</param>
        public void LogError(UnityEngine.Object context, ErrorCodes error, object message)
            => LogError(context, DefaultTag, error, message);

        /// <summary>
        /// Logs an error message with a specific custom tag without a <see cref="UnityEngine.Object"/> context.
        /// </summary>
        /// <param name="tag">A custom tag string used to categorize the log message.</param>
        /// <param name="error">The specific <see cref="ErrorCodes"/>.</param>
        /// <param name="message">The message or object to log.</param>
        public void LogError(string tag, ErrorCodes error, object message)
        {
            string formattedMessage = Format(LogType.Error, tag, (int)error, message);
            m_Logger.LogError(tag, formattedMessage);
        }

        /// <summary>
        /// Logs an error message with a specific custom tag.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="tag">A custom tag string used to categorize the log message.</param>
        /// <param name="error">The specific <see cref="ErrorCodes"/>.</param>
        /// <param name="message">The message or object to log.</param>
        public void LogError(UnityEngine.Object context, string tag, ErrorCodes error, object message)
        {
            string formattedMessage = Format(LogType.Error, tag, (int)error, message);
            m_Logger.LogError(tag, formattedMessage, context);
        }

        /// <summary>
        /// Logs a warning message using the default log tag.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="warning">The specific <see cref="WarningCodes"/>.</param>
        /// <param name="message">The message or object to log.</param>
        public void LogWarning(UnityEngine.Object context, WarningCodes warning, object message)
            => LogWarning(context, DefaultTag, warning, message);

        /// <summary>
        /// Logs a warning message with a specific custom tag without a <see cref="UnityEngine.Object"/> context.
        /// </summary>
        /// <param name="tag">A custom tag string used to categorize the log message.</param>
        /// <param name="warning">The specific <see cref="WarningCodes"/>.</param>
        /// <param name="message">The message or object to log.</param>
        public void LogWarning(string tag, WarningCodes warning, object message)
        {
            string formattedMessage = Format(LogType.Warning, tag, (int)warning, message);
            m_Logger.LogWarning(tag, formattedMessage);
        }

        /// <summary>
        /// Logs a warning message with a specific custom tag.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="tag">A custom tag string used to categorize the log message.</param>
        /// <param name="warning">The specific <see cref="WarningCodes"/>.</param>
        /// <param name="message">The message or object to log.</param>
        public void LogWarning(UnityEngine.Object context, string tag, WarningCodes warning, object message)
        {
            string formattedMessage = Format(LogType.Warning, tag, (int)warning, message);
            m_Logger.LogWarning(tag, formattedMessage, context);
        }

        /// <summary>
        /// Logs a general message using the default tag. This method is conditional and may be stripped from Release builds.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="message">The message or object to log.</param>
        public void Log(UnityEngine.Object context, object message)
            => Log(context, DefaultTag, message);

        /// <summary>
        /// Logs a general message with a specific custom tag with a null <see cref="UnityEngine.Object"/> context. This method is conditional and may be stripped from Release builds.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="tag">A custom tag string used to categorize the log message.</param>
        /// <param name="message">The message or object to log.</param>
        public void Log(string tag, object message)
            => Log(null, tag, message);

        /// <summary>
        /// Logs a general message with a specific custom tag. This method is conditional and may be stripped from Release builds.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="tag">A custom tag string used to categorize the log message.</param>
        /// <param name="message">The message or object to log.</param>
        public void Log(UnityEngine.Object context, string tag, object message)
            => ConditionalLog(context, tag, message);

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        /// <summary>
        /// Performs the actual logging of a general message. This method is conditionally compiled and only runs in UNITY_EDITOR or DEVELOPMENT_BUILD configurations.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message.</param>
        /// <param name="tag">The tag string used for the log message.</param>
        /// <param name="message">The message or object to log.</param>
        private void ConditionalLog(UnityEngine.Object context, string tag, object message)
        {
            string formattedMessage = Format(LogType.Log, tag, 0, message);
            m_Logger.Log(tag, formattedMessage, context);
        }

        /// <summary>
        /// Logs a critical, unconditional message using the default tag.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="message">The message or object to log.</param>
        public void LogAlways(UnityEngine.Object context, object message)
            => LogAlways(context, DefaultTag, message);

        /// <summary>
        /// Logs a critical, unconditional message with a specific tag and without a <see cref="UnityEngine.Object"/> context.
        /// </summary>
        /// <param name="tag">A custom tag string used to categorize the log message.</param>
        /// <param name="message">The message or object to log.</param>
        public void LogAlways(string tag, object message)
        {
            string formattedMessage = Format(LogType.Log, tag, 0, message);
            m_Logger.Log(tag, formattedMessage);
        }

        /// <summary>
        /// Logs a critical, unconditional message with a specific tag.
        /// </summary>
        /// <param name="context">The <see cref="UnityEngine.Object"/> associated with the message, often used for highlighting in the Unity console.</param>
        /// <param name="tag">A custom tag string used to categorize the log message.</param>
        /// <param name="message">The message or object to log.</param>
        public void LogAlways(UnityEngine.Object context, string tag, object message)
        {
            string formattedMessage = Format(LogType.Log, tag, 0, message);
            m_Logger.Log(tag, formattedMessage, context);
        }

        /// <summary>
        /// Formats the log message, conditionally adding caller information only for undefined/unknown codes.
        /// </summary>
        /// <param name="level">The type/severity of the log message (<see cref="LogType"/>).</param>
        /// <param name="tag">The tag used for the log message.</param>
        /// <param name="errorCode">The numeric error or warning code. Use 0 for general logs.</param>
        /// <param name="message">The message or object content to log.</param>
        /// <param name="sourceFilePath">Automatically populated path to the source file where the method was called.</param>
        /// <param name="sourceLineNumber">Automatically populated line number in the source file where the method was called.</param>
        /// <param name="memberName">Automatically populated member name where the method was called.</param>
        /// <returns>A formatted string ready for output to the underlying logger.</returns>
        private string Format(
            LogType level,
            string tag,
            int errorCode,
            object message,
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0,
            [CallerMemberName] string memberName = ""
        )
        {
            string timeStamp = DateTimeOffset.Now.ToString("HH:mm:ss.fff");

            string levelName = level switch
            {
                LogType.Exception => "EXCEPTION",
                LogType.Warning => "WARNING",
                LogType.Assert => "ASSERT",
                LogType.Error => "ERROR",
                LogType.Log => "LOG",
                _ => "LOG"
            };

            string codePrefix = errorCode != 0
                ? $"[{tag}] [{levelName}-{errorCode}]"
                : $"[{tag}] [{levelName}]";

            string sourceInfo = string.Empty;

#pragma warning disable CS0618 // Ignore obsolete warning, since the warning was made to simplify incorrect enum usage warnings.
            bool isGenericError =
                (level == LogType.Error && errorCode == (int)ErrorCodes.Undefined) ||
                (level == LogType.Warning && errorCode == (int)WarningCodes.Undefined);
#pragma warning restore CS0618

            if (isGenericError)
            {
                sourceInfo = $" (Source: {System.IO.Path.GetFileName(sourceFilePath)}:{sourceLineNumber} in {memberName})";
            }

            return $"[{timeStamp}] {codePrefix}: {message}{sourceInfo}";
        }
    }
}
