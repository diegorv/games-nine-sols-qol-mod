# Verified game facts

From the decompiled code unless marked otherwise ("seen in game" = F9 dump or log). Line numbers and IL offsets
match the build in `..\ninesols\decompiled\BUILD.txt`; the research reports in `..\ninesols\research\` have the
citations.

## Respawn and death

- `GameCore.FetchReviveData`: debug branch; force-save branch (`SaveManager.forceSaveFlag`); otherwise
  `lastSaveTeleportPoint.ChangeSceneData` and `_lastTeleportPoint = lastSaveTeleportPoint`.
- `GameCore.InitializeGameLevel`: `SetPosition(spawn)` → `ChangedDoneEvent` → penalty if `IsNeedDeathPenalty` →
  `resetManager.GameLevelStart` → `_lastTeleportPoint.OnSceneChangeDone` → fade-out callback (sets Playing).
- `PlayerDeadRecord.StorePlayerDataBeforeDead`: bag = carried Jin/XP × `DeathPenaltyRate` (= **1**, seen in
  game); drop point = last safe ground; a killer monster that can carry the bag gets a murderer tag. Dying again
  before pickup overwrites the record. The bag is picked up by interaction (`PlayerRestoreDeathPenaltyAction`).
  The bag is a `DropItem` with `IsTouchPlayerToPick = false` and a `DropPickable` (seen in game).
- `PlayerGamePlayData.PlayerDeathPenalty`: `async void`; after 100 ms: `deathCount++`, `CurrentGold = 0`,
  `CurrentExp = 0`, `AutoSave(FlagOnly)`, `Player.i.ResetDeathPenalty()`.
- `SafeGroundRecorder` records in `LateUpdate` on safe ground only; `ResetLevel` and `ForceSave` also write it.
  The player sits under the ResetManager (`GameCore(Clone)/RCG LifeCycle`, seen in game).
- `RCGLifeCycle.DontDestroyForever` only adds to a static list (safe in Awake). Back to title destroys every
  unregistered GameObject.
- Bosses: `PlayerWalkToTrigger` starts the fight for `BossLogic`, `Boss_SpearHorse_Logic`, `BossGouMangLogic`,
  `A1_S7_Logic`; others use `PlayerSensor`, `BossArea`, scene load or FSMs (Yi Gung, Jee, Jiequan).

## Death penalty

- `PlayerGamePlayData.PlayerDeathPenalty` is `async void`; its body is `<PlayerDeathPenalty>d__32.MoveNext` (a
  struct): after `UniTask.Delay(100)`, `data.deathCount += 1`, a Debug.Log, `ldloc.1; ldc.i4.0; call set_CurrentGold`
  and the same for `set_CurrentExp` (once each, IL_00D9-IL_00E2, no labels), `AutoSave(FlagOnly).Forget()`,
  `Player.i.ResetDeathPenalty()` (the only writer that clears `_lastNeedDeathPenalty`).
- Callers, all `if (IsNeedDeathPenalty)`: `GameCore.InitializeGameLevel` (every scene load), `DebugFastRevive` (debug
  mode), our `Respawner.QuickReviveAt`. Maps with the game's own quick revive defer it to the next scene load.
- That AutoSave is the only save after most deaths with our RespawnNearDeath (quick revive and boss reload skip the Root
  Node's save): a design that skipped the whole penalty would lose the kept Jin on quit.
- `StorePlayerDataBeforeDead` runs at death only when the death has a penalty (`NeedDeathPenaltyCheck`: not in memory
  mode, VR challenge or with `DeathPanaltyImmune`) and returns early when `CanStoreDeadRecord` fails (the record still
  holds an older bag). Bag spawn and pickup never read the contents: an empty bag spawns, shows on the map and gives
  `AddGold(0)`/`GainExp(0)` (seen in game). `deathCount` has no reader in code.

## Parry

- `PlayerParryState.Parried` (public virtual) tests `elapsed <= spamDatas[spamLevel] + 0f`; elapsed =
  `Time.time` − the latest buffered parry press (`PlayerInputCommandQueue`, presses expire after 0.5 s).
  `spamDatas` is private, `{0.13333334, 0.1, 0, 0, 0}`, read only there (IL_01EB..IL_0200). `spamLevel++` on a
  Parry exit without a parry; reset when exactly one press is buffered on enter, or on level reset.
- `ParryCounterDefenseState` overrides `Parried` and is always perfect. Auto parry, `IsAlwaysAccurate` and the
  QTE tutorial skip the window.
- `EffectDealer.FacingRuleCheck(DamageDealer, Vector3, Vector2)` is called only by
  `DamageReceiver.IsHitDamageNotParried` and `ParriableAttackEffect.ParryCheckForEvent`. Unparriable attacks are
  refused later in `ParriableAttackEffect.PlayerParriedCheck` (`if (!component.parriable) return false`).
  `Health.parryDealer` is the same object as `PlayerParryState.parryEffectDealer` (`.../SpriteHolder/ParryArea`,
  seen in game), whose rule the Parry state sets on enter (FaceToFace on ground/wall/ledge/rope).
- After a parry from behind, `KnockBack` turns Yi to face the attacker.

## Jin

- `AddGold(int, GoldSourceTag)` has 4 callers: coins (`DropMoney`), Transmute Unto Wealth (10 per talisman kill,
  a skill), the `add.money` console command and the death bag (`DeathPenalty` tag). Coins report `Monster`
  (pooled copies do not inherit the `[NonSerialized]` tag) or `Chest`.
- Gold is a `ScriptableDataFloat`. `AddGold` writes past its cap; every shop write (`ConsumeCheck`, `Receive`)
  goes through the clamped setter. The cap is **99,999 with no ExternalRepository** (seen in game): the excess is
  lost at the next purchase or sale.
- Selling = `MerchandiseData.Trade` → `OutcomeResult.Receive()` on a 販賣 trade. Recycling at the 3D printer is
  assumed to be one (not yet seen in the log: look for `[jin] sold` or `is not a sale`).

## Drops

- `DropItem` states Drop → OnGround (landing) → FlyToPicker → PickedUp. Vanilla flights: touch on the ground
  (`SensorTouchTarget`) or a prefab force-fly timer. `Picked()` credits (`IDropping.AbsorbedByPlayer`) and
  returns to the pool. `EnterLevelReset` (each spawn and reuse) resets state, target, trigger and the timer.
- `ResetLevel(false)` returns only monsters to the pool: drops stay where they were. A full scene load returns
  every pooled drop; a drop flying at that moment loses its value (the game logs "When DropItem destroyed and
  absorbed?").

## HUD

- The boss bar: `GameCore.monsterHpUI` (`UIMonsterHPManager`) keeps a private `bossHPList`; `CurrentBossHP` is its
  first entry (null when no bar shows); `UIBossHP.bindingPosture` is public. The bar draws `GetPostureValue()` (int)
  and `RemainTotal = GetPostureValue() + InternalInjury`. `DestroyBossHealth` removes the bar from the list at once
  (it only plays an Exit animation); `BossArea.localBossHP` bars bypass the list (whether any scene uses one:
  UNCONFIRMED).
- The HUD shows while `UIManager.playerInGameUIDisplayVote.Result` (public `RuntimeConditionVote`) is true: an AND
  of the gameplay input state (votes false on pause, map, dialogue, cutscene, death, loading), the "show gameplay
  UI" setting, the area name banner and a few scripted scenes. BossHPNumbers hides with it (seen in game).
- Back to title destroys every GameObject not registered with `RCGLifeCycle.DontDestroyForever` (which registers
  the object and the children it has at that moment).

## Dash and heal

- The dash is the `Roll` state, entered only by `Player.DodgeCheck` (`CanRoll || CanDownDash`, `RollDodgeAbility`,
  in the air `!IsWeak` and `RollDodgeInAirUpgrade`, `!IsInQTETutorial`). One air dash per air time:
  `airDodgeCount` (raised in `PlayerRollState.OnStateEnter`, cleared only by `ResetJumpAndDash`, read only by
  `CanRoll`). `ResetJumpAndDash` runs on landing (`VelY <= 0`), wall slide/run, ledge, rope, pile, grapple pass,
  zipline, every parry, jump-kick bounce, weakness hit, talisman stick and respawn; it also refills double jump,
  air attack, spin kick and the parry cooldown, so never call it to grant a dash. Each roll zeroes VelY and has no
  gravity. `RollDodgeInAirUpgrade` is read in code only by `DodgeCheck` (and the `activate.airDash` console command).
- Dash invulnerability: `PlayerRollState.OnStateEnter` turns off every hit receiver whose `CanDisable` is true (all
  but `SolidTrap` and `Heal`) unless `IsWeak`; `OnStateExit` schedules `DodgeEnd` (`IsDodgeAttack = false`,
  receivers back on) with `Timer.AddTask(..., 0.05f, null, "")`, an owner-less task nothing cancels.
  `IsDodgeAttack` has no effect (its readers in `PlayerAttackState` are empty branches).
- Healing: `PlayerDrinkPotionState`; the heal happens at the "Heal" clip's `Done` animation event, and the Quick
  Dose jade (`(Jade Data) Fast Pipe 速藥玉`, the state's own `SpeedUpHealJade` wrapper) only sets `animator.speed =
  1.66666f` on enter (`OnStateExit` sets 1). `PlayerMainAbilityCollection.SpeedUpHealJade` is never read.

## Teleport

Wiring seen in game with a one-off F9 dump (commit `37742f6`).

- Root Node menu = `GameCore.savePanelUiController` (`SavePanel`, same object as `UIManager.SavePointUI`); its
  `RCGUIComponentResetGroup` is on the panel itself and `EnableCheck`s every button when it is enabled. Buttons in
  hierarchy order: Challenge, Rest, Jade, SkillTree, Status, "back to last" (Last Node), "GoToAG 回議會" (Pavilion),
  "Teleport 神遊" (= `SavePanel.TeleportButton`, two button components on one GameObject), each condition on a child:
  - Teleport: "神遊已解鎖" (teleport ability acquired), "不在最後階段" (inverted: final stage), "match YiBase"
    (`TeleportPointMatchSavePanelCondition` with the Pavilion's node), memory challenge (inverted).
  - Pavilion: home flute flag, "not match YiBase" (inverted), `TreeGotMadCondition` (inverted; Lady Ethereal
    killed and no double jump yet), `PlayerInPrisionCondition` (inverted), final stage, memory challenge; its
    `TeleportToSavePointAction` (AG, `GoHomeFlute`) sits on the button with the same conditions.
  - Last Node: "match YiBase", Tree, final stage, memory challenge, flute, `TeleportCollectionLastSavedCondition`
    (inverted: a Last Node exists); `TeleportToLastSavePointAction` (`GoHomeFlute`).
- Pause menu tab "TeleportPanel Tab" (`UIManager.menuUI.tabsUI`, `PanelType == TeleportPanel`, `bindPanel ==
  UIManager.teleportPanel`): `PlayerSitAtSavePointCondition` (so it never shows away from a Root Node, which masks
  nozwock's static-cache bug), `CurrentTeleportPointActivateCondition`, "[Ability] 神遊Activated", final stage,
  "match YiBase", memory challenge. Map, Inventory and Encyclopedia tabs are hidden while sitting.
- The game's teleport is the ability `(Ability) 神遊系統`, unlocked by the story after the point where I am
  (A2, Yingzhao): before that even the Pavilion shows no Teleport, only Pavilion/Last Node trips.
- `TeleportPointButton.SubmitImplementation` works from any Root Node I sit at (bound `SavePoint` +
  `fluteCutScene`); `PlayerIsHere` blocks the current node. `SavePanel.CurrentSavePoint` goes stale (only that
  method clears it). `SetLastSaveItem` writes the flag before a lookup that throws for unknown nodes;
  `TeleportPointCollection` refuses the Pavilion as Last Node; Last Node never changes the respawn point.

## Startup and quitting to the title screen

- Seen in game (2026-10-18, no NineSolsAPI): Unity destroys BepInEx's manager object right after the chainloader,
  when the first scene loads; `DontDestroyOnLoad` does not hold yet. `HideAndDontSave` keeps it (BepInEx's
  `HideManagerGameObject`, NineSolsAPI's Awake, our `Plugin.RegisterLifeCycle`).
- A quit to the title screen runs `DestroyAllGameObjects._DestroyAll` (rcg.rcgmakercore.Runtime): it destroys every
  `FindObjectsOfType<GameObject>(true)` result except objects in `RCGLifeCycle`'s list, itself and
  `PrimeTweenManager`. `RCGLifeCycle.DontDestroyForever` adds the object and its current children, calls
  `DontDestroyOnLoad` and appends " (RCGLifeCycle)" to the name.
- The game's respawn reload calls `Player.ResetParent` (`GameCore.ResetWhenRespawnReloadScene`), which puts Yi back
  under his original parent after a lift or `PlayerTransformParentChangeTrigger` made him a child of something.
- `Timer` (RCG_General) adds new tasks at the start of its own Update and counts `RCGTime.deltaTime` (=
  `Time.deltaTime`) per task, so a task added in a frame may count that frame's deltaTime, depending on whether
  Timer updates after the player (script execution order, a project setting: UNCONFIRMED).

## Config menu and runtime

- ConfigurationManager 18.3.1 finds `ConfigurationManagerAttributes` by type name, copies `Order`/`IsAdvanced`
  by field name, sorts by Order descending.
- BepInEx's chainloader adds every plugin in one loop, so every plugin's Awake runs before any Start.
- GUIDs of the mods our features replace: `supidowagon.PerfectParry`, `nozwock.PerfectParry`,
  `supidowagon.AnyDirectionParry`, `nozwock.NoDeathPenalty`, `HPNumbers`, `nozwock.FarawayPickup`, `JinMultiplier`,
  `nozwock.TeleportFromAnyNode`, `TeleportFromAnywhere` (Ixrec's, confirmed from its DLL).
- HarmonyX 2.9 `PatchAll(Type)` = `CreateClassProcessor(type, true).Patch()`; `[HarmonyPatch(type, "Name")]`
  resolves with `AccessTools.DeclaredMethod`; `AccessTools.MethodDelegate` builds open-instance delegates for
  private methods; there is no `AccessTools.AsyncMoveNext` (resolve state machines through
  `AsyncStateMachineAttribute`), and `MethodType.Enumerator` handles iterators and UniTask-returning methods, not
  `async void`. `__state` is one local per patch declaring type, so a prefix and postfix in the same class share
  it; postfixes run on every return path (not when the original throws).
