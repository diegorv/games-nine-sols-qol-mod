using System.Linq;
using BepInEx.Configuration;
using NineSolsQoL.Core;

namespace NineSolsQoL.Features.Respawn;

// Respawn near where you died instead of at the last Root Node: on the last safe ground for
// ordinary deaths, and just outside the arena (after reloading the room) for boss deaths.
internal static class RespawnFeature {
    public static ConfigEntry<bool> Enabled = null!;
    public static ConfigEntry<bool> BossRespawnOutsideArena = null!;

    public static void Init(ConfigFile config) {
        Enabled = config.Bind("Respawn", "Enabled", true,
            "Respawn near where you died instead of at the last Root Node. Off: the game's normal respawn " +
            "for every death");
        BossRespawnOutsideArena = config.Bind("Respawn", "BossRespawnOutsideArena", true,
            "After dying in a boss fight, reload the room so the fight starts over and respawn just " +
            "outside the arena. Off: the game's normal respawn after boss deaths");

        DebugTools.AddDumpSection(core => new[] { SafeTrail.Describe() }.Concat(ArenaTracker.Describe(core)));
    }
}
