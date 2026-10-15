# Backlog

Deferred work from reviews, to pick up before or alongside the next character.

## Foundation work (before the Magician)

From the Railgunner final review (2026-10-13). The Railgunner is the template the next characters will copy, so these should land first.

- **Core `ChargeTracker` with unit tests.** Move the Railshot charge state machine out of the plugin (`Railshot.cs` flags, `RailshotState`, the stall/pause logic in `RailgunnerModule.Tick`) into a pure Core class: `Begin(heldAtStart, frame)`, `Touch(frame)`, `ChargedFor(held)`, `ShouldCancel(frame, inputRunning, graceFrames)`, `BlocksAttackAt(frame)`. Test: hold through the recharge, stalled hold, hit-stop freeze, release while paused, attack-press frame, forced release from `cantPressGameplayButtons` / falling in a hole. The spec's Testing section promises these tests.
- **Use `Tiles` for the duplicated `0.64f` constants** in `AimLine.cs`, `RailshotFire.cs` and `RailshotEffects.cs` (`Vec2` and `Tiles` now live in `SoRCharacters.Core`).
- **Shared `ModGuard`.** Promote `RailshotFire.Guard`/`LogOnce` to a shared utility next to `Plugin.RunOnce`. While doing it, make `RailshotEffects.TickFlashes`' catch also destroy the failing flash.
- **Shared session reset.** One hook that clears per-agent state on quit-to-menu/title screen for every character. The Railgunner fixes this in `RailgunnerModule.Tick`; the next character would otherwise copy it.

## Small items (can wait)

- `IsPaused()` lacks `!gc.justEndedFreezeFrame`: a release in the single frame after a hit-stop cancels instead of firing (no cooldown cost).
- `BeamGeometry.Walk` allocates a list, a clipper and a comparer per call.
- Redundant `GraphUpdateScene.Apply()` after cutting walls.
- After a refused hit (teleporting victim, zero-damage roll), the victim's health bar may briefly show 1 HP.
- Line 'Em Up's popup and sheet text only show for player 1 in co-op (RogueLibs limitation); progress is tracked per player.
- RogueLibs CreateCustomAudio fails on Linux when the game path has spaces (unescaped file URL); the mod uses its own loader (ModAudio).

## Railgunner minor items (from reviews)

- Held railgun: `KeepVisible` in `HeldRailgun.Show` sits outside the try; a throw there would leave an entry with nothing shown until the charge ends.
- Held railgun: a knife/held item can flicker back during hit-stop freeze frames (hiding runs only when `AimGun` runs); once the game hides the gun mid-charge (out of control, whole-body animation) it stays hidden for that charge; while out of control the gun follows the aim but the body doesn't.
- Railgun item: using it bumps the "items used" end stat (side effect of the `Usable` category that gives the Use button); the reminder reuses the yellow "Countdown" text type.
- Alerted NPCs don't stop during their alert window (no clean vanilla way to pause an NPC's brain).
- Line 'Em Up popups only show for player 1 in co-op (RogueLibs).
