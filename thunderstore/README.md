# Nine Sols QoL

Quality of life tweaks for Nine Sols. Each one has its own section in the config and can be turned off.
Work in progress.

## Respawn

Respawn near where you died instead of at the last Root Node.

- Ordinary deaths: you get back up on the last safe ground before the death, in the same room.
- Boss deaths: the room reloads so the boss fight resets, and you start right outside the arena.

The death penalty is the game's (your Jin and XP go into the death bag, which you pick up by interacting
with it), unless No death penalty is on.

## No death penalty

You keep your Jin and XP when you die. The death bag still appears as usual (or is carried by the enemy that
killed you), but empty: you can still pick it up, for the skills that react to that.

## Parry window

A perfect parry is easier to land: by default you can press parry up to 12 frames (200 ms) before a hit
instead of 8 (133 ms). `PerfectParryFrames` sets the window, from 1 to 30 frames. The game's penalty for
mashing parry still works the same way, scaled to your window.

## Any direction parry

Parry attacks that come from behind you. Attacks that cannot be parried at all stay that way, and you
still have to press parry.

## Pickup

Jin, health, qi, ammo and arrows dropped anywhere in the room fly to you on their own, `Delay` seconds
(0.5 by default) after they appear. The death bag and key items still have to be picked up by hand.

## Jin multipliers

Multipliers for the Jin you earn, all 1 (unchanged) by default:

- `MonsterDrops`: coins dropped by enemies, chests and breakable objects.
- `TransmuteUntoWealth`: the 10 Jin from each enemy killed by a talisman explosion.
- `Selling`: the Jin you receive when you sell an item, such as recycling at the 3D printer. The shop shows
  the multiplied amount. Prices you pay are unchanged.

The death bag is never multiplied.

Note: picking up coins can take your Jin above 99,999, but the game cuts it back to 99,999 the next time you
buy or sell something, and the excess is lost. Spend it before you get there.
