# Open items

Every planned feature is done and 1.0.0 is on Thunderstore. Next: release the audit fixes, unless I ask for a new
feature.

## Release

- 1.1.0 (minor: the NineSolsAPI dependency is gone): push `main`, then run the **release** workflow with `minor`
  (`docs/thunderstore-upload.md`).

## Not seen in game yet

- LootMagnet: the "flying drops wait while Yi is dead" path is verified by review only, and so is its 2026-10-18 fix
  (coins keep flying to a dead Yi when NoDeathPenalty keeps the death's Jin): die while coins fly, then compare
  F9's `gold=` before and after the revive.
- DashInvulnerability: the 2026-10-18 fix of the DodgeEnd tolerance (audit combat-1) was tested with single dashes
  only; chained dashes (`keptDashes` rising in F9) not yet.
- JinMultipliers: Transmute Unto Wealth not yet seen in the log (`[jin] Transmute Unto Wealth`). Selling was seen
  on 2026-10-18 (money bags in the Pavilion).
- NoDeathPenalty: picking up an empty bag with the Life Recovery or Azure Recovery skill (I don't have them yet).
  Their effects live in prefab FSMs; nothing in code reads the bag's contents, so they should still trigger.
- TeleportFromAnyNode/EarlyTeleport: the story locks (Prison, after Lady Ethereal, final stage), and
  EarlyTeleport handing over to the game when `(Ability) 神遊系統` is acquired. The Teleport tab and button
  decisions log a few `[teleport]`/`[early teleport]` Debug lines per menu opening (each tab change runs
  `PrepareValidTab` twice, and both button components `EnableCheck`).

## Audit questions that need later bosses

From the 2026-10-18 audit (`..\ninesols\research\audit\SUMMARY.md`), to check live with bepinex-mcp at each new boss
(`search_gameobjects(component_name="PlayerWalkToTrigger" | "BossArea" | "PlayerTransformParentChangeTrigger")`):

- respawn-1, fixed after 1.1.1: the offline scan of all 117 scenes (`..\ninesols\research\scenescan\`) showed that
  only the Horseman (`A2_S5`) and Goumang (`A3_S5`) scenes start their fight with an active `PlayerWalkToTrigger`
  and have no `BossArea`; every `PlayerWalkToTrigger` in the ~25 `BossArea` scenes is a story cutscene. A boss
  respawn now uses the `BossArea` when the scene has an active one, and a recorded entrance only otherwise (or
  when it lies outside every boss area). Tested on the Horseman (unchanged); still to see at a `BossArea` boss:
  the log line `(outside the boss area)` after a death there, even if a cutscene played in that scene before.
- respawn-5: the game's scripted override respawn (`SaveManager.forceSaveFlag`) is not among the early-outs; matters
  only if a story sequence uses it.
- abilities-hud-3: with two boss bars, BossHPNumbers follows only the first (read `UIMonsterHPManager.bossHPList`
  during a fight with two bosses).
- Scene data offline: `..\ninesols\research\scenescan\scan.py` (UnityPy 1.25, ~2 min) reads every scene in
  `NineSols_Data\data.unity3d` and lists each `PlayerWalkToTrigger`, `BossArea`, `PlayerTransformParentChangeTrigger`
  and `PlayerSensor` with its path and active state, plus the boss scripts per scene (`all.json`). It cannot answer
  what happens during a fight (two boss bars, `forceSaveFlag`).
- respawn-2 (fixed in 1.1.0 with `ResetParent`): the game has only 6 `PlayerTransformParentChangeTrigger`, in
  `A3_S1_GardenRuins_Final`, `A3_S3_OxygenChamber_Final` and `A4_SG3_MemoryCrate New`, none a boss scene; a death on
  a lift there would confirm it.

## Possible improvements

- LootMagnet: health, qi and ammo drops fly in even when full and the excess is wasted (like FarawayPickup;
  accepted). Possible improvement: leave them on the ground while the resource is full.
- RespawnNearDeath for other bosses: BossArea bosses, FSM bosses (Yi Gung, Jee, Jiequan), A4_S5 (fight starts on
  load), Goumang (locked doors). They fall back to the scene entrance today; start with an F9 dump at the arena.

## Accepted limits

- NoDeathPenalty: a hot reload between a death and its penalty (2 s, or until the next door on maps with the
  game's own quick revive) loses that death's Jin (dev only). On such a map two deaths share one pending
  penalty: if the setting is toggled between them and the second death stores no bag (`CanStoreDeadRecord`
  fails), the second death's decision wins, giving the old full bag plus the kept Jin (off, then on) or an empty
  bag and zeroed Jin (on, then off). Fixing it needs one more patch (the pending flag before
  `NeedDeathPenaltyCheck`); whether `CanStoreDeadRecord` can flip between two deaths without a scene load is
  UNCONFIRMED. An unretrieved bag from a vanilla death is overwritten by the next death, as in vanilla.
- Hot reload, dev only: each reload leaves the old plugin object and BossHPNumbers' destroyed canvas in
  RCGLifeCycle's static list (a few entries, never read again), and a reload during a quick revive's fade runs the
  old assembly's FadeToBlack callback.
- JinMultipliers: at the 99,999 cap a sale (and "sell all") still takes the item and the Jin is lost, as in vanilla
  (my choice; said in the Selling description and the README).

## If it happens again

- The game froze once during the HPNumbers session (2026-10-17) and had to be restarted. Cause unknown: the
  restart overwrote `LogOutput.log` (`[Logging.Disk] AppendLog = false` in `BepInEx.cfg`) and the game no longer
  writes Unity's `Player.log`. If it happens again, copy the log before reopening the game (or turn `AppendLog`
  on).
- Lag: with only our mod there were no frames over 100 ms. With chatty mods the BepInEx console is the prime
  suspect: compare `[hitch]` lines and try `[Logging.Console] Enabled = false`.

## History

- Commit `adba129` ("Remove old prompt") carries part of the Pickup code and does not build alone; `472e3b2`
  completes it.
- Every feature's review is applied: HPNumbers, AirDash and HealSpeed (now BossHPNumbers, EarlyAirDash, FastHeal)
  in `52c4abb`, `44ba9b5`, `ab22e56`, DashInvulnerability in `625c239`, TeleportFromAnyNode and EarlyTeleport in
  "Apply the Teleport review findings" (reports in `research\teleport.review*.md`). The feature rename was
  checked by building, reloading and reading the log, not by a review.
- On 2026-10-18 the session handoff file `PROMPT-NEW-SESSION.md` was split into `CLAUDE.md` and `docs/`.
- On 2026-10-18 the code was audited (10 Opus areas, Haiku checks): `..\ninesols\research\audit\SUMMARY.md`. Its fix
  plan was applied in the commits after 1.0.0, up to "Drop the NineSolsAPI dependency"; the audit was wrong that a
  release install only needed the RCGLifeCycle registration (Unity also destroys the manager object at startup).
