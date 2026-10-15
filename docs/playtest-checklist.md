# Playtest Checklist

## Config

- File: `BepInEx/config/grant.sor.characters.cfg` in the game folder. It is created on the first launch with the mod installed.
- Railgunner settings are in the `[Railgunner]` section (charge time, speeds, recharge, beam size and range, Line 'Em Up kills and shots, screen shake, muzzle flash, beam fade, afterglow, sparks per tile, sound names, Supercapacitors recharge multiplier). Restart the game after editing. Invalid values fall back to the default and log a warning.
- Custom sounds: drop `railgun_charge` or `railgun_fire` (.wav, .ogg or .mp3) into `BepInEx/plugins/SoRCharacters/sounds/` to replace the charge-up or main fire sound (the folder and a README are created on first launch). Restart the game after changing files; the log says which file was loaded or that the built-in sound is used.
- **`ResetPresets`** (section `[Presets]`): set it to `true`, then start the game once. The mod rewrites its character presets and turns the setting back to `false` by itself. Each existing preset is backed up (to `BackupData`) before it is replaced.
- **Warning:** if you save a mod character in the Character Creator while its ability or quest isn't registered (for example the mod failed to load), the game drops the ability or quest from the saved character. Fix: set `ResetPresets` to `true` and start the game once.

## Railgunner

### Loading and setup

- [ ] The log shows `Preset 'Railgunner' written to …` and a `[Railgunner]` section appears in the config file.
- [ ] A run as "Railgunner" shows the Railgunner Kit trait, a Knife and the Railgun item.
- [ ] The character sheet shows Strength 1, Endurance 1, Accuracy 2, Speed 2 (preset value + 1).
- [ ] The Railshot icon is in the special ability slot.
- [ ] A "Railgun" item (the held-railgun picture) is in the inventory with the description "Why shoot one guy when you can shoot five? Just get them to stand in a nice, orderly line. They won't stay in it long." It is still there after changing level, and saving the character in the Character Creator keeps it.
- [ ] The Railgun item can't be dropped (not by dragging out, not by the drop key), can't be sold (Pawn Shop machine, shop, Sell-O-Matic) or stored in an ATM, and an NPC can't steal it or be given it.
- [ ] Using the Railgun item from the inventory shows "Hold the special ability to charge it." over his head, and the item stays in the inventory.

### Charging and recharge

- [ ] Holding the special ability key plays the charge-up sound (`ZombieSpitCharge`).
- [ ] While charging up (before "READY"), he walks slower (half speed, `ChargedSpeedMultiplier`).
- [ ] Once "READY", he crawls (about 15% speed, `ArmedSpeedMultiplier`); after firing or cancelling he walks normally again.
- [ ] Once "READY", flicking the mouse (or stick) to the other side makes the aiming line swing round smoothly (about 90° per second, `AimTurnDegreesPerSecond`) the shorter way, instead of snapping.
- [ ] Releasing while the aiming line is still swinging fires along the line as drawn, not at the cursor.
- [ ] After 1 s the "READY" text appears and a thin red laser aiming line, fading out with distance, follows the mouse (or the aim stick on a controller).
- [ ] Once READY, a steady wobbling tone plays until he fires or the charge ends; it never keeps playing afterwards (check releasing, pausing, opening the inventory, dying, the elevator and quitting to the menu while charged).
- [ ] While holding, he can't punch or shoot.
- [ ] Releasing before the charge completes does nothing: no shot, no recharge.
- [ ] While charged, "READY" then "2" and "1" pop up over him one per second, and 3 s after "READY" (`ArmedMaxSeconds`) the shot fires on its own along the aiming line, exactly like a release: ready tone stops, recharge counts down from 15. The log says `auto-fire after 3s armed`.
- [ ] After an auto-fire, keeping the key held does nothing, and letting go doesn't fire again or restart the recharge (log: `released after auto-fire -> nothing`). Pressing it again later works normally once recharged.
- [ ] Releasing before the countdown ends fires as before and no further countdown numbers appear.
- [ ] Releasing when charged fires the shot, greys the icon and counts down from 15 to 0, with "Recharged" at the end.
- [ ] Taking a hit while charged hurts more (+50%).
- [ ] Dying while charged removes the aiming line and the slowdown; nothing fires.
- [ ] Taking the elevator while charged removes the aiming line and the slowdown; nothing fires.
- [ ] Being knocked out while charged removes the aiming line and the slowdown; nothing fires.
- [ ] Pausing while charged cancels the charge: no shot, no recharge.
- [ ] Opening the inventory while charged cancels the charge: no shot, no recharge.
- [ ] While charging, nearby NPCs with no job walk over to investigate, and it draws more people the longer he holds (louder once charged, loudest 2 s after "READY", `HumLoudAfterSeconds`). Pausing or releasing stops it.
- [ ] An NPC (Cop or civilian) who sees him charging or armed shows a "!" over their head, and the log says `<name>#<id> alerted`.
- [ ] About 2 s later (`AlertSeconds`) that NPC turns hostile (log: `turned hostile`): Cops attack, civilians run as hostile civilians do. This happens even if he has fired, released or moved out of sight in the meantime.
- [ ] Each NPC is alerted only once per charge; his followers and NPCs already hostile to him get no "!" and don't change.
- [ ] An alerted NPC who dies, or a level change during that second, doesn't turn anyone hostile afterwards (and nothing errors in the log).
- [ ] Pressing attack while charged acts as a release (fires if charged, cancels if not), with no swing or gunshot.
- [ ] While charging and once READY he visibly holds a railgun (grey body, cyan coils) in both hands, pointing along the aiming line; the log shows `held gun sprite collection: Items` and `holding the railgun sprite` (if it says `Laser Gun sprite`, the custom sprite wasn't found and the vanilla Laser Gun is shown instead).
- [ ] Before READY the railgun follows the mouse (or stick) directly; once READY it swings round slowly with the aiming line, and his body turns with it. Aiming left, the gun is flipped the right way up.
- [ ] With the Knife (or another melee weapon) equipped, the knife isn't drawn in his hand while he holds the railgun; with Fist, no fist glitch; with a gun equipped, only the railgun is shown.
- [ ] After firing, auto-firing or cancelling (release early, pause, inventory) the railgun disappears and his normal look returns (knife in hand again; a gun reappears next time he shoots it). His equipped weapon and its ammo are unchanged.
- [ ] Dying, being knocked out or taking the elevator while holding it leaves no railgun behind (next level, he isn't holding it).
- [ ] Local co-op: each Railgunner holds his own railgun only while he is charging.

### Firing

- [ ] Firing plays the Alien mind-ray sound (`MindControlFire`) layered with `LaserGunFire`.
- [ ] Every NPC lined up in the beam is killed and gibbed.
- [ ] A wall in the line is cut.
- [ ] Steel walls stay, but an NPC behind one still dies.
- [ ] Objects in the line are destroyed, and they count for a "destroy" mission.
- [ ] Floor objects (mines, traps, trip wires, manholes) in the line are left alone.
- [ ] Indestructible objects are left alone.
- [ ] A robot in the line is gibbed.
- [ ] The aiming line and the beam stop at the map edge.
- [ ] There is a strong screen shake and a very loud noise.
- [ ] The moment it fires there is a big, brief white flash: a wide white band along the whole beam (about 0.12 s, `FlashSeconds`), a bright white burst of larger particles at the muzzle, and an explosion-style light at the muzzle.
- [ ] After the white flash, the beam is a thin white-hot core inside a wider electric-blue glow, and it fades out fast (about a quarter second, `BeamFadeSeconds`).
- [ ] Lots of small yellow-orange spark streaks (12 per tile by default) spray sideways out of both sides of the whole beam and slow to a stop (`SparksPerTile`; 0 = none along the beam), with no noticeable slowdown on a full-length shot.
- [ ] Big spark bursts (about 30 sparks each) appear where the beam kills someone, where it cuts a wall, and at the far end of the beam.
- [ ] A faint blue afterglow stays along the path a little longer than the beam (about half a second, `AfterglowSeconds`).
- [ ] No flash, beam, afterglow, muzzle burst or sparks are left over a few seconds after the shot, or after a level change (fire, then immediately take the elevator or quit to the menu); the log shows `beam created (layer 1, shader tk2d/BlendVertexColor …)` once and no beam/sparks errors.
- [ ] Nearby NPCs and cops come to investigate.
- [ ] An owner who watches you cut their wall turns hostile.
- [ ] The game doesn't stutter noticeably on a long wall-cutting shot. If it does, report it; the fix is spreading wall destruction over a few frames.
- [ ] Local co-op: two Railgunners each have their own charge, aiming line and recharge.
- [ ] Local co-op in split screen: each aiming line shows only on its owner's screen.
- [ ] Local co-op: a partner standing in the beam dies and doesn't count for Line 'Em Up.
- [ ] A Mech pilot inside his mech who is in the beam dies (both the mech and the pilot).
- [ ] The Mayor in the line dies.

### Line 'Em Up (Big Quest)

- [ ] The missions sheet shows "Railshots with 3+ kills: 0/3".
- [ ] A shot that kills 2 doesn't change it.
- [ ] A shot that kills 3 or more updates it to 1/3; a shot that kills 5 still counts only once.
- [ ] The third shot with 3+ kills shows the "Big Quest completed" popup and "Lined them up 3 times!".
- [ ] Extra qualifying shots after completion raise the count ("Lined them up 4 times!").
- [ ] The count starts again at 0/3 on the next level.
- [ ] Leaving a level without completing it fails that level.
- [ ] Changing `LineEmUpKills` or `LineEmUpShots` in the config changes the numbers shown in the text and in the quest description.

### Upgrade traits (level-up rewards)

- [ ] Neither upgrade appears in the Character Creator or the Traits unlock menu.
- [ ] Upgrades are offered only on levels that are a multiple of 3, in the first trait slot, and not every time (the game rolls a trait tier); the Railgunner needs several runs to see each.
- [ ] Supercapacitors is offered at level-up only to the Railgunner (never to other characters). Its description says "10 s instead of 15 s", and after taking it the recharge counts down from 10 instead of 15 (`SupercapacitorsRechargeMultiplier`).
- [ ] Breaching Slug is offered at level-up only to the Railgunner. After taking it, steel walls in the line are cut and the aiming line goes through them; the map border still stops the beam and the aiming line, and lockdown barriers survive.
