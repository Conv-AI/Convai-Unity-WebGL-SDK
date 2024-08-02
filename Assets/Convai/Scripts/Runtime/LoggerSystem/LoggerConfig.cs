using UnityEngine;

namespace Convai.Scripts.Runtime.LoggerSystem
{
    public static class LoggerConfig
    {
        private static LoggerSettings _settings;

        private static LoggerSettings Settings
        {
            get
            {
                if (_settings == null) _settings = Resources.Load<LoggerSettings>("Settings/LoggerSettings");

                return _settings;
            }
        }

        // LipSync logging levels
        public static bool LipSyncLogDebug => Settings.lipSyncDebug;
        public static bool LipSyncLogError => Settings.lipSyncError;
        public static bool LipSyncLogException => Settings.lipSyncException;
        public static bool LipSyncLogInfo => Settings.lipSyncInfo;
        public static bool LipSyncLogWarning => Settings.lipSyncWarning;

        // CharacterResponse logging levels
        public static bool CharacterLogDebug => Settings.characterResponseDebug;
        public static bool CharacterLogError => Settings.characterResponseError;
        public static bool CharacterLogException => Settings.characterResponseException;
        public static bool CharacterLogInfo => Settings.characterResponseInfo;
        public static bool CharacterLogWarning => Settings.characterResponseWarning;

        // Actions logging levels
        public static bool ActionsLogDebug => Settings.actionsDebug;
        public static bool ActionsLogError => Settings.actionsError;
        public static bool ActionsLogException => Settings.actionsException;
        public static bool ActionsLogInfo => Settings.actionsInfo;
        public static bool ActionsLogWarning => Settings.actionsWarning;
    }
}