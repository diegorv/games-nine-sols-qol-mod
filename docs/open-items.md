# Open items

Every planned feature is done. Next: the first release, unless I ask for a new feature.

## Release

- Our own `thunderstore/icon.png` (the template's placeholder is still there).
- The rest of the first release: `docs/thunderstore-upload.md`.

## Not seen in game yet

- LootMagnet: the "flying drops wait while Yi is dead" path is verified by review only.
- JinMultipliers: Transmute Unto Wealth and selling not yet seen in the log (`[jin] Transmute Unto Wealth`,
  `[jin] sold`; `is not a sale` would mean the 3D printer uses another trade type).
- NoDeathPenalty: picking up an empty bag with the Life Recovery or Azure Recovery skill (I don't have them yet).
  Their effects live in prefab FSMs; nothing in code reads the bag's contents, so they should still trigger.
- TeleportFromAnyNode/EarlyTeleport: the story locks (Prison, after Lady Ethereal, final stage), and
  EarlyTeleport handing over to the game when `(Ability) 神遊系統` is acquired. The Teleport tab and button
  decisions log a few `[teleport]`/`[early teleport]` Debug lines per menu opening (each tab change runs
  `PrepareValidTab` twice, and both button components `EnableCheck`).

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
