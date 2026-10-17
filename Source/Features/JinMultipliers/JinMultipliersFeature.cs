using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using NineSolsQoL.Core;

namespace NineSolsQoL.Features.JinMultipliers;

// Multipliers for the Jin the player earns.
//
// - Coins: each coin calls PlayerGamePlayData.AddGold(value, tag) once when it is picked up. Coins
//   from enemies, chests and breakable objects arrive tagged GoldSourceTag.Monster, or Chest for a
//   coin that was already in the scene: LootSpawner sets the tag, a [NonSerialized] field, on the
//   coin prefabs, which pooled copies do not inherit.
// - Transmute Unto Wealth (a skill): AddGold(10, GoldSourceTag.FooExplode) for each enemy killed by
//   a talisman explosion (Player.FooExplodeKillMonster).
// - Selling, e.g. recycling at the 3D printer: a shop trade of type 販賣 ("sell") takes the item
//   (mainMaterial) and adds OutcomeResult.ModifyAmount to the gold field (MerchandiseData.Trade ->
//   Receive), without AddGold. Spending never goes through Receive.
// The Jin of the death bag (GoldSourceTag.DeathPenalty) is never multiplied.
internal static class JinMultipliersFeature {
    private const int MaxMultiplier = 1000;

    // Gold is stored as a float and read back as an int: stay well inside the int range
    private const long MaxGold = 2_000_000_000;

    public static ConfigEntry<bool> Enabled = null!;
    public static ConfigEntry<int> MonsterDrops = null!;
    public static ConfigEntry<int> TransmuteUntoWealth = null!;
    public static ConfigEntry<int> Selling = null!;

    // Coins multiplied since the mod loaded, shown by the F9 dump: a log line per coin would flood
    // the log
    private static int coinsMultiplied;
    private static long extraJinFromCoins;

    public static void Init(ConfigFile config) {
        var section = new ConfigSection(config, "JinMultipliers");
        var range = new AcceptableValueRange<int>(1, MaxMultiplier);
        Enabled = section.Bind("Enabled", true,
            "Multiply the Jin you earn by the multipliers below. Off: the game's amounts");
        MonsterDrops = section.Bind("MonsterDrops", 1,
            "Multiplies the Jin of every coin you pick up, dropped by enemies, chests and breakable " +
            "objects (the game counts them all as enemy drops). Vanilla: 1. The death bag is never " +
            "multiplied", range);
        TransmuteUntoWealth = section.Bind("TransmuteUntoWealth", 1,
            "Multiplies the 10 Jin that the Transmute Unto Wealth skill gives for each enemy killed by a " +
            "talisman explosion. Vanilla: 1", range);
        Selling = section.Bind("Selling", 1,
            "Multiplies the Jin you receive when you sell an item, such as recycling at the 3D printer. The " +
            "shop shows the multiplied amount. Prices you pay are unchanged. Vanilla: 1", range);

        DebugTools.AddDumpSection(Describe);
    }

    // Before PlayerGamePlayData.AddGold: the amount to add instead. Runs inside the coin pickup and
    // the talisman kill, so it must not throw.
    public static int MultiplyEarning(PlayerGamePlayData data, int amount, GoldSourceTag source) {
        try {
            // In boss memory mode AddGold throws the Jin away anyway
            if (!Enabled.Value || amount <= 0 || ApplicationCore.IsInBossMemoryMode) return amount;

            var multiplier = source switch {
                GoldSourceTag.Monster or GoldSourceTag.Chest => MonsterDrops.Value,
                GoldSourceTag.FooExplode => TransmuteUntoWealth.Value,
                _ => 1,
            };
            if (multiplier <= 1) return amount;

            var total = Multiply(amount, multiplier, data.CurrentGold);
            if (source != GoldSourceTag.FooExplode) {
                coinsMultiplied++;
                extraJinFromCoins += total - amount;
            } else {
                Log.Debug($"[jin] Transmute Unto Wealth: {amount} x {multiplier} = {total} Jin");
            }
            return total;
        } catch (Exception e) {
            Log.Error($"JinMultipliers failed, adding the game's amount: {e}");
            return amount;
        }
    }

