using System;

namespace NineSolsQoL.Core;

// Safe access to the game state.
//
// SingletonBehaviour<T>.Instance creates a new, empty object when no instance exists (title
// screen, loading), so GameCore.Instance must never be read before GameCore.IsAvailable().
internal static class GameAccess {
    private static readonly Predicate<MonsterBase> IsTaggedBoss = monster => monster.CompareTag("Boss");

    // The GameCore while a level is loaded and the player exists, otherwise null
    public static GameCore? CoreInLevel() {
        if (!GameCore.IsAvailable()) return null;
        var core = GameCore.Instance;
        if (core == null || core.gameLevel == null || core.player == null) return null;
        return core;
    }

    // A boss health bar is shown, or an engaged monster is tagged "Boss" (the check the game itself
    // uses in BossEngagingCondition). The game clears both as soon as the player dies.
    public static bool IsBossFightActive(GameCore core) {
        if (core.monsterHpUI != null && core.monsterHpUI.CurrentBossHP != null) return true;
        return MonsterManager.IsAvailable() && MonsterManager.Instance.QueryEngagingMonsters(IsTaggedBoss);
    }
}
