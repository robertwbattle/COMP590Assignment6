@AGENTS.md

# Project: Haunted Victorian Mansion (Unity 6000.5, URP)

Every new scene must look like another room of `Assets/Scenes/HauntedVictorianMansion.unity`. **Read `STYLE_GUIDE.md` before you create or change a scene.**

Hard rules (the full reasoning and numbers are in the guide):
- Build with primitives (cube, sphere, cylinder) and **only** the materials in `Assets/MansionKit/Materials`. No textures or imported meshes. Add a new material only following §4 of the guide.
- Reuse the prefabs in `Assets/MansionKit/Prefabs`. Start new rooms with **Tools ▸ Mansion Kit ▸ New Room Scene**.
- Lighting: one warm key light (intensity 10–13), other point lights ≤ 5, one cool moonlight counterpoint. Warm lights must have a visible source. No neutral white light. Flat ambient `#6A5D6A`. Fog off.
- Camera: eye height 1.64 m, FOV 59, far 30, solid `#191619` background.
- 1 unit = 1 m. Pivots at floor level, fronts face −Z. Ceiling 5.10 m. Don't use non-uniform scale on parents.
- Put one quiet uncanny detail per room.
- Finish by running **Tools ▸ Mansion Kit ▸ Validate Active Scene** with 0 errors.
- Place kit prefabs at y = 0. Hung pieces (window, portrait, chandelier) already include their hang height.
- Material and shader moves must keep their `.meta` files. Scenes reference assets by GUID.
- The Editor can be driven with `unity command script eval`. The kit's scripted entry points are listed in STYLE_GUIDE.md §10. They never open dialogs.
