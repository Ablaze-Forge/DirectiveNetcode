using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;

namespace AblazeForge.DirectiveNetcode.Logging
{
    public abstract class ErrorCodeLoggerBase
    {
        protected readonly ILogger Logger;

        public ErrorCodeLoggerBase(ILogger logger)
        {
            Logger = logger ?? Debug.unityLogger;
        }

        public abstract void LogError(string tag, ErrorCodes error, object message);
        public abstract void LogWarning(string tag, WarningCodes warning, object message);
        public abstract void Log(string tag, object message);
        public abstract void LogError(object tag, ErrorCodes error, object message);
        public abstract void LogWarning(object tag, WarningCodes warning, object message);
        public abstract void Log(object tag, object message);

        protected string Format(WarningCodes warning, object message)
        {
            return $"[WAR -{(int)warning}-]: {message}";
        }

        protected string Format(ErrorCodes error, object message)
        {
            return $"[ERR -{(int)error}-]: {message}";
        }
    }

    public sealed class ErrorCodeLogger : ErrorCodeLoggerBase
    {
        public ErrorCodeLogger(ILogger logger) : base(logger)
        {
        }

        public override void LogError(string tag, ErrorCodes error, object message)
        {
            Logger.LogError(tag, Format(error, message));
        }

        public override void LogWarning(string tag, WarningCodes warning, object message)
        {
            Logger.LogWarning(tag, Format(warning, message));
        }

        public override void Log(string tag, object message)
        {
            Logger.Log(tag, message);
        }

        public override void LogError(object tag, ErrorCodes error, object message)
        {
            LogError(tag.GetType().Name, error, message);
        }

        public override void LogWarning(object tag, WarningCodes warning, object message)
        {
            LogWarning(tag.GetType().Name, warning, message);
        }

        public override void Log(object tag, object message)
        {
            Log(tag.GetType().Name, message);
        }
    }

    public sealed class ErrorCodeTracker : ErrorCodeLoggerBase
    {
        private readonly ConcurrentDictionary<ErrorCodes, ushort> m_ErrorCount = new();
        private readonly ConcurrentDictionary<WarningCodes, ushort> m_WarningCount = new();

        public ErrorCodeTracker(ILogger logger) : base(logger)
        {
        }

        public override void LogError(string tag, ErrorCodes error, object message)
        {
            m_ErrorCount.AddOrUpdate(error, 1, (key, value) => (ushort)(value + 1));

            Logger.LogError(tag, Format(error, message));
        }

        public override void LogWarning(string tag, WarningCodes warning, object message)
        {
            m_WarningCount.AddOrUpdate(warning, 1, (key, value) => (ushort)(value + 1));

            Logger.LogWarning(tag, Format(warning, message));
        }

        public override void Log(string tag, object message)
        {
            Logger.Log(tag, message);
        }

        public override void LogError(object tag, ErrorCodes error, object message)
        {
            LogError(tag.GetType().Name, error, message);
        }

        public override void LogWarning(object tag, WarningCodes warning, object message)
        {
            LogWarning(tag.GetType().Name, warning, message);
        }

        public override void Log(object tag, object message)
        {
            Log(tag.GetType().Name, message);
        }

        public int GetErrorCount()
        {
            if (m_ErrorCount.Count == 0)
            {
                return 0;
            }

            return m_ErrorCount.Values.Sum(v => v);
        }

        public int GetErrorCount(ErrorCodes error)
        {
            if (m_ErrorCount.TryGetValue(error, out ushort count))
            {
                return count;
            }

            return 0;
        }


        public int GetWarningCount()
        {
            if (m_WarningCount.Count == 0)
            {
                return 0;
            }

            return m_WarningCount.Values.Sum(v => v);
        }

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