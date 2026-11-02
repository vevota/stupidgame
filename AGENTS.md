# stupidgame

3D foddian platformer prototype. You have a magneto stick, two banded planks, and a
canyon to cross. Climb to the mountain.

## Pinned

- Unity **6000.6.7f1** (`~/Unity/Hub/Editor/6000.6.7f1/Editor/Unity`)
- Template: **Universal 3D (URP 17.6.0)**
- Input: **new Input System only** (`activeInputHandler: 1`) — never use `UnityEngine.Input`,
  it throws. Poll `Keyboard.current` / `Mouse.current`.
- Physics: fixed step 0.02, gravity `-30` (set from the level file)

## Commands

```sh
./tools/cscheck.sh    # 2s C# compile check against Unity's refs — works with the editor OPEN
./tools/verify.sh     # full batchmode compile, greps errors out of the log
./tools/build.sh      # Linux player -> build/stupidgame.x86_64
./tools/shot.sh 4     # run player under xvfb, screenshot after 4s
```

`cscheck.sh` is the fast loop — use it after every edit. The other three need the editor
closed, because the editor holds a lock on the project.

## Hard rules

1. **Never delete files.** Not in this project, not anywhere on the machine. Move or
   comment things out instead. Ask first if something really must go.
2. **Code-first.** Everything is spawned by `GameRoot` from `Assets/Resources/levels/lvl01.json`.
   Do not hand-edit `.unity` or `.prefab` YAML and do not add components in the inspector —
   they will be invisible to the level file and lost on reload.
3. **No new packages** without asking. No asset store downloads.
4. **Placeholder art only** until slice 5. Colours come from `Materials.ColorFor`.
5. **Keep it launching.** Every change must leave the game bootable. Commit at each
   working slice.
6. Update "Current slice" below whenever it changes.

## Decisions (locked unless we revisit)

- **First person.** Camera is a child of the player; `FirstPersonLook` owns yaw (applied to
  the player) and pitch (applied to the camera). The player mesh is disabled — the capsule
  exists only as a collider.
- **Viewmodels render on layer 8** through a second camera that clears depth only. Raycasts
  must always exclude `1 << ItemController.ViewmodelLayer`, or you will grab the stick itself.
  That second camera must be a URP **Overlay** camera added to the main camera's
  `cameraStack`. A second *base* camera always clears the colour buffer in URP and blanks
  the world even with `ClearFlags.Depth`.
- **The camera is never parented to the player.** Physics interpolation overwrites rotation
  written to an interpolated Rigidbody's transform, which silently kills mouse look. The
  look script sets the camera's world position/rotation each `LateUpdate` from the player's
  interpolated position.
- **Items**: `ItemController` holds fist/magneto. `1` fists, `2` magneto, wheel cycles.
  Switching items calls `MagnetoStick.CancelHold()`, so the plank is dropped, never left
  attached to a disabled stick. Fists are deliberately an empty viewmodel.
- **Held props are anchored AND pivoted at the clicked point.** `_localAnchor` is captured
  at pickup and the prop is placed so that exact point sits `holdDistance` in front of the
  camera. The grab point is a real pivot: grabbing a plank by its end must swing differently
  from grabbing it in the middle.
- **Grip is springy, not rigid.** Position follows on a spring anchored at the clicked point
  (`positionSharpness`), so the prop lags and has weight. Rotation eases toward a *level*
  carry pose (`levelSharpness`) instead of being welded to the camera, so a bad grab
  self-corrects over ~0.4 s rather than wrecking a crossing. Trailing inertia comes from
  `inertiaLag`.
- **Escalation warning**: do not make the grip fully rigid again. A fully camera-locked prop
  swings wildly on pitch, is hard to aim onto a gap, and fights the player mid-jump.
- **The beam is world-space** (`MagnetoBeam`): a LineRenderer from near the stick to the grab
  point when holding, or a thinner dimmer one to the aim point when only targeting. It must
  render on the main camera, not the viewmodel layer — that camera has its own FOV, so a
  viewmodel-space beam would not line up with the world. World-space also means geometry
  occludes it correctly.
- **The wheel is overloaded by context**: push/pull while holding, item cycling when empty.
  `ItemController` bails when `_stick.Held != null`.
- **`V` + mouse free-rotates the held prop** and sets `FirstPersonLook.SuppressLook`, or the
  camera turns at the same time as the prop.
- **Never re-orient a held prop to the aim.** Forcing the long axis along the view is what
  made every pickup look like a short-side grab (`HoldOrientation.For`, now superseded).
  Rotation comes from the camera plus `Q`/`E` spin only.