    // After MerchandiseData.Trade, whose last step added OutcomeResult.ModifyAmount to
    // OutcomeResult.ModifyFlag. For a sale paid in Jin, adds the rest of the multiplied amount
    // through the same setter, so the game's checks on the gold field (its maximum) apply as they do
    // to the vanilla amount. Runs inside the shop's trade, so it must not throw.
    public static void AfterTrade(MerchandiseData merchandise) {
        try {
            if (!Enabled.Value) return;
            var outcome = merchandise.OutcomeResult;
            if (outcome == null || outcome.ModifyAmount <= 0 || !IsGold(outcome.ModifyFlag)) return;
            // A sale gives an item away; a trade that only pays Jin is a reward, not a sale
            if (merchandise.merchandiseType != MerchandiseData.MerchandiseType.販賣 || merchandise.mainMaterial == null) {
                Log.Debug($"[jin] '{merchandise.name}' paid {outcome.ModifyAmount} Jin but is not a sale " +
                          $"({merchandise.merchandiseType}, item to sell " +
                          $"{(merchandise.mainMaterial != null ? merchandise.mainMaterial.name : "none")}): not multiplied");
                return;
            }

            var multiplier = Selling.Value;
            if (multiplier <= 1) return;
            var total = SaleAmount(outcome.ModifyAmount);
            outcome.ModifyFlag.CurrentValue += total - outcome.ModifyAmount;
            Log.Debug($"[jin] sold '{merchandise.name}': {outcome.ModifyAmount} x {multiplier} = {total} Jin, " +
                      $"gold now {outcome.ModifyFlag.CurrentValue}");
        } catch (Exception e) {
            Log.Error($"JinMultipliers (selling) failed, the game's amount was added: {e}");
        }
    }

    // After MerchandiseItemButton.UpdateView. In the branch where the game writes a sale's price (a
    // sale, with the item to sell in the inventory), shows the multiplied amount instead.
    public static void AfterShopRowUpdate(MerchandiseItemButton row) {
        try {
            if (!Enabled.Value || Selling.Value <= 1) return;
            var merchandise = row.bindData;
            if (merchandise == null || row.priceText == null || merchandise.mainMaterial == null ||
                merchandise.merchandiseType != MerchandiseData.MerchandiseType.販賣 || !row.HasEnoughMaterial) {
                return;
            }

            var outcome = merchandise.OutcomeResult;
            if (outcome == null || outcome.ModifyAmount <= 0 || !IsGold(outcome.ModifyFlag)) return;
            row.priceText.text = SaleAmount(outcome.ModifyAmount).ToString();
        } catch (Exception e) {
            Log.Error($"JinMultipliers shop price failed: {e}");
        }
    }

    private static int SaleAmount(int amount) =>
        (int)Math.Min((long)amount * Selling.Value, MaxGold);

    // amount * multiplier, kept small enough that the player's gold stays under MaxGold
    private static int Multiply(int amount, int multiplier, int gold) {
        var room = MaxGold - Math.Max(gold, 0);
        return (int)Math.Max(amount, Math.Min((long)amount * multiplier, room));
    }

    // The player's Jin, as opposed to another currency a shop may trade in
    private static bool IsGold(ScriptableDataFloat? flag) {
        if (flag == null || !GameCore.IsAvailable()) return false;
        var data = GameCore.Instance.playerGameData;
        return data != null && ReferenceEquals(flag, data.goldField);
    }

    // The cap is the gold field's MaxValue: AddGold ignores it, but every shop trade clamps the
    // gold to it and moves the excess to ExternalRepository (lost when there is none)
    private static IEnumerable<string> Describe(GameCore core) {
        var data = core.playerGameData;
        var gold = data.goldField;
        var repository = gold.ExternalRepository;
        var overflow = repository != null
            ? $"'{repository.name}' holding {repository.CurrentValue}"
            : "none (the excess is lost)";
        return new[] {
            $"JinMultipliers: enabled={Enabled.Value} multipliers monster={MonsterDrops.Value} " +
            $"transmute={TransmuteUntoWealth.Value} sell={Selling.Value}",
            $"JinMultipliers: gold={data.CurrentGold} cap={gold.MaxValue} overflowTo={overflow} " +
            $"coinsMultiplied={coinsMultiplied} extraJinFromCoins={extraJinFromCoins}",
        };
    }
}
