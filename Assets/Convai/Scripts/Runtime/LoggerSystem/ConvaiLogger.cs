using System;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Convai.Scripts.Runtime.LoggerSystem
{
    public static class ConvaiLogger
    {
        #region LogCategory enum

        public enum LogCategory
        {
            Character,
            LipSync,
            Actions
        }

        #endregion

        private static readonly Dictionary<LogLevel, string> LevelColors = new()
        {
            { LogLevel.Debug, "cyan" },
            { LogLevel.Info, "grey" },
            { LogLevel.Warning, "yellow" },
            { LogLevel.Error, "red" },
            { LogLevel.Exception, "orange" }
            //List of available colours at: https://docs.unity3d.com/Manual/StyledText.html
        };

        private static bool ShouldLog(LogLevel level, LogCategory category)
        {
            // Check log level and log category at the same time
            return (level, category) switch
            {
                (LogLevel.Debug, LogCategory.Character) => LoggerConfig.CharacterLogDebug,
                (LogLevel.Info, LogCategory.Character) => LoggerConfig.CharacterLogInfo,
                (LogLevel.Warning, LogCategory.Character) => LoggerConfig.CharacterLogWarning,
                (LogLevel.Error, LogCategory.Character) => LoggerConfig.CharacterLogError,
                (LogLevel.Exception, LogCategory.Character) => LoggerConfig.CharacterLogException,

                (LogLevel.Debug, LogCategory.LipSync) => LoggerConfig.LipSyncLogDebug,
                (LogLevel.Info, LogCategory.LipSync) => LoggerConfig.LipSyncLogInfo,
                (LogLevel.Warning, LogCategory.LipSync) => LoggerConfig.LipSyncLogWarning,
                (LogLevel.Error, LogCategory.LipSync) => LoggerConfig.LipSyncLogError,
                (LogLevel.Exception, LogCategory.LipSync) => LoggerConfig.LipSyncLogException,

                (LogLevel.Debug, LogCategory.Actions) => LoggerConfig.ActionsLogDebug,
                (LogLevel.Info, LogCategory.Actions) => LoggerConfig.ActionsLogInfo,
                (LogLevel.Warning, LogCategory.Actions) => LoggerConfig.ActionsLogWarning,
                (LogLevel.Error, LogCategory.Actions) => LoggerConfig.ActionsLogError,
                (LogLevel.Exception, LogCategory.Actions) => LoggerConfig.ActionsLogException,

                _ => false
            };
        }

        private static bool IsStringJson(string message)
        {
            string trimmedMessage = message.Trim();
            return trimmedMessage.StartsWith("{") || trimmedMessage.StartsWith("[");
        } // ReSharper disable Unity.PerformanceAnalysis
        private static void LogMessage(string message, LogLevel level, LogCategory category, params object[] args)
        {
            if (!ShouldLog(level, category)) return;

            string formattedMessage = IsStringJson(message) ? $"{message}" : string.Format(message, args);

            string logMessage =
                $"[{Enum.GetName(typeof(LogLevel), level)}][{Enum.GetName(typeof(LogCategory), category)}]: {formattedMessage}";

            if (LevelColors.TryGetValue(level, out string color) && color != "default")
                logMessage = $"<color={color}>{logMessage}</color>";

            // Select the first frame that is from the non-ConvaiLogger type.
            StackTrace trace = new(2, true);
            for (int i = 0; i < trace.FrameCount; i++)
            {
                StackFrame frame = trace.GetFrame(i);
                if (frame.GetMethod().ReflectedType != typeof(ConvaiLogger))
                {
                    logMessage +=
                        $"\n[Stack Trace - Method: {frame.GetMethod().Name}, at Line: {frame.GetFileLineNumber()} in File: {frame.GetFileName()}]";
                    break;
                }
            }

            switch (level)
            {
                case LogLevel.Debug:
                    Debug.Log(logMessage);
                    break;
                case LogLevel.Info:
                    Debug.Log(logMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                case LogLevel.Error:
                    Debug.LogError(logMessage);
                    break;
                case LogLevel.Exception:
                    Debug.LogError(logMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        public static void Info(object message, LogCategory category)
        {
            Info(message.ToString(), category);
        }

        public static void DebugLog(object message, LogCategory category)
        {
            DebugLog(message.ToString(), category);
        }

        public static void Warn(object message, LogCategory category)
        {
            Warn(message.ToString(), category);
        }

        public static void Error(object message, LogCategory category)
        {
            Error(message.ToString(), category);
        }

        public static void Info(string message, LogCategory category, params object[] args)
        {
            LogMessage(message, LogLevel.Info, category, args);
        }

        public static void DebugLog(string message, LogCategory category, params object[] args)
        {
            LogMessage(message, LogLevel.Debug, category, args);
        }

        public static void Warn(string message, LogCategory category, params object[] args)
        {
            LogMessage(message, LogLevel.Warning, category, args);
        }

        public static void Error(string message, LogCategory category, params object[] args)
        {
            LogMessage(message, LogLevel.Error, category, args);
        }

        public static void Exception(string message, LogCategory category, params object[] args)
        {
            LogMessage(message, LogLevel.Exception, category, args);
        }

        public static void Exception(Exception ex, LogCategory category)
        {
            string message = $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            Exception(message, category);
        }

        private enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error,
            Exception
        }
    }
}