# How the features work

Every feature here is done and tested in game. "My choice" marks a design decision I took; keep it unless I
change it. The player-facing description is in `thunderstore/README.md`, the code layout in `README.md`.

TrueDamage was **dropped** (it makes the game harder); its report stays in `research\truedamage.md`.

## Abilities

### EarlyAirDash

Config: `Enabled` (default **off**, my choice).

The game has its own air dash (one per air time: `CanRoll` needs `airDodgeCount == 0`, refilled by
`ResetJumpAndDash`), gated by the `RollDodgeInAirUpgrade` ability, unlocked later in the game. A transpiler swaps
the one `RollDodgeInAirUpgrade.IsActivated` read in `Player.DodgeCheck` (IL_0054/IL_0059) for
`AirDashUnlocked(upgrade)` = the game's getter OR (`Enabled` and not `IsAcquired`): only before the upgrade is
acquired, so once it is, the game's own rules (challenge ability packs, unequip, conditions) decide again. With
the Down Dash jade, down dashes are unlimited (the game's own `CanDownDash`). The ability is never written (no
save change; UI and skill tree still see it locked). `[air dash]` Debug line when the mod made the difference.

### FastHeal

Config: `Enabled` (default on).

Heal at the Quick Dose jade's speed without the jade (my choice; no multiplier, no stacking).
`PlayerDrinkPotionState.OnStateEnter` sets `animator.speed = 1.66666f` when its private `isSpeedUpHeal`
(`SpeedUpHealJade.IsActivated`) is true; a transpiler swaps that one call for `SpeedUpHeal(state)` = the jade's
getter OR `Enabled`. The jade the state checks is `(Jade Data) Fast Pipe 速藥玉` (seen in game with F9). `[heal]`
Debug line when the mod made the difference.

## Combat

### PerfectParryWindow

Config: `Enabled`, `PerfectParryFrames` (1-30, default 12).

Transpiler in `PlayerParryState.Parried` replaces the read `spamDatas[spamLevel]` with
`PerfectWindow(windows, level, elapsed)`; every non-zero window × frames/8 (12 → 200/150 ms), zeros (anti-mash)
kept. `[parry]` Debug line per parry. Also applies in the Yingzhao leaderboard challenge (my choice).

### ParryFromBehind

Config: `Enabled`.

Postfix on `EffectDealer.FacingRuleCheck(DamageDealer, Vector3, Vector2)`: a failed check becomes true when the
instance is `Player.i.health.parryDealer`. Always player-only.

### DashInvulnerability

Config: `Enabled` (on), `InvulnerabilityAfterDash` (0.05-0.5 s, default 0.1, my choice; vanilla 0.05).

