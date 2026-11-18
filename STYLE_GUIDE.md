# Mansion Kit Style Guide

The reference scene is `Assets/Scenes/HauntedVictorianMansion.unity`, a candlelit Victorian parlour with an entry hall. Every new scene should look like another room in the same house. This guide records what the reference scene does, with real numbers taken from its files. The reusable pieces live in `Assets/MansionKit/`.

---

## 1. Look and feel

**Warm and lived-in, slightly wrong.** The room is cosy at first glance: velvet, walnut, brass and candlelight. The haunting comes from small details rather than gore or monsters:

- a ghost visible only in a clouded mirror,
- a portrait hung a few degrees crooked (West Portrait is rolled 6° on Z),
- cold moonlight at the windows pushing against warm firelight,
- embers rising from a hearth nobody is tending.

**Rules that follow from this:**

1. Every room has at least one **warm practical light**: a candle, chandelier, lamp or hearth.
2. Every room has one **cold counterpoint**: moonlit glass, the mirror's teal, or a cool fill.
3. Put in **one quiet uncanny detail** per room, not five. Something tilted, something reflected, something that moves when nothing should.
4. Keep it dark, but readable. The far corners stay dim, and the focal object is always lit.

---

## 2. How the scene is built

The whole scene is **kitbashed from Unity primitives with flat-colour materials.** It has no textures, imported meshes or normal maps.

| Primitive | Count | Used for |
|---|---|---|
| Cube | 430 | walls, trim, panels, furniture bodies, floorboards, books |
| Sphere | 42 | flames, candle cups, cushion caps, tuft buttons, portrait faces, knobs |
| Cylinder | 39 | candles, chains, chandelier arms, table legs, rolled sofa arms, logs, curtain rods |

How detail is made:

- **Layer thin slabs** 1–3 cm in front of a surface: wall, then wainscot, then inset panel, then inset field. Each layer sits about 2 cm further toward the room (−Z local) and uses a different shade of wood.
- **Trim everything.** Each surface change gets a band: baseboard, chair rail, crown molding, plus a brass bead under the crown.
- **Repeat with rhythm.** Inset panels are spaced about 1.66 m apart, sofa buttons 0.42 m, floorboards 0.45 m.
- **Use brass as punctuation.** Small brass accents go on almost every object: nameplates, handles, knobs, gilt book titles, curtain ties, ceiling plates.
- Keep the **shapes simple and the silhouettes Victorian**: rolled arms, tapered legs, pilasters with plinth and capital, a mantel shelf wider than the fireplace.

**Don't:** import realistic or photo-textured meshes, use stock textures, or use the default grey material. If something needs a new material, see §4.

---

## 3. Palette

Values are copied from `Assets/MansionKit/Materials`. *Uses* is how many renderers in the reference scene use each material, which shows how much of each colour belongs in a room.

