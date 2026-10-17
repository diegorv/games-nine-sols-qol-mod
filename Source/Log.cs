using BepInEx.Logging;
using NineSolsQoL.Core;

namespace NineSolsQoL;

internal static class Log {
    private static ManualLogSource? source;

    public static void Init(ManualLogSource logSource) {
        source = logSource;
    }

    public static void Info(object data) => source?.LogInfo(data);

    public static void Warning(object data) => source?.LogWarning(data);

    public static void Error(object data) => source?.LogError(data);

    // Diagnostics, only when DebugLogging is on. Written at Info level because BepInEx's default
    // disk log filters out the Debug level.
    public static void Debug(object data) {
        if (DebugTools.DebugLogging?.Value == true) source?.LogInfo(data);
    }
}
