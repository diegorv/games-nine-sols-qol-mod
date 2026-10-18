using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using NineSolsQoL.Core;

namespace NineSolsQoL.Features.NoDeathPenalty;

// Keep Jin and experience when Yi dies.
//
// Vanilla, a death that has a penalty (not in memory mode, VR challenges or with the DeathPanaltyImmune
// ability) first fills the death bag with the Jin and experience Yi carries
// (PlayerDeadRecord.StorePlayerDataBeforeDead). Then, when Yi is revived or the next scene loads,
// PlayerGamePlayData.PlayerDeathPenalty sets both to 0, counts the death, saves and clears the pending
// penalty. NoDeathPenaltyTranspiler makes the penalty skip only the two lines that set Jin and experience
// to 0, and NoDeathPenaltyPatches empties the bag the game just filled: it still appears where it would
// (picking it up still triggers whatever reacts to it) but gives nothing.
//
// Both halves follow one decision taken at death. The penalty runs seconds later (on maps with the game's
// own quick revive, only at the next scene load), and a change in the config menu in between must not
// leave a full bag together with the kept Jin, or an empty bag together with zeroed Jin.
internal static class NoDeathPenaltyFeature {
    public static ConfigEntry<bool> Enabled = null!;

    // Set by the transpiler when it found the penalty's two lines
    public static bool Patched;

    // Whether the last death keeps its Jin and experience: written at every death that has a penalty,
    // read by that penalty. A hot reload in between resets it, and the penalty then applies as in vanilla
    // while the bag is already empty (dev only).
    private static bool keepAtPenalty;

    public static void Init(ConfigFile config) {
        var section = new ConfigSection(config, "NoDeathPenalty");
        Enabled = section.Bind("Enabled", true,
            "Keep your Jin and experience when you die. The death bag still appears where you died, but empty: " +
            "you can still pick it up, for the skills that react to that. Off: the game's penalty, where the Jin " +
            "and experience you carry go into the death bag, and dying again before you pick it up loses them");

        DebugTools.AddDumpSection(Describe);
    }

    // Before PlayerDeadRecord.StorePlayerDataBeforeDead: whether it will write a new bag. Its first line
    // returns when CanStoreDeadRecord fails, and then the record still holds an older death's bag, which
    // must not be emptied. Must not throw.
    public static bool WillStoreBag(PlayerDeadRecord record) {
        try {
            return record.CanStoreDeadRecord.IsValid;
        } catch (Exception e) {
            Log.Error($"NoDeathPenalty could not read CanStoreDeadRecord: {e}");
            return false;
        }
    }

    // After StorePlayerDataBeforeDead, which the game calls only for a death that has a penalty: takes this
    // death's decision and, when Jin and experience are kept, empties the bag the game just filled. Runs
    // inside the game's death handling, so it must not throw.
    public static void AfterBagStored(PlayerDeadRecord record, bool bagStored) {
        keepAtPenalty = false;
        try {
            if (!Enabled.Value || !Patched) {
                Log.Debug($"[death] the game's penalty applies (NoDeathPenalty enabled={Enabled.Value} " +
                          $"patched={Patched})");
                return;
            }

            // Set before emptying the bag: should that fail, a full bag and the kept Jin are the better outcome
            keepAtPenalty = true;
            if (bagStored) {
                record.ContainMoney.CurrentValue = 0f;
                record.ContainEXP.CurrentValue = 0f;
            }
            Log.Debug($"[death] Jin and experience will be kept: " +
                      (bagStored ? "the death bag is empty" : "the game stored no new death bag"));
        } catch (Exception e) {
            Log.Error($"NoDeathPenalty failed at death: {e}");
        }
    }

    // Called by PlayerDeathPenalty in place of its "CurrentGold = 0"
    public static void SetGoldAtPenalty(PlayerGamePlayData data, int value) {
        if (!keepAtPenalty) data.CurrentGold = value;
    }

    // Called by PlayerDeathPenalty in place of its "CurrentExp = 0", right after the gold
    public static void SetExpAtPenalty(PlayerGamePlayData data, int value) {
        if (!keepAtPenalty) {
            data.CurrentExp = value;
            return;
        }
        Log.Debug($"[death] the death penalty kept {data.CurrentGold} Jin and {data.CurrentExp} experience");
    }

    // The F9 dump already shows the carried Jin and the bag's Jin (DebugTools.DescribeDeathBag)
    private static IEnumerable<string> Describe(GameCore core) {
        return new[] {
            $"NoDeathPenalty: enabled={Enabled.Value} patched={Patched} lastDeathKeeps={keepAtPenalty} " +
            $"penaltyPending={core.player.IsNeedDeathPenalty} exp={core.playerGameData.CurrentExp} " +
            $"bagExp={core.deadRecord.ContainEXP.CurrentValue}",
        };
    }
}