| Material | Hex | Smooth. | Uses | Role |
|---|---|---|---|---|
| **Wood** | | | | |
| `M_Wood_Trim` | `#623B28` | 0.30 | 119 | moldings, panels, sills, door panels (the most-used colour) |
| `M_Wood_Walnut` | `#422519` | 0.30 | 75 | furniture bodies, frames, wainscot, inset fields (the dark base) |
| `M_Wood_FloorMid` | `#60402C` | 0.19 | 39 | the main floorboard shade |
| `M_Wood_FloorLight` | `#6B4630` | 0.19 | 19 | floorboard accent |
| `M_Wood_FloorDark` | `#543626` | 0.19 | 18 | floorboard accent |
| **Metal** | | | | |
| `M_Metal_AgedBrass` | `#A37D43` | 0.42 | 75 | all metal. It is non-metallic (`_Metallic` 0) on purpose, so it reads as old brass rather than shiny chrome |
| **Fabric** | | | | |
| `M_Fabric_RedVelvet` | `#782630` | 0.08 | 33 | upholstery, curtains, book covers |
| `M_Fabric_RoseVelvet` | `#99414A` | 0.08 | 24 | cushions, buttons, arm caps (a lighter red for highlights) |
| `M_Fabric_GoldBinding` | `#C09766` | 0.07 | 18 | rug borders and medallions |
| `M_Fabric_Burgundy` | `#5E2837` | 0.07 | 5 | rug field, the curtain folds furthest back |
| `M_Fabric_GreenBookcloth` | `#354D42` | 0.19 | 8 | books (the only cool non-glowing accent) |
| `M_Fabric_SlateCloth` | `#313842` | 0.19 | 6 | painted clothing |
| **Surfaces** | | | | |
| `M_Surface_PlumWallpaper` | `#50313E` | 0.19 | 9 | walls |
| `M_Surface_Plaster` | `#65534C` | 0.19 | 2 | ceilings |
| `M_Surface_Limestone` | `#A69580` | 0.19 | 7 | fireplace surround, hearth |
| `M_Surface_SootIron` | `#211B20` | 0.19 | 11 | firebox, grate, painted hair (near-black) |
| `M_Surface_Ivory` | `#DFC8A0` | 0.19 | 23 | candle wax, book pages, collars, fine moldings (the lightest non-glowing colour) |
| **Paint** | | | | |
| `M_Paint_OliveBackdrop` | `#383C35` | 0.19 | 2 | portrait backgrounds |
| `M_Paint_Flesh` | `#AC8062` | 0.19 | 2 | portrait faces |
| **Emissive** | | | | |
| `M_Emissive_CandleFlame` | base `#F3BD5C`, emission HDR (2.0, 1.40, 0.58) | 0.30 | 12 | every flame |
| `M_Emissive_Ember` | base `#AB4426`, emission HDR (1.35, 0.45, 0.14) | 0.20 | 1 | glowing logs and coals |
| `M_Emissive_MoonlitGlass` | base `#243D56`, emission `#2F4659` | 0.37 | 2 | window panes |
| **FX** | | | | |
| `M_FX_HauntedMirror` | glass `#527470` | n/a | 1 | the `Mansion/Haunted Mirror` shader, with a procedural ghost |
| `M_FX_EmberParticle` | `#FFB254` | n/a | 1 | ember particles |

**Colour rules**

- **Value structure:** dark walls and floor (≈ 20–40 % brightness), mid-value red fabric, and small bright accents (brass, ivory, flame). Nothing outside the emissives is brighter than Ivory `#DFC8A0`.
- **Hue families:** browns (wood), reds and purples (fabric, wallpaper), gold (brass, binding), plus a few cool notes (green bookcloth, slate, moonlit blue, mirror teal). **No saturated primaries. No pure white or pure black.**
- **Rough proportions by surface area:** about 55 % wood and wallpaper, 25 % red fabric, 10 % stone, plaster and ivory, 7 % brass, 3 % cool accents and glow.
- **Smoothness has three bands:** fabric 0.07–0.08 (velvet soaks up light), most surfaces 0.19, polished wood 0.30, brass 0.42. Don't go above 0.45 unless it is glass.

---

## 4. Materials

- **Reuse before you add.** The reference scene reuses materials across object types: Ivory is also pages and collars, Soot Iron is also hair, Red Velvet is also book covers. Do the same.
- When you really need a new material:
  - Use **URP/Lit**, Specular workflow, no textures, `_Metallic` 0.
  - Name it `M_<Category>_<Name>` and put it in `Assets/MansionKit/Materials/<Category>/`.
  - Pick its colour inside an existing hue family (§3) and its smoothness from the bands above.
  - Add a row to the palette table in this guide.
- **Emissive materials must have the `_EMISSION` keyword on.** In URP, setting an emission colour is not enough. In the reference scene all three emissives had their colours set but the keyword off, so the flames didn't glow. That is now fixed in the kit materials.

---

## 5. Lighting

The reference light rig uses realtime lights only, with **shadows off on every light**.

| Light | Type | Colour | Intensity | Range | Role |
|---|---|---|---|---|---|
| Ambient fill | Directional | `#AAA6C9` cool lavender | 0.2 | n/a | soft moonlit fill, angled down through the windows |
| Chandelier warm light | Point | `#FFCF98` | 12.5 | 11.2 | **key light**, 3.26 m up, room centre |
| Mantel fill | Point | `#FFCB9E` | 4.4 | 6.2 | lifts the focal wall (fireplace, portraits) |
| Hearth light | Point | `#FF823F` ember orange | 3.0 | 5.4 | low, in front of the firebox |
| Entry lamp | Point | `#F8CC9A` | 4.5 | 4.2 | small secondary space |
| Cool window light | Point | `#92A9EC` moon blue | 5.4 | 5.8 | 0.85 m inside one window |

