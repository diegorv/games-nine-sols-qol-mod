# Nine Sols QoL

Quality of life tweaks for Nine Sols. Each one has its own section in the config and can be turned off.
Work in progress.

## Respawn

Respawn near where you died instead of at the last Root Node.

- Ordinary deaths: you get back up on the last safe ground before the death, in the same room.
- Boss deaths: the room reloads so the boss fight resets, and you start right outside the arena.

The death penalty is unchanged: your Jin and XP go into the death bag as usual, and you pick the bag up
by interacting with it.

## Parry window

A perfect parry is easier to land: by default you can press parry up to 12 frames (200 ms) before a hit
instead of 8 (133 ms). `PerfectParryFrames` sets the window, from 1 to 30 frames. The game's penalty for
mashing parry still works the same way, scaled to your window.
