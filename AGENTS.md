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
./tools/verify.sh     # batchmode compile, greps errors out of the log
./tools/build.sh      # Linux player -> build/stupidgame.x86_64
./tools/shot.sh 4     # run player under xvfb, screenshot after 4s
```

The editor holds a lock on the project. `verify.sh` / `build.sh` / `shot.sh` fail while it
is open — close the editor, run them, reopen it to play.

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
| `Assets/Scripts/FollowCamera.cs` | third-person orbit |
| `Assets/Scripts/Hud.cs` | readouts + crosshair |
| `Assets/Scripts/ScreenshotCapture.cs` | `-screenshot` / `-quitAfter` for headless capture |
| `Assets/Resources/levels/lvl01.json` | the level |

## Current slice

**Slice 1 (in progress): the loop.** Start plateau -> 24 m canyon -> far plateau -> mountain
steps -> goal. Two planks on the start plateau. Falling drops you to the canyon floor and a
staircase back up. No bridging is required yet — this slice only proves the loop, the
camera, and the fall-back path.

Next: **slice 2**, grab/carry/throw works end to end.

Then: **slice 3**, midair freeze + stand on it + re-grab, i.e. leapfrog across the canyon.
This is the go/no-go for the whole game.