**Environment:** ambient mode is **Flat `#6A5D6A`** (a dusty mauve), reflection intensity 0.3, **fog off**, no skybox.

**Lighting rules**

1. **One key light per room:** a chandelier or big practical at 10–13 intensity. Put every other point light at ≤ 5.
2. **Every warm light is visible.** Each warm light has a flame, lamp or ember the player can see at its source.
3. **One cool light per room,** coming from where moonlight would come in (a window, a mirror, a doorway to outside).
4. **Light colours:** warm lights have r ≥ g ≥ b. Cool lights are blue-lavender. Never use neutral white. The validator checks this.
5. Keep **4 or fewer point lights touching any one object.** The URP asset's per-object limit is 4.
6. Shadows are off in the reference scene. If a new scene turns them on, turn them on only for the key light, with soft shadows. Don't mix shadowed and unshadowed rooms in one build.

---

## 6. Camera and post-processing

- The camera is at standing eye height, **1.64 m**, tilted up about 1.75°. **FOV 59**, near 0.07, far **30** (rooms are small, so nothing past 30 m matters).
- Clear to a **solid `#191619`**. HDR is on and MSAA is allowed.
- Post-processing is **off** on the camera, and the scene has no Volume. To add it, make one shared Volume profile under `Assets/MansionKit/Settings/` and use it in every scene: subtle bloom (to bring out the now-emissive flames), ACES tonemapping and a light vignette. Don't make one-off grading per scene.

---

## 7. Scale and dimensions

The scene uses 1 unit = 1 m. Pivots are **at floor level**, and objects **face −Z** (toward a default camera). Prefabs made by the kit tool follow both rules.

