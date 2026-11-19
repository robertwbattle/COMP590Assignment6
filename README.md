# Haunted Victorian Mansion: VR Escape Room

COMP 590 Assignment 6. A VR escape room for Meta Quest, built in Unity.

- **Team:** Amit Khanal, Robert Battle, Jay Bhatta
- **Repository:** https://github.com/robertwbattle/COMP590Assignment6
- **Engine:** Unity 6000.5.9f1 (URP)
- **VR:** Meta XR SDK 209 (OVR camera rig) on OpenXR
- **Target:** Meta Quest (Android)
- **Main scene:** `Assets/Scenes/HauntedVictorianMansion.unity`

## Overview

You are shut in the Ashcombe parlour. A tall candle on the mantel is the timer. Solve three puzzles, then escape through the moonlit window before the candle burns out.

## Running in the Unity Editor with the Meta XR Simulator

This works on macOS and Windows without a headset.

1. Open the project in Unity 6000.5.9f1 through Unity Hub.
2. Install the standalone Meta XR Simulator app.
3. In Unity, choose **Window > Meta > Meta XR Simulator > Activate**. The console prints `[Meta XR Simulator is activated]`.
4. Open `Assets/Scenes/HauntedVictorianMansion.unity` and press Play. The game appears in the simulator window.
5. In the simulator's Inputs panel, turn on **Keyboard and mouse > Point & click** (Action input: Right). The controller ray follows the mouse, and clicking is the trigger.
6. Keyboard controls: WASD moves, holding the right mouse button looks around, and the arrow keys rotate. The full key list is in the simulator's Input bindings panel.

## Building for Quest

1. In Unity, open **File > Build Profiles** and select the **Mansion Quest** profile (`Assets/Jay/VR/Mansion Quest.asset`). It targets Android and already lists the mansion scene.
2. Connect the headset with developer mode turned on.
3. Click **Build and Run**.

## Quest controls

- Left thumbstick: move.
- Right thumbstick: snap turn (30 degrees).
- Point a controller and pull the trigger: put out the coffee-table candle.
- Grip near an object: grab it.
- Grip, or pull the trigger while the ray is on an object, from up to 2.25 m away: the object flies to your hand. Release to drop it, or place it in a slot.
- Grip a mirror and twist your wrist left or right: the mirror turns in 15 degree clicks.
- Trigger after the game ends: play again.

## Walkthrough

<details>
<summary>Spoilers: full solution</summary>

**Puzzle 1, the candle.** Put out the lit candle on the coffee table. The parlour bookcase sinks into the floor and opens a hidden room.

**Puzzle 2, the library.**

- Three loose books (red, slate-grey, green) are spread through both rooms.
- Lean close to the haunted mirror above the fireplace. Three book shapes fade into the glass, showing the order: red, slate, green.
- Shelve the books in that order in the three slots of the cabinet in the hidden room.
- A wrong order rattles a lock and buzzes the controllers. The right order opens the cabinet doors and shows a silvered mirror pane.

**Puzzle 3, the moonlight.**

- Moonlight shines through the first window onto a fixed mirror. That mirror bounces it onto a second mirror, which faces the wrong way.
- Turn the second mirror. Put the pane into the third (empty) mirror frame and turn that mirror too, so the beam lands on the moon seal on the other window's bars.
- After a second of moonlight, the seal breaks and the bars swing open.

**Escape.** Reach your hand out through the open window to win. The win screen shows your time and your best time.

**Losing.** If the mantel candle burns out, you lose. The limit is 8 minutes by default (`timeLimit` on the Escape Game object). As time runs low, the lights dim, you hear whispers and a heartbeat, and the controllers pulse to match.

</details>

## How the project meets the brief