- **Grab offset is shown in the HUD** (`grab=1.50m`) and logged on grab. If it reads 0.00 the
  hit point is not reaching `Grab()` and the anchor is silently falling back to the centre.
- **Props accept `rotY` in the level JSON.** When testing grab-point behaviour, make sure the
  plank's long axis is not aligned with your view — translation along the view axis is
  invisible in first person and looks like "nothing changed".
- **Ignore collision while held.** A plank grabbed off-centre overlaps your capsule; as a
  kinematic body it would shove you around. `Physics.IgnoreCollision` is toggled on grab and
  restored on drop/freeze/throw/item-switch.
- **Aim assist** is a 0.18 m sphere cast after the thin ray; a thin ray alone makes small
  props feel broken. Never exclude the currently-aimed prop from target validation — doing
  so makes the aim latch flip every frame (flickering highlight, unclickable props).
- **No bunny hop.** Jump needs a fresh press; the buffer only exists so a press slightly
  before landing still counts. `jumpCooldown` (0.1 s) stops one press firing twice.
- **Grounding must not depend on velocity.** Coyote time refills on any ground contact. An
  earlier version only refilled when `velocity.y <= 0`, which silently disabled jumping while
  standing on tilted or edge-of-plank surfaces, because depenetration pushes the capsule up a
  little each frame. Ground normals accept up to ~70° (`normal.y >= 0.35`), and the probe
  fires 9 parallel down-rays including diagonals, because a narrow plank edge leaves all
  cardinal rays in the air.

- **Magnetism is physical**: only props with `magnetic: true` (visible metal) can be grabbed
  or frozen. Wood without metal is scenery you can carry, never float.
- **Stasis is permanent** until re-grabbed. No charges, no decay. The **prop count is the
  budget** — two planks, unlimited placing, so the skill is aim + timing + jump distance
  ("leapfrog bridging": stand on A, place B, jump, re-grab A behind you, repeat).
- **Grab range 7 m** must stay >= plank length (3) + body reach, or leapfrogging becomes
  impossible. Regression-check this if you change `holdDistance`/`range`.
- **No air control** (`airControl 0.25`). Committed jumps are the difficulty.
- **Falling is not death.** You land on the canyon floor and climb the stairs back up. No
  checkpoint, no kill plane except a `killY` safety net at `-40`.

## Files

| Path | What |
|---|---|
| `Assets/Scripts/GameRoot.cs` | bootstrap (no scene edits), spawns player/camera/goal |
| `Assets/Scripts/LevelLoader.cs` | JSON -> scene objects |
| `Assets/Scripts/Prop.cs` | hold / freeze / throw / tint |
| `Assets/Scripts/MagnetoStick.cs` | aim, grab, stasis, throw |
| `Assets/Scripts/PlayerMotor.cs` | rigidbody controller, ground probe, coyote/jump buffer |
| `Assets/Scripts/FirstPersonLook.cs` | mouse look, cursor lock |
| `Assets/Scripts/ItemSystem.cs` | fist/magneto slots + procedural viewmodels |
| `Assets/Scripts/MagnetoBeam.cs` | beam + impact tip, world space |
| `Assets/Scripts/HoldOrientation.cs` | SUPERSEDED, kept for a possible align-to-aim key |
| `Assets/Scripts/FollowCamera.cs` | UNUSED, kept for a possible third-person toggle |
| `Assets/Scripts/Hud.cs` | readouts + crosshair |
| `Assets/Scripts/ScreenshotCapture.cs` | `-screenshot` / `-quitAfter` for headless capture |
| `Assets/Resources/levels/lvl01.json` | the level |

## Controls

WASD move · Shift sprint · Ctrl crouch · Space jump · 1 fists · 2 magneto ·
wheel push/pull while holding, cycle items when empty · LMB grab/drop ·
RMB freeze/unfreeze · V + mouse rotate held prop · Q/E spin · F throw · R respawn ·
Esc free mouse, click to recapture.

## Current slice

**Slice 2 (in progress): grab/carry/throw end to end, first person, item slots.**
Slice 1 (the loop: plateau -> 24 m canyon -> stairs back up -> far plateau -> goal) is built.
Two planks sit on the start plateau.

Next: **slice 3**, midair freeze + stand on it + re-grab, i.e. leapfrog across the canyon.
This is the go/no-go for the whole game.

Then: **slice 3**, midair freeze + stand on it + re-grab, i.e. leapfrog across the canyon.
This is the go/no-go for the whole game.