| Element | Size |
|---|---|
| Ceiling height | **5.10 m** (grand parlour). Entry hall ceiling is 3.70 m |
| Parlour footprint | 13.4 × 11.6 m |
| Wall thickness | 0.16 m |
| Wainscot / chair rail | 1.06 m / 1.07 m |
| Crown molding | top at 5.06 m |
| Floorboards | 0.45 m wide, 1.6–4.2 m long, staggered, mostly the mid shade |
| Door opening | about 1.3 × 2.4 m, casing 1.6 × 2.5 m |
| Sofa | 2.9 m long, seat at 0.58 m |
| Coffee table | top at 0.63 m |
| Sideboard | top at 1.0 m |
| Bookcase | 2.0 × 3.0 m |
| Mantel shelf | 1.9 m (after the prefab's 1.22 scale) |
| Chandelier candles | about 3.9 m above the floor |

---

## 8. Effects

- **Rising Embers** (particle system): Cone emitter at 10°, 10–11 particles/s, max 30, lifetime 0.55–1.4 s, speed 0.34–0.76, size 0.012–0.033, no gravity, `M_FX_EmberParticle`. Reuse it for braziers, candles in draughts, and censers.
- **Haunted Mirror shader** (`Mansion/Haunted Mirror`): procedural haze, sheen and a veiled figure with eyes, all from UV. It takes one parameter, `_GlassColor`. Use it on any mirror, or swap it onto a window pane for a second-floor apparition. Keep the glass colour in the teal family.

---

## 9. Hierarchy and naming

- **Group roots by purpose:** `Room` (or `Room and Entry Hall`), `<Room> Furniture`, `Room Lighting`, focal set-pieces at root, `Player Start` containing the camera.
- **Name objects in plain English for what they are:** `Rolled back rail`, `Gilt spine band`, `Chair rail`. Don't leave names like `Cube (14)`.
- **Assets:** `M_<Category>_<Name>` for materials, `P_<Name>` for prefabs, scenes in PascalCase (`HauntedVictorianMansion`).
- **Colliders** go only on large blocking or walkable boxes: floors, walls, sofa bodies, tables, the hearth. The reference scene has 43 colliders across 511 renderers. Trim and props don't get colliders.
- **Avoid non-uniform scale on a parent.** It squashes child spheres and makes trim thicknesses uneven. Size the children instead. The reference scene does this on its walls, portraits, windows and fireplace, and the validator warns about it.

---

## 10. Kit contents (`Assets/MansionKit/`)

```
MansionKit/
  Materials/{Wood,Metal,Fabric,Surface,Paint,Emissive,FX}/M_*.mat
  Shaders/HauntedMirror.shader
  Prefabs/                (generated, see below)
    Architecture/ P_Door_Paneled, P_Pilaster, P_Window_Moonlit, P_Fireplace_HauntedMirror
    Furniture/    P_Sofa_Chesterfield, P_CoffeeTable, P_Bookcase, P_Sideboard, P_Rug_Patterned
    Decor/        P_Portrait
    Props/        P_Book_Closed
    Lighting/     P_Chandelier (with its light), P_Candle
  Editor/MansionKitTools.cs
```

**Editor menu: Tools ▸ Mansion Kit**

| Command | What it does |
|---|---|
| **Extract Prefabs From Reference Scene** | Opens the reference scene and saves the set-pieces above as prefabs. Each pivot sits on the floor under the piece, and each front faces −Z. It doesn't change the scene. Run it again after editing the reference scene. |
| **New Room Scene…** | Builds a new scene at any width and depth with the kit's room shell: floor, plaster ceiling, 4 walls with wallpaper, wainscot, panels and moldings. It also adds the light rig (moon fill plus the `P_Chandelier` prefab as the key), the environment and the camera, all matching the reference. |
| **Validate Active Scene** | Reports materials from outside the kit, emissives with no glow, off-palette light colours, light-intensity outliers, wrong ambient or camera settings, and non-uniform parent scale. |

**Placing prefabs.** Wall-hung and ceiling-hung pieces keep the height they had in the mansion, so place every prefab at **y = 0**. The window, portrait and chandelier will then hang at the right height (window sill about 1.5 m, portrait bottom about 2.0 m, chandelier bottom about 3.65 m). Wall pieces sit against a wall whose room side is at local z ≈ 0. Rotate them so −Z faces into the room.

`P_Fireplace_HauntedMirror`, `P_Portrait` and `P_Window_Moonlit` keep their original root scale (for example 1.22, 1.22, 1.0). So the validator's non-uniform-scale warning will appear for them in any scene. That warning is expected for these three, and nothing to fix.

**From a script or agent** (no dialogs, refuses to discard unsaved changes):

```csharp
MansionKit.EditorTools.MansionKitPrefabs.ExtractAll();                        // returns the list of prefab paths
MansionKit.EditorTools.MansionKitRoomBuilder.CreateScene("Library", 9f, 7f, false);
var (errors, warnings) = MansionKit.EditorTools.MansionKitValidator.Validate(); // details in MansionKitValidator.LastReport
```

The project has Unity's Pipeline package (`com.unity.pipeline`), so an agent can run these against the open Editor with `unity command script eval --code '…'`.

---

## 11. Prompt template for new scenes

Paste this into a prompt and fill in the brackets:

> Build a new room for the haunted Victorian mansion: **[room name, e.g. "the library" / "the master bedroom"]**, about **[W × D]** m.
> Follow `STYLE_GUIDE.md`. Start with **Tools ▸ Mansion Kit ▸ New Room Scene**. Furnish it from `Assets/MansionKit/Prefabs`. Build any new pieces from primitives, using only kit materials.
> Focal point: **[e.g. a four-poster bed / a reading desk under a window]**. Warm key light: **[chandelier / candelabra / hearth]**. Cool counterpoint: **[moonlit window / mirror]**.
> One uncanny detail: **[e.g. a rocking chair angled toward an empty corner / a second mirror ghost / a portrait whose eyes are ivory]**.
> Finish by running **Validate Active Scene** with 0 errors.

---

## 12. Known gaps in the reference scene

| Issue | Status |
|---|---|
| The emissive materials had the `_EMISSION` keyword off, so flames, embers and moonlit glass didn't glow | **Fixed** in the kit materials |
| No post-processing and no Volume | Open. See §6 before adding it |
| Non-uniform scale on the wall, portrait, window and fireplace parents | Open. Leave it as is, but don't copy it into new rooms. Validating the mansion gives 0 errors and 3 warnings (the fireplace and the two portraits) |
| All lights are realtime with no shadows, and nothing is baked | Fine for now. If you bake lighting, bake every room in the build |