| Requirement | How it is met |
|---|---|
| Theme | Haunted Victorian mansion, built entirely from primitives with a shared material and lighting style guide (`STYLE_GUIDE.md`). |
| Custom shaders | `Mansion/Haunted Mirror` (a procedural ghost in the fireplace mirror) and `Mansion/MirrorWhisper` (a clue that fades in with the viewer's distance, worked out in the shader). |
| Particle system | Rising Embers in the fireplace. |
| Three puzzles | See the walkthrough. The puzzles use grabbing, placing objects in sockets, turning in steps, and a moonbeam that reflects off mirrors using raycasts. |
| Game | Win (escape) and lose (candle timer). Also a best-time record and a pocket-watch readout on the left hand showing time left and seals broken. |
| Movement | Continuous thumbstick locomotion with snap turn (OVRPlayerController). The body capsule follows the headset for room-scale walking. |
| Senses beyond visuals | Spatial 3D sound: ambience, candle snuff, bookcase grind, book thuds, mirror clicks, lock and bars, heartbeat, whispers, and win and lose stings. Controller haptics: grab, snap, mirror clicks, wrong-order buzz, window-bars rumble, win/lose pulses, and heartbeat. |

## Project layout

- `Assets/MansionKit/`: kit materials, shaders, prefabs, and editor tools (Robert Battle).
- `Assets/Jay/VR/`: VR player rig, movement, candle pointer, and the Quest build profile (Jay Bhatta).
- `Assets/Puzzles/Puzzle1_1.cs`: candle puzzle (Robert Battle).
- `Assets/Amit/`: Puzzles 2 and 3, grab, socket and turn interaction, game loop, timer, UI, audio, and haptics (Amit Khanal). Subfolders are `Scripts/{Interaction,Puzzles,Game,Audio}`, `Prefabs`, and `Audio`.

## Team contributions

- **Robert Battle:** mansion scene, Mansion Kit and style guide, secret room, puzzle 1, window bars.
- **Jay Bhatta:** VR rig, movement, controller pointer, Quest build.
- **Amit Khanal:** puzzles 2 and 3, game loop, sound, haptics, README.

## Testing

- The game was tested in the Unity Editor with scripted play-mode runs of every puzzle, the win and lose states, and restart.
- A full playthrough on a Quest headset is still to be done before submission.
- Editor test shortcuts (hold Shift):
  - Shift+1, Shift+2, Shift+3: solve that puzzle.
  - Shift+T: set the timer to 20 seconds.
  - Shift+W: win. Shift+L: lose. Shift+R: restart.
  - K (no Shift): toggle the secret bookcase.
- These keys are set on the Escape Game object (`debugKeys`). Turn them off for a real playthrough.

## Notes and known limitations

- The style guide forbids colliders on props. The puzzle props are the exception, so they can be grabbed.
- Audio clips are CC0 or self-made (Python-synthesised). See the outside resources below.
- The project has no imported meshes or textures. Everything is built from primitives and project materials.

## Outside resources

The rows below are copied from `CREDITS.md`. Self-made sounds are listed in the notes above, not here.

| Name | Source | License |
|------|--------|---------|
| Meta XR SDK (`com.meta.xr.sdk.all` 209.0.0) | Meta, via Unity Package Manager | Meta Platform Technologies SDK License |
| OpenXR Plugin (`com.unity.xr.openxr` 1.19.0) | Unity Technologies, via Unity Package Manager | Unity Companion License |
| Abandoned passages (horror ambience loop) (`amb_mansion_loop.ogg`, from `freezer_0.ogg`) | https://opengameart.org/content/abandoned-passages-horror-ambience-loop (congusbongus) | CC0 |
| 100 CC0 metal and wood SFX (`sfx_book_thud_1/2`, `sfx_mirror_click`, `sfx_cupboard_unlock`, `sfx_bars_open_2`) | https://opengameart.org/content/100-cc0-metal-and-wood-sfx (Barina) | CC0 |
| Iron Door (`sfx_bars_open_1`) | https://opengameart.org/content/iron-door (TheMightyGlider) | CC0 |
| Door Lock Sounds (`sfx_wrong_order`, `sfx_seal_unlock`) | https://opengameart.org/content/door-lock-sounds (davesch) | CC0 |

## Demo and reflection video

Demo and reflection video: (link to be added)

An optional idea for the demo: a helper switches on a fan when the window opens, to suggest cold night air.
