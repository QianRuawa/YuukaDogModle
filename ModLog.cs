using UnityEngine;

#nullable enable
namespace YuukaDogModle
{
    public static class ModLog
    {
        private const string ModName = "ModName";

        public static void Log(object message) => Debug.Log((object)$"{"yuuka_dog"} Log: {message}");

        public static void Warning(object message)
        {
            Debug.LogWarning((object)$"{"yuuka_dog"} Warning: {message}");
        }

        public static void Error(object message)
        {
            Debug.LogError((object)$"{"yuuka_dog"} Error: {message}");
        }
    }
}