I asked for invulnerability during the dash: the game already has it, and the F9/log diagnostics showed the hits
came right after it (the Spear Horse's `SlashDamage`, always ~0.05 s after the receivers came back). A transpiler
swaps the one `ldc.r4 0.05` between `ldftn DodgeEnd` and `Timer.AddTask` in `PlayerRollState.OnStateExit` for
`InvulnerabilityAfterDash()`; a prefix skips a `DodgeEnd` that fires while a new dash (not weak) is under way (the
timer has no owner, so the previous dash's end would cut the new one's invulnerability). A prefix on
`Player.OnTakeDamage` logged `[dash]` hits during or right after a dash while I tuned it (removed after the 1.0.0
audit): with 0.1 s, two of three slash hits still landed ~0.05 s after the window (0.2 s would cover them); I kept
0.1.

## Death

### RespawnNearDeath

Config: `Enabled`, `BossRespawnOutsideArena`.

Ordinary deaths: quick revive on the last safe ground (`PlayerDeadState.Revive` prefix). Boss deaths: reload the
scene and spawn just outside the arena.

1. Prefix `PlayerDeadState.OnStateEnter` → `DeathSnapshot.Record()` (position, last safe ground, level, boss
   fight: `monsterHpUI.CurrentBossHP != null` or an engaged monster tagged "Boss").
2. Prefix on the private async `PlayerDeadState.Revive` → `RespawnController.TryRespawn`; false skips the game's
   revive. The game's respawn still runs in debug mode, maps with native `CanQuickRevive()`, VR challenge, memory
   mode, leaderboard challenge, a pending tutorial respawn, core state not Playing, or a level change.
3. Ordinary death → `Respawner.QuickReviveAt`: `PlayerDeathPenalty()` if `IsNeedDeathPenalty` (vanilla quick
   revive never applies it), `FadeToBlack` → `ResetLevel(false)` (spawns the bag) → `RestoreEverything` →
   Normal → `SetPosition` → `ForceOnGround` → `SafeGroundRecorder.ForceSave(pos)` (the reset moved the last safe
   ground to the death spot) → camera → BGM → `FadeOutBlack`. Fallback: `RespawnAtSavePoint`.
4. Boss death → `Respawner.ReloadSceneAt`: `ChangeScene` to the same scene at a level-relative point: the newest
   `SafeTrail` point outside the active `BossArea`s + 64 margin, which most boss scenes use (their walk triggers are
   all cutscenes); in a scene without one (Yingzhao, Goumang), outside the fight entrance, recorded when its
   `PlayerWalkToTrigger` first fires; then the scene entrance. Clears `GameCore._lastTeleportPoint` first. Validated
   on Yingzhao many times.
5. Never patch `CanQuickRevive`/`FetchReviveData`: `FetchReviveData` sets `_lastTeleportPoint`, whose
   `OnSceneChangeDone` plays the Root Node revive animation that only an animation event ends: invisible player
   with no control away from the node.

### NoDeathPenalty

Config: `Enabled`.

Transpiler on the async `PlayerDeathPenalty`'s `MoveNext` (found through `AsyncStateMachineAttribute`) points its
two `CurrentGold = 0` / `CurrentExp = 0` calls at helpers that skip them when the death decided to keep; the rest
of the penalty (delay, deathCount, AutoSave, ResetDeathPenalty) stays vanilla. Prefix + postfix on
`PlayerDeadRecord.StorePlayerDataBeforeDead` take the decision at death (`Enabled` and the transpiler matched,
kept in a static until the penalty) and empty the bag only when the game stored a new one
(`CanStoreDeadRecord`). Bag still spawns, empty, no option (my choice: the bag-pickup skills still trigger).

## HUD

### BossHPNumbers

Config: `Enabled`; `BossHP` (on) and `BossInternalHP` (off), each with `Position` (pixels of a 1920x1080 screen,
left end of the number), `FontSize` (8-72, 19), `Color`.

Like MicheliniDev's HPNumbers: TMP texts on a canvas of our own (ScreenSpaceOverlay + CanvasScaler 1920x1080,
registered in RCGLifeCycle, destroyed on unload), read in `LateUpdate`, text rebuilt only when the int changes.
The boss is the one whose bar the game shows (`monsterHpUI.CurrentBossHP.bindingPosture`: `GetPostureValue()`,
`InternalInjury`); everything hides with the HUD (`playerInGameUIDisplayVote.Result`). No Yi numbers and current
value only (my choice). Off when `HPNumbers` (the original's GUID) is loaded.

## Loot and Jin

### LootMagnet

Config: `Enabled`, `Delay` (0-3 s, default 0.5).

`DropItem.Update` prefix starts the game's own flight (`ForceFlyToTargetTurnOffSolid`, `flyToTarget =
CenterAnchor`, `FlyToPicker`) for value drops (`AbstractDropping` or `DropArrow`) with touch pickup; timer = the
game's `_forceFlyToPlayerCounter`; no flights while loading or dead; flying drops wait in the air while Yi is
dead. All loose drops, like FarawayPickup (my choice: no OnlyJin toggle).

### JinMultipliers

Config: `Enabled`, `MonsterDrops`, `TransmuteUntoWealth`, `Selling` (1-1000, default 1; mine 100/200/100).

`AddGold` prefix multiplies Monster/Chest coins and FooExplode; `MerchandiseData.Trade` postfix adds the rest for
sales (type 販賣 with a `mainMaterial`, paid in the gold field); `MerchandiseItemButton.UpdateView` postfix shows
the multiplied price. Death bag and purchases untouched. The 99,999 Jin cap stays vanilla (my choice; documented
in F1 and the Thunderstore README).

## Travel

### TeleportFromAnyNode

Config: `Enabled` (default on).

Postfix on the protected `TeleportPointMatchSavePanelCondition.isValid` ("this Root Node is the Pavilion"): a
false result becomes true only for the instance on the Root Node menu's Teleport button (a direct child of
`SavePanel.TeleportButton`) or under the pause menu's Teleport tab, while I sit at another Root Node
(`TeleportMenus.SittingNode`) whose Pavilion button (the button with a `TeleportToSavePointAction` to
`AGSavePoint`, `GoHomeFlute`) is valid right now; inverted instances are never touched. Every other condition
stays, so the game's story locks hide the Teleport where they hide the Pavilion button. A prefix on the
`TeleportPointButton.SubmitImplementation` stub stores the node left with `SetLastSaveItem` (guarded), like the
Pavilion button: the trip ends as the route through the Pavilion would (respawn at the destination, Last Node at
the node left). `[teleport]` Debug lines. Off when nozwock's or Ixrec's mod is loaded.

### EarlyTeleport

Config: `Enabled` (default **off**, like EarlyAirDash).

I asked for it after the F9 showed my save does not have the game's teleport yet (`(Ability) 神遊系統` not
acquired). Postfixes on `AbilityActivatedCondition.isValid` (only the Teleport tab's "[Ability] 神遊Activated")
and `GameFlagPropertyCondition.isValid` (only the non-inverted one on the Teleport button, "神遊已解鎖" = that
ability's `acquired` field; the inverted "not in the final stage" stays) pass while the ability is not
acquired. Nothing written; once the game gives the ability its rules decide. Alone: Teleport only at the
Pavilion. `[early teleport]` Debug lines.
