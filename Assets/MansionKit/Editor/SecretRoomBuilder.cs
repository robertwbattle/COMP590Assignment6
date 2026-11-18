// Builds the secret room behind the parlour bookcase in HauntedVictorianMansion.unity.
//   Tools > Mansion Kit > Build Secret Room (Mansion)
// Run it once on a scene that doesn't have it yet; it refuses if "Secret Room" already exists.
// Everything it changes in the existing scene:
//   * West wall: wallpaper, wainscot, baseboard and chair rail are split around a doorway; the panel in it is removed.
//   * Living room floor collider: split into three slabs around the hole under the bookcase.
//   * Floorboards: the five rows under the bookcase are cut at the hole's front edge.
//   * Parlor Bookcase: gets a kinematic Rigidbody and a collider on its crown, so its top can be walked on once sunk.
// Everything it adds: "Secret Room" (the room), "Secret Bookcase" (hatch, void, barrier and the SecretBookcase script).

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MansionKit.EditorTools
{
    public static class SecretRoomBuilder
    {
        // Hole under the bookcase (world). Z edges sit on existing floorboard seams; X runs from the wall's far face
        // to just in front of the bookcase.
        const float HoleX0 = -6.77f, HoleX1 = -5.68f, HoleZ0 = 2.088f, HoleZ1 = 4.408f;
        // Doorway behind the bookcase (hidden by it: bookcase is z 2.13..4.30, 3.15 m tall).
        const float DoorZ0 = 2.39f, DoorZ1 = 4.06f, DoorHeight = 2.7f;
        // Secret room: 3/4 of the parlour (13.4 x 11.6) per side, beyond the west wall, centred on the parlour's Z.
        const float RoomX1 = -6.77f, RoomWidth = 10.05f, RoomDepth = 8.7f;
        const float RoomX0 = RoomX1 - RoomWidth;
        // Pocket under the secret room floor that the hatch slides into.
        const float HatchSlide = 1.12f, PocketX0 = HoleX0 - HatchSlide - 0.06f;

        const float BoardTop = 0.002f, BoardBottom = -0.010f, SlabTop = -0.010f, SlabBottom = -0.21f;
        const float Seam = 0.012f;
        // How far the bookcase sinks. Its crown (top at 3.145) ends 3.5 cm below the floorboard tops, leaving
        // a deliberate step and gap around it.
        const float SinkDepth = 3.18f;
        const float HatchDrop = 0.035f, BackingDepth = 0.02f;
        const float PitBottom = -3.6f;
        const float RoomLightIntensity = 4f, RoomLightRange = 6f;
        const uint ParlourLayer = 1u, SecretLayer = 2u; // rendering layer bits

        public const string VoidMaterialPath = MansionKitStyle.MaterialRoot + "/FX/M_FX_Void.mat";

        [MenuItem("Tools/Mansion Kit/Build Secret Room (Mansion)")]
        static void BuildMenu()
        {
            var msg = Build();
            EditorUtility.DisplayDialog("Mansion Kit", msg, "OK");
        }

        public static string Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != MansionKitStyle.ReferenceScene) return $"Open {MansionKitStyle.ReferenceScene} first.";
            if (GameObject.Find("Secret Room") != null) return "This scene already has a Secret Room.";

            var bookcase = GameObject.Find("Parlor Furniture/Parlor Bookcase");
            var wall = GameObject.Find("Room and Entry Hall/West wall");
            var slab = GameObject.Find("Room and Entry Hall/Living room floor collider");
            var boards = GameObject.Find("Room and Entry Hall/Floorboards");
            if (!bookcase || !wall || !slab || !boards) return "Missing a bookcase, west wall, floor collider or floorboards object.";

            CutDoorway(wall.transform);
            SplitSlab(slab);
            var rows = CutFloorboards(boards.transform);

            var mech = new GameObject("Secret Bookcase").transform;
            var hatch = BuildHatch(mech, rows);
            BuildVoid(mech);
            var barrier = Box("Invisible barrier", mech, new Vector3((HoleX0 + HoleX1) / 2, 1.5f, (HoleZ0 + HoleZ1) / 2),
                new Vector3(HoleX1 - HoleX0 + 0.3f, 3f, HoleZ1 - HoleZ0 + 0.3f), null, true);
            Object.DestroyImmediate(barrier.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(barrier.GetComponent<MeshFilter>());
            barrier.GetComponent<Collider>().enabled = false;

            MakeTopWalkable(bookcase.transform);
            if (!bookcase.TryGetComponent<Rigidbody>(out var rb)) rb = bookcase.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var lights = BuildRoom();

            var sb = mech.gameObject.AddComponent<SecretBookcase>();
            sb.bookcase = bookcase.transform;
            sb.floorHatch = hatch;
            sb.barrier = barrier.GetComponent<Collider>();
            sb.roomLights = lights.ToArray();
            sb.sinkDepth = SinkDepth;
            sb.hatchDrop = HatchDrop;
            sb.hatchSlide = new Vector3(-HatchSlide, 0, 0);
            sb.roomLightIntensity = RoomLightIntensity;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return $"Secret room built. Bookcase sinks {sb.sinkDepth:F3} m; press K in Play mode to toggle.";
        }

        // ---------- Existing scene edits ----------

        static void CutDoorway(Transform wall)
        {
            float a = wall.InverseTransformPoint(new Vector3(wall.position.x, 0, DoorZ0)).x;
            float b = wall.InverseTransformPoint(new Vector3(wall.position.x, 0, DoorZ1)).x;
            float lo = Mathf.Min(a, b), hi = Mathf.Max(a, b);
            var children = new List<Transform>();
            foreach (Transform c in wall) children.Add(c);
            foreach (var c in children)
            {
                float cx = c.localPosition.x, half = c.localScale.x / 2, bottom = c.localPosition.y - c.localScale.y / 2;
                if (cx + half <= lo || cx - half >= hi || bottom >= DoorHeight) continue; // not in the opening
                if (c.name.StartsWith("Inset")) { Object.DestroyImmediate(c.gameObject); continue; }
                if (cx - half < lo) CloneSpan(c, cx - half, lo);
                if (cx + half > hi) CloneSpan(c, hi, cx + half);
                if (c.name == "Wallpaper")
                {
                    var lintel = CloneSpan(c, lo, hi);
                    lintel.name = "Wallpaper lintel";
                    float top = c.localPosition.y + c.localScale.y / 2;
                    lintel.localPosition = new Vector3(lintel.localPosition.x, (DoorHeight + top) / 2, lintel.localPosition.z);
                    lintel.localScale = new Vector3(lintel.localScale.x, top - DoorHeight, lintel.localScale.z);
                }
                Object.DestroyImmediate(c.gameObject);
            }
            // The secret room's chandeliers light the far side of the parlour wallpaper too.
            foreach (Transform c in wall)
                if (c.name.StartsWith("Wallpaper")) c.GetComponent<Renderer>().renderingLayerMask = ParlourLayer | SecretLayer;
        }

        static Transform CloneSpan(Transform src, float from, float to)
        {
            var c = Object.Instantiate(src.gameObject, src.parent).transform;
            c.name = src.name;
            c.SetSiblingIndex(src.GetSiblingIndex() + 1);
            c.localPosition = new Vector3((from + to) / 2, src.localPosition.y, src.localPosition.z);
            c.localScale = new Vector3(to - from, src.localScale.y, src.localScale.z);
            return c;
        }

        static void SplitSlab(GameObject slab)
        {
            var b = slab.GetComponent<Renderer>().bounds;
            SetWorldBox(Object.Instantiate(slab, slab.transform.parent), "Living room floor (south)", b.min.x, b.max.x, b.min.y, b.max.y, b.min.z, HoleZ0);
            SetWorldBox(Object.Instantiate(slab, slab.transform.parent), "Living room floor (north)", b.min.x, b.max.x, b.min.y, b.max.y, HoleZ1, b.max.z);
            SetWorldBox(Object.Instantiate(slab, slab.transform.parent), "Living room floor (east of hatch)", HoleX1, b.max.x, b.min.y, b.max.y, HoleZ0, HoleZ1);
            Object.DestroyImmediate(slab);
        }

        // Shortens every board in the hole's rows so it stops at the hole's front edge. Returns those rows (z range + material).
        static List<(float z0, float z1, Material m)> CutFloorboards(Transform boards)
        {
            var rows = new List<(float, float, Material)>();
            var kids = new List<Transform>();
            foreach (Transform c in boards) kids.Add(c);
            foreach (var c in kids)
            {
                var r = c.GetComponent<Renderer>();
                var b = r.bounds;
                if (b.center.z < HoleZ0 || b.center.z > HoleZ1 || b.min.x >= HoleX1) continue;
                if (!rows.Exists(p => Mathf.Abs(p.Item1 - b.min.z) < 0.005f)) rows.Add((b.min.z, b.max.z, r.sharedMaterial));
                if (b.max.x <= HoleX1 + Seam) { Object.DestroyImmediate(c.gameObject); continue; }
                float lx = boards.InverseTransformPoint(new Vector3(HoleX1 + Seam / 2, 0, 0)).x;
                float rx = c.localPosition.x + c.localScale.x / 2;
                c.localPosition = new Vector3((lx + rx) / 2, c.localPosition.y, c.localPosition.z);
                c.localScale = new Vector3(rx - lx, c.localScale.y, c.localScale.z);
            }
            rows.Sort((p, q) => p.Item1.CompareTo(q.Item1));
            return rows;
        }

        // ---------- New mechanism parts ----------

        static Transform BuildHatch(Transform parent, List<(float z0, float z1, Material m)> rows)
        {
            var hatch = new GameObject("Floor Hatch").transform;
            hatch.SetParent(parent, false);
            hatch.position = new Vector3((HoleX0 + HoleX1) / 2, 0, (HoleZ0 + HoleZ1) / 2);
            var walnut = MansionKitStyle.Mat("Wood", "Walnut");
            SetWorldBox(Box("Hatch backing", hatch, Vector3.zero, Vector3.one, walnut, true), null,
                HoleX0, HoleX1, BoardBottom - BackingDepth, BoardBottom, HoleZ0, HoleZ1);
            foreach (var (z0, z1, m) in rows)
                SetWorldBox(Box("Floorboard", hatch, Vector3.zero, Vector3.one, m, false), null,
                    HoleX0, HoleX1 - Seam / 2, BoardBottom, BoardTop, z0, z1);
            var rb = hatch.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            return hatch;
        }

        static void BuildVoid(Transform parent)
        {
            var mat = VoidMaterial();
            var v = new GameObject("Void").transform;
            v.SetParent(parent, false);
            const float t = 0.1f, top = SlabBottom + 0.14f; // just under the hatch's lowest point
            void Side(string n, float x0, float x1, float z0, float z1)
            {
                var g = Box(n, v, Vector3.zero, Vector3.one, mat, false);
                SetWorldBox(g, null, x0, x1, PitBottom, Mathf.Min(top, BoardBottom - BackingDepth - HatchDrop - 0.005f), z0, z1);
                g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // Inner faces sit 2 mm inside the hole so they never share a plane with the slabs' cut faces.
            const float e = 0.002f;
            Side("Void east", HoleX1 - e, HoleX1 + t, HoleZ0 - t, HoleZ1 + t);
            Side("Void west", HoleX0 - t, HoleX0 + e, HoleZ0 - t, HoleZ1 + t);
            Side("Void south", HoleX0, HoleX1, HoleZ0 - t, HoleZ0 + e);
            Side("Void north", HoleX0, HoleX1, HoleZ1 - e, HoleZ1 + t);
            var floor = Box("Void floor", v, Vector3.zero, Vector3.one, mat, false);
            SetWorldBox(floor, null, HoleX0 - t, HoleX1 + t, PitBottom - t, PitBottom, HoleZ0 - t, HoleZ1 + t);
        }

        // The bookcase's own top is the floor once it has sunk: give the crown a collider so it can be walked on.
        static void MakeTopWalkable(Transform bookcase)
        {
            var crown = bookcase.Find("Bookcase crown");
            if (crown != null && !crown.TryGetComponent<BoxCollider>(out _)) crown.gameObject.AddComponent<BoxCollider>();
        }

        // ---------- The room ----------

        static List<Light> BuildRoom()
        {
            var room = new GameObject("Secret Room").transform;
            var walnut = MansionKitStyle.Mat("Wood", "Walnut");
            float cx = (RoomX0 + RoomX1) / 2, hz = RoomDepth / 2, h = MansionKitStyle.CeilingHeight;

            // Floor slabs, leaving the pocket the hatch slides into a few cm lower.
            SetWorldBox(Box("Floor", room, Vector3.zero, Vector3.one, walnut, true), null, RoomX0, PocketX0, SlabBottom, SlabTop, -hz, hz);
            SetWorldBox(Box("Floor (beside pocket)", room, Vector3.zero, Vector3.one, walnut, true), null, PocketX0, RoomX1, SlabBottom, SlabTop, -hz, HoleZ0);
            float pocketTop = BoardBottom - BackingDepth - HatchDrop - 0.005f;
            SetWorldBox(Box("Hatch pocket", room, Vector3.zero, Vector3.one, walnut, false), null, PocketX0, RoomX1, SlabBottom, pocketTop, HoleZ0, hz);
            // Walkable surface over the pocket, above the hatch's path (collider only).
            var walk = Box("Pocket walk surface", room, Vector3.zero, Vector3.one, null, true);
            Object.DestroyImmediate(walk.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(walk.GetComponent<MeshFilter>());
            SetWorldBox(walk, null, PocketX0, RoomX1, BoardBottom - 0.015f, BoardBottom, HoleZ0, hz);

            BuildFloorboards(room, -hz, hz);
            SetWorldBox(Box("Plaster ceiling", room, Vector3.zero, Vector3.one, MansionKitStyle.Mat("Surface", "Plaster"), true), null,
                RoomX0, RoomX1, h, h + 0.18f, -hz, hz);

            MansionKitRoomBuilder.Wall("North wall", room, new Vector3(cx, 0, hz), 0f, RoomWidth);
            MansionKitRoomBuilder.Wall("South wall", room, new Vector3(cx, 0, -hz), 180f, RoomWidth);
            MansionKitRoomBuilder.Wall("West wall", room, new Vector3(RoomX0, 0, 0), -90f, RoomDepth);
            // East side: the parlour's west wall is the shared wall, so only trim on this side, with the doorway cut.
            // Yaw 90 maps local +X to world -Z, so the doorway's local span is the negated world span.
            MansionKitRoomBuilder.Wall("East wall trim", room, new Vector3(-6.69f, 0, 0), 90f, RoomDepth,
                wallpaper: false, gapMin: -DoorZ1, gapMax: -DoorZ0, gapHeight: DoorHeight);

            // Four chandeliers on a 2 x 2 grid at the quarter points.
            var lights = new List<Light>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{MansionKitStyle.PrefabRoot}/Lighting/P_Chandelier.prefab");
            int i = 0;
            foreach (float x in new[] { cx + RoomWidth / 4, cx - RoomWidth / 4 })
                foreach (float z in new[] { -RoomDepth / 4, RoomDepth / 4 })
                {
                    var c = (GameObject)PrefabUtility.InstantiatePrefab(prefab, room);
                    c.name = $"Chandelier {++i}";
                    c.transform.position = new Vector3(x, 0, z);
                    var l = c.GetComponentInChildren<Light>();
                    l.range = RoomLightRange;
                    l.intensity = 0f;
                    l.enabled = false;
                    // URP reads a light's layers from its additional data (Light.renderingLayerMask only drives shadows).
                    l.GetUniversalAdditionalLightData().renderingLayers = SecretLayer;
                    lights.Add(l);
                }

            // Secret room geometry is lit only by its own lights (the parlour's lights would otherwise reach it through the wall).
            foreach (var r in room.GetComponentsInChildren<Renderer>(true)) r.renderingLayerMask = SecretLayer;
            return lights;
        }

        // Board rows continue the parlour's seams (0.464 m pitch starting at z = -5.791) through the doorway.
        static void BuildFloorboards(Transform room, float zMin, float zMax)
        {
            var parent = new GameObject("Floorboards").transform;
            parent.SetParent(room, false);
            var shades = new[] { MansionKitStyle.Mat("Wood", "FloorDark"), MansionKitStyle.Mat("Wood", "FloorMid"), MansionKitStyle.Mat("Wood", "FloorLight") };
            var rng = new System.Random(591);
            const float pitch = 0.464f, width = 0.446f, start = -5.791f;
            for (float z0 = start; z0 < zMax; z0 += pitch)
            {
                float a = Mathf.Max(z0, zMin), b = Mathf.Min(z0 + width, zMax);
                if (b - a < 0.05f) continue;
                float x = RoomX0;
                while (x < RoomX1 - 0.05f)
                {
                    float len = Mathf.Min(1.6f + (float)rng.NextDouble() * 2.6f, RoomX1 - x);
                    var m = shades[new[] { 0, 1, 1, 2 }[rng.Next(4)]];
                    SetWorldBox(Box("Floorboard", parent, Vector3.zero, Vector3.one, m, false), null, x, x + len - Seam, BoardBottom, BoardTop, a, b);
                    x += len;
                }
            }
        }

        // ---------- Helpers ----------

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider) =>
            MansionKitRoomBuilder.Box(name, parent, pos, size, mat, collider);

        // Places an axis-aligned box by its world-space extents (parent must be unrotated and unscaled).
        static void SetWorldBox(GameObject g, string name, float x0, float x1, float y0, float y1, float z0, float z1)
        {
            if (name != null) g.name = name;
            var t = g.transform;
            t.rotation = Quaternion.identity;
            t.position = new Vector3((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2);
            var ps = t.parent != null ? t.parent.lossyScale : Vector3.one;
            t.localScale = new Vector3((x1 - x0) / ps.x, (y1 - y0) / ps.y, (z1 - z0) / ps.z);
        }

        static Material VoidMaterial()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(VoidMaterialPath);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "M_FX_Void" };
            m.SetColor("_BaseColor", Color.black);
            AssetDatabase.CreateAsset(m, VoidMaterialPath);
            return m;
        }
    }
}
