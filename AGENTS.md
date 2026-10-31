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
- **Items**: `ItemController` holds fist/magneto. `1` fists, `2` magneto, wheel cycles.
  Switching items calls `MagnetoStick.CancelHold()`, so the plank is dropped, never left
  attached to a disabled stick. Fists are deliberately an empty viewmodel.
- **Held prop orientation**: local +X along aim, local +Y up (`HoldOrientation.For`). Props
  are authored long-axis-on-X, thin-axis-on-Y so the flat face stays up and is standable.
- **Aim assist** is a 0.18 m sphere cast after the thin ray; a thin ray alone makes small
  props feel broken.

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
| `Assets/Scripts/HoldOrientation.cs` | held-prop orientation convention |
| `Assets/Scripts/FollowCamera.cs` | UNUSED, kept for a possible third-person toggle |
| `Assets/Scripts/Hud.cs` | readouts + crosshair |
| `Assets/Scripts/ScreenshotCapture.cs` | `-screenshot` / `-quitAfter` for headless capture |
| `Assets/Resources/levels/lvl01.json` | the level |

## Controls

WASD move · Space jump · 1 fists · 2 magneto · wheel cycle items · LMB grab/drop ·
RMB freeze/unfreeze · F throw · R respawn · Esc free cursor, click to recapture.

## Current slice

**Slice 2 (in progress): grab/carry/throw end to end, first person, item slots.**
Slice 1 (the loop: plateau -> 24 m canyon -> stairs back up -> far plateau -> goal) is built.
Two planks sit on the start plateau.

Next: **slice 3**, midair freeze + stand on it + re-grab, i.e. leapfrog across the canyon.
This is the go/no-go for the whole game.

Then: **slice 3**, midair freeze + stand on it + re-grab, i.e. leapfrog across the canyon.
This is the go/no-go for the whole game.
