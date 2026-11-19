# Haunted Victorian Mansion: VR Escape Room

COMP 590 Assignment 6. A VR escape room for Meta Quest, built in Unity.

- **Team:** Amit Khanal, Robert Battle, Jay Bhatta
- **Repository:** https://github.com/robertwbattle/COMP590Assignment6
- **Engine:** Unity 6000.5.9f1 (URP)
- **VR:** Meta XR SDK 209 (OVR camera rig) on OpenXR
- **Target:** Meta Quest (Android)
- **Main scene:** `Assets/Scenes/HauntedVictorianMansion.unity`

## Overview

You are shut in the Ashcombe parlour and the front door is barred. A tall candle on the mantel is the timer. Solve three puzzles to break the house's hold, then walk out of the front door into the moonlit garden before the candle burns out.

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
- Arm-swing walking: hold both grips with empty hands and swing your arms as if walking. You move the way you are looking, faster the harder you swing.
- Right thumbstick: snap turn (30 degrees).
- Point a controller and pull the trigger: put out the coffee-table candle.
- Grip near an object: grab it.
- Grip, or pull the trigger while the ray is on an object, from up to 2.25 m away: the object flies to your hand. Release to drop it, or place it in a slot.
- Grip a mirror and twist your wrist left or right: the mirror turns in 15 degree clicks.
- Trigger after the game ends: play again.

## Walkthrough

<details>
<summary>Spoilers: full solution</summary>

**Puzzle 1, the candle.** Every few seconds the candle on the coffee table gutters and flickers a ghostly teal. Put it out (point at it and pull the trigger). The parlour bookcase sinks into the floor and opens a hidden room.

**Puzzle 2, the library.**

- Three loose books (red, slate-grey, green) are spread through both rooms.
- Lean close to the haunted mirror above the fireplace. Three book shapes fade into the glass, showing the order: red, slate, green.
- Shelve the books in that order in the three slots of the cabinet in the hidden room. (The cabinet is against the far wall of the hidden room.)
- A wrong order rattles a lock and buzzes the controllers. The right order opens the cabinet doors and shows a loose mirror pane. A message points out that one of the parlour's standing mirrors is missing its glass.

**Puzzle 3, the moonlight.**

- Moonlight shines through the first window onto a fixed mirror. That mirror bounces it onto a second mirror, which faces the wrong way.
- Turn the second mirror. Put the pane into the third (empty) mirror frame and turn that mirror too, so the beam lands on the moon seal on the other window's bars.
- After a second of moonlight, the seal breaks. Behind you, the iron bars on the front door crash to the floor and the door swings open.

**Escape.** Walk out of the front door, across the porch and down the garden path toward the open gate. Once you are out in the garden you win, and the win screen shows your time and the top-5 leaderboard.

**Losing.** If the mantel candle burns out, you lose. The limit is 8 minutes by default (`timeLimit` on the Escape Game object). As time runs low, the lights dim, you hear whispers and a heartbeat, and the controllers pulse to match.

</details>

## How the project meets the brief

| Requirement | How it is met |
|---|---|
| Theme | Haunted Victorian mansion: a parlour, an entry hall with a barred front door, a hidden library, and a moonlit front garden with an iron gate, dead trees and headstones. Everything is built entirely from primitives with a shared material and lighting style guide (`STYLE_GUIDE.md`). |
| Custom shaders | Three: `Mansion/Haunted Mirror` (a procedural ghost in the fireplace mirror), `Mansion/MirrorWhisper` (a clue that fades in with the viewer's distance, worked out in the shader), and `Mansion/ComfortVignette` (a per-eye field-of-view ring used while moving). |
| Particle systems | Three: rising embers in the fireplace, a wisp of smoke when you snuff the Puzzle 1 candle, and dust drifting in the moonbeam. |
| Three puzzles | See the walkthrough. They must be solved in order to escape, and `EscapeGame.PuzzlesSolved` counts them (shown as "seals n/3"). The puzzles use grabbing, placing objects in sockets, turning in steps, and a moonbeam that reflects off mirrors using raycasts. |
| Game | Win (escape) and lose (candle timer). Also a top-5 fastest-escapes leaderboard on the win screen (saved on the device) and a pocket-watch readout on the left hand showing time left and seals broken. |
| Movement | Three ways to move: room-scale walking (the body capsule follows the headset), thumbstick locomotion with snap turn, and arm-swing walking (after McCullough et al. 2015). A comfort vignette narrows the view only during artificial movement and snap turns (after Fernandes & Feiner 2016). |
| Senses beyond visuals | Spatial 3D sound: ambience, candle snuff, bookcase grind, book thuds, mirror clicks, lock and bars, heartbeat, whispers, and win and lose stings. Controller haptics: grab, snap, mirror clicks, wrong-order buzz, door-unlocking rumble, win/lose pulses, and heartbeat. Two pseudo-haptic weight illusions (see below). |


## Illusions and the papers behind them

| Illusion or technique | Where in the game | Paper |
|---|---|---|
| **Pseudo-haptic weight with a control/display ratio.** The heavy cheval mirrors turn at 0.6 times your wrist's rotation (`controlDisplayRatio` on each mirror's `Turnable`), so they feel heavy to turn. | Puzzle 3 mirrors | Lécuyer, Coquillart, Kheddar, Richard & Coiffet. "Pseudo-Haptic Feedback: Can Isometric Input Devices Simulate Force Feedback?" IEEE VR 2000, pp. 83–90. doi:10.1109/VR.2000.840369 |
| **Weight through perceivable tracking offsets.** Held objects trail and sag slightly behind your real hand in proportion to their weight (`heaviness` on `Grabbable`: books 0.25, mirror pane 0.6), so you instinctively lift heavier things higher. | Books, mirror pane | Rietzler, Geiselhart, Gugenheimer & Rukzio. "Breaking the Tracking: Enabling Weight Perception using Perceivable Tracking Offsets." CHI 2018. doi:10.1145/3173574.3173702 |
| **Dynamic field-of-view restriction.** A soft-edged circular vignette closes in during thumbstick or arm-swing movement and snap turns, and opens when you stop, to reduce VR sickness. It can be turned off on the Escape Game object (`comfortEnabled`). | All artificial movement | Fernandes & Feiner. "Combating VR Sickness through Subtle Dynamic Field-of-View Modification." IEEE 3DUI 2016, pp. 201–210. doi:10.1109/3DUI.2016.7460053 |
| **Arm-swing locomotion.** Swinging your arms walks you forward, which keeps the body involved without needing a large play space. | Movement | McCullough, Xu, Michelson, et al. "Myo Arm: Swinging to Explore a VE." ACM SAP 2015, pp. 107–113. doi:10.1145/2804408.2804416 |

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
- The project has no imported meshes or textures. Every object (furniture, fireplace, portraits, chandeliers, cheval mirrors, library cabinet, candles) was built by the team from Unity primitives with our own materials, emissive glows and two custom shaders, instead of downloading asset-store models.

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

