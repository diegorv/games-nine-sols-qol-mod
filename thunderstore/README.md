# Nine Sols QoL

Quality of life tweaks for Nine Sols: respawn near death, no death penalty, an easier perfect parry, loot magnet
and more. Each one can be turned off in the config menu (F1), where they are grouped as below.

Installing it also installs NineSolsAPI (which keeps the game from unloading the mods when it returns to the
title screen; note that it turns achievements off by default, in its own config) and the BepInEx
ConfigurationManager (the F1 menu).

## Abilities

### Early air dash

Off by default. The game gives Yi an air dash later on (once per air time, recharged when you land, slide on
a wall, grab a ledge or rope, parry and so on). With `Early air dash` on, you can use it before you get that
upgrade. Nothing is unlocked in your save, and once you get the upgrade the game's own rules apply again.

### Fast heal

Healing with the pipe is as fast as with the Quick Dose jade, without equipping it: the animation plays 1.67
times faster, so a heal takes 60% of the time. With the jade equipped nothing changes.

## Combat

### Perfect parry window

A perfect parry is easier to land: by default you can press parry up to 12 frames (200 ms) before a hit
instead of 8 (133 ms). `Perfect parry frames` sets the window, from 1 to 30 frames. The game's penalty for
mashing parry still works the same way, scaled to your window.

### Parry from behind

Parry attacks that come from behind you. Attacks that cannot be parried at all stay that way, and you
still have to press parry.

### Dash invulnerability

A dash already makes Yi invulnerable, but the game ends it 0.05 s after the dash, so an attack that is still
active when the dash ends can hit right away. Yi now stays invulnerable for `Invulnerability after dash`
seconds after a dash (0.1 by default), and a dash that starts during that time keeps its own invulnerability.
Attacks can't be parried during that time either, and solid traps still hurt, as in the game.

## Death

### Respawn near death

Respawn near where you died instead of at the last Root Node.

- Ordinary deaths: you get back up on the last safe ground before the death, in the same room.
- Boss deaths: the room reloads so the boss fight resets, and you start right outside the arena.

The death penalty is the game's (your Jin and XP go into the death bag, which you pick up by interacting
with it), unless No death penalty is on.

### No death penalty

You keep your Jin and XP when you die. The death bag still appears as usual (or is carried by the enemy that
killed you), but empty: you can still pick it up, for the skills that react to that.

## HUD

### Boss HP numbers

The boss's HP as a number while the game shows the boss bar, and its internal injury too if you turn on
`Boss internal HP`. Each number has its position, font size and colour in the config (positions are in pixels
of a 1920x1080 screen and scale with other resolutions). The numbers hide with the game's HUD: menus, the map,
dialogues and cutscenes.

## Loot and Jin

### Loot magnet

Jin, health, qi, ammo and arrows dropped anywhere in the room fly to you on their own, `Delay` seconds
(0.5 by default) after they appear. The death bag and key items still have to be picked up by hand.

### Jin multipliers

Multipliers for the Jin you earn, all 1 (unchanged) by default:

- `Monster drops`: coins dropped by enemies, chests and breakable objects.
- `Transmute Unto Wealth`: the 10 Jin from each enemy killed by a talisman explosion.
- `Selling`: the Jin you receive when you sell an item, such as recycling at the 3D printer. The shop shows
  the multiplied amount. Prices you pay are unchanged.

The death bag is never multiplied.

Note: picking up coins can take your Jin above 99,999, but the game cuts it back to 99,999 the next time you
buy or sell something, and the excess is lost. Spend it before you get there.

## Travel

### Teleport from any node

The game lets you teleport only from the Pavilion's Root Node, so a trip between two Root Nodes goes through
the Pavilion. Now every Root Node that shows the Pavilion button also shows Teleport, with the same list of
Root Nodes. The node you leave becomes the Last Node, as when you go to the Pavilion. Where the game hides the
Pavilion button (for example during the Prison), Teleport stays hidden too, so the story's locks still apply.
It needs the game's teleport, which the story unlocks later on, or Early teleport.

### Early teleport

Off by default. Use the game's teleport before the story unlocks it: at the Pavilion's Root Node, and with
Teleport from any node at every Root Node that shows the Pavilion button. You can only go to Root Nodes you have
activated. Nothing is unlocked in your save, and once the game gives you the teleport its own rules apply.

## Compatibility

Each feature turns itself off, with a warning in the log, when the mod it replaces is installed, so the two never
patch the game twice:

| Feature | Turns off with |
|---|---|
| Perfect parry window | Supidowagon's or nozwock's PerfectParry |
| Parry from behind | Supidowagon's AnyDirectionParry |
| No death penalty | nozwock's NoDeathPenalty |
| Boss HP numbers | MicheliniDev's HPNumbers |
| Loot magnet | nozwock's FarawayPickup |
| Jin multipliers | Ixrec's JinMultiplier |
| Teleport from any node | nozwock's TeleportFromAnyNode, Ixrec's TeleportFromAnywhere |

## Credits

Every feature is written from the game's own code; the ideas come from these mods, which are worth a look:
[PerfectParry and AnyDirectionParry](https://thunderstore.io/c/nine-sols/p/Supidowagon/PerfectParry/) by
Supidowagon, [JinMultiplier](https://thunderstore.io/c/nine-sols/p/Ixrec/JinMultiplier/) and
[TeleportFromAnywhere](https://thunderstore.io/c/nine-sols/p/Ixrec/TeleportFromAnywhere/) by Ixrec,
[FarawayPickup](https://thunderstore.io/c/nine-sols/p/nozwock/FarawayPickup/),
[NoDeathPenalty](https://thunderstore.io/c/nine-sols/p/nozwock/NoDeathPenalty/) and TeleportFromAnyNode by
nozwock, and [HPNumbers](https://thunderstore.io/c/nine-sols/p/MicheliniDev/HPNumbers/) by MicheliniDev.

Source code, issues and the full credits: [GitHub](https://github.com/diegorv/games-nine-sols-qol-mod).
