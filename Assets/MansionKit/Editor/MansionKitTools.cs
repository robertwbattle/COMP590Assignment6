// Mansion Kit editor tools.
//   Tools > Mansion Kit > Extract Prefabs From Reference Scene
//   Tools > Mansion Kit > New Room Scene...
//   Tools > Mansion Kit > Validate Active Scene
// The numbers in this file are the ones STYLE_GUIDE.md documents. Change both together.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MansionKit.EditorTools
{
    public static class MansionKitStyle
    {
        public const string KitRoot = "Assets/MansionKit";
        public const string MaterialRoot = KitRoot + "/Materials";
        public const string PrefabRoot = KitRoot + "/Prefabs";
        public const string ReferenceScene = "Assets/Scenes/HauntedVictorianMansion.unity";

        // Environment (matches the reference scene's lighting and camera settings)
        public static readonly Color AmbientFlat = Hex("6A5D6A");
        public static readonly Color CameraBackground = Hex("191619");
        public const float CameraFov = 59f, CameraNear = 0.07f, CameraFar = 30f, EyeHeight = 1.64f;

        // Light rig
        public static readonly Color MoonFill = Hex("AAA6C9");   // directional ambient fill
        public static readonly Color Chandelier = Hex("FFCF98");
        public static readonly Color Lamp = Hex("F8CC9A");
        public static readonly Color Hearth = Hex("FF823F");
        public static readonly Color Moonlight = Hex("92A9EC");
        public static readonly Quaternion MoonFillRotation = new Quaternion(0.0851766f, 0.9142142f, -0.2970462f, 0.2621467f);

        // Architecture (metres)
        public const float CeilingHeight = 5.10f;
        public const float WallThickness = 0.16f;
        public const float BoardWidth = 0.45f;

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        public static Material Mat(string category, string name)
        {
            var path = $"{MaterialRoot}/{category}/M_{category}_{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) Debug.LogError($"[MansionKit] Missing material {path}");
            return m;
        }
    }

    public static class MansionKitPrefabs
    {
        // scene path -> prefab path (relative to PrefabRoot). Flip = turn contents 180 deg so the front faces -Z.
        static readonly (string scenePath, string prefab, bool flip)[] Extracts =
        {
            ("Parlor Furniture/East Sofa",                         "Furniture/P_Sofa_Chesterfield", true),
            ("Parlor Furniture/Coffee Table",                      "Furniture/P_CoffeeTable", false),
            ("Parlor Furniture/Parlor Bookcase",                   "Furniture/P_Bookcase", false),
            ("Parlor Furniture/Sideboard",                         "Furniture/P_Sideboard", false),
            ("Parlor Furniture/Patterned Rug",                     "Furniture/P_Rug_Patterned", false),
            ("Parlor Furniture/Coffee Table/Closed Book",          "Props/P_Book_Closed", false),
            ("West Portrait",                                      "Decor/P_Portrait", false),
            ("Moonlit Window and Drapes",                          "Architecture/P_Window_Moonlit", false),
            ("Fireplace and Haunted Mirror",                       "Architecture/P_Fireplace_HauntedMirror", false),
            ("Room and Entry Hall/Entry Hall Door",                "Architecture/P_Door_Paneled", false),
            ("Room and Entry Hall/Parlor Pilasters/East wall pilaster", "Architecture/P_Pilaster", false),
        };

        [MenuItem("Tools/Mansion Kit/Extract Prefabs From Reference Scene")]
        static void ExtractAllMenu()
        {
            var made = ExtractAll(true);
            if (made != null)
                EditorUtility.DisplayDialog("Mansion Kit", $"Saved {made.Count} prefabs:\n\n" + string.Join("\n", made), "OK");
        }

        // interactive=false never shows a dialog (for scripted use); it refuses to leave a modified scene.
        public static List<string> ExtractAll(bool interactive = false)
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != MansionKitStyle.ReferenceScene)
            {
                if (interactive ? !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() : scene.isDirty)
                {
                    Debug.LogWarning("[MansionKit] Active scene has unsaved changes; extraction cancelled.");
                    return null;
                }
                scene = EditorSceneManager.OpenScene(MansionKitStyle.ReferenceScene, OpenSceneMode.Single);
            }

            var made = new List<string>();
            foreach (var (scenePath, prefab, flip) in Extracts)
            {
                var src = GameObject.Find(scenePath);
                if (src == null) { Debug.LogWarning($"[MansionKit] Not found in scene: {scenePath}"); continue; }
                var copy = Object.Instantiate(src);
                copy.name = Path.GetFileName(prefab);
                ResetRoot(copy.transform, src.transform.lossyScale, flip, src.transform.position.y);
                Save(copy, prefab, made);
            }

            // Chandelier: the fixture plus its light (siblings in the scene).
            var chandelier = GameObject.Find("Room Lighting/Chandelier");
            var chandelierLight = GameObject.Find("Room Lighting/Chandelier warm light");
            if (chandelier != null)
            {
                var root = new GameObject("P_Chandelier");
                var anchor = Vector3.Scale(chandelier.transform.position, new Vector3(1, 0, 1)); // floor under the fixture
                AttachCopy(chandelier, root.transform, anchor);
                if (chandelierLight != null) AttachCopy(chandelierLight, root.transform, anchor);
                Save(root, "Lighting/P_Chandelier", made);
            }

            // Candle: taper + flame from the sideboard, pivot at the base of the candle.
            var candle = GameObject.Find("Parlor Furniture/Sideboard/Candle");
            var flame = GameObject.Find("Parlor Furniture/Sideboard/Flame");
            if (candle != null && flame != null)
            {
                var root = new GameObject("P_Candle");
                var b = candle.GetComponent<Renderer>().bounds;
                var anchor = new Vector3(b.center.x, b.min.y, b.center.z);
                AttachCopy(candle, root.transform, anchor);
                AttachCopy(flame, root.transform, anchor);
                Save(root, "Lighting/P_Candle", made);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return made;
        }

        // Pivot goes to the floor under the object; the height it hung at (worldY) is kept in the children.
        static void ResetRoot(Transform t, Vector3 keepScale, bool flip, float worldY)
        {
            t.SetParent(null, false);
            t.position = Vector3.zero;
            t.rotation = Quaternion.identity;
            t.localScale = keepScale;
            var kids = new List<Transform>();
            foreach (Transform c in t) kids.Add(c);
            foreach (var c in kids)
            {
                var p = c.localPosition + Vector3.up * (worldY / keepScale.y);
                if (flip)
                {
                    p = new Vector3(-p.x, p.y, -p.z);
                    c.localRotation = Quaternion.Euler(0f, 180f, 0f) * c.localRotation;
                }
                c.localPosition = p;
            }
        }

        static void AttachCopy(GameObject src, Transform parent, Vector3 anchor)
        {
            var c = Object.Instantiate(src, src.transform.position - anchor, src.transform.rotation, parent);
            c.name = src.name;
            c.transform.localScale = src.transform.lossyScale;
        }

        static void Save(GameObject go, string relPath, List<string> made)
        {
            var path = $"{MansionKitStyle.PrefabRoot}/{relPath}.prefab";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            PrefabUtility.SaveAsPrefabAsset(go, path, out var ok);
            Object.DestroyImmediate(go);
            if (ok) made.Add(path); else Debug.LogError($"[MansionKit] Failed to save {path}");
        }
    }

    public class MansionKitRoomWindow : EditorWindow
    {
        float width = 10f, depth = 8f;
        string sceneName = "NewMansionRoom";

        [MenuItem("Tools/Mansion Kit/New Room Scene...")]
        static void Open() => GetWindow<MansionKitRoomWindow>(true, "New Mansion Room", true).minSize = new Vector2(320, 130);

        void OnGUI()
        {
            sceneName = EditorGUILayout.TextField("Scene name", sceneName);
            width = Mathf.Max(3f, EditorGUILayout.FloatField("Width (X, m)", width));
            depth = Mathf.Max(3f, EditorGUILayout.FloatField("Depth (Z, m)", depth));
            EditorGUILayout.HelpBox($"Ceiling is fixed at {MansionKitStyle.CeilingHeight} m to match the kit.", MessageType.None);
            if (GUILayout.Button("Create")) { MansionKitRoomBuilder.CreateScene(sceneName, width, depth); Close(); }
        }
    }

    public static class MansionKitRoomBuilder
    {
        public static void CreateScene(string name, float width, float depth, bool interactive = true)
        {
            if (interactive ? !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() : SceneManager.GetActiveScene().isDirty) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyEnvironment();
            BuildShell(width, depth);
            BuildRig(width, depth);
            var path = $"Assets/Scenes/{name}.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[MansionKit] Created {path}. Furnish it from {MansionKitStyle.PrefabRoot}.");
        }

        public static void ApplyEnvironment()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = MansionKitStyle.AmbientFlat;
            RenderSettings.fog = false;
            RenderSettings.skybox = null;
            RenderSettings.reflectionIntensity = 0.3f;
        }

        static void BuildShell(float w, float d)
        {
            var room = new GameObject("Room").transform;
            float h = MansionKitStyle.CeilingHeight;

            var floor = Box("Floor collider", room, new Vector3(0, -0.11f, 0), new Vector3(w, 0.2f, d), MansionKitStyle.Mat("Wood", "Walnut"), true);
            var boards = new GameObject("Floorboards").transform; boards.SetParent(room, false);
            var shades = new[] { MansionKitStyle.Mat("Wood", "FloorDark"), MansionKitStyle.Mat("Wood", "FloorMid"), MansionKitStyle.Mat("Wood", "FloorLight") };
            var rng = new System.Random(590);
            int row = 0;
            for (float z = -d / 2 + MansionKitStyle.BoardWidth / 2; z < d / 2; z += MansionKitStyle.BoardWidth, row++)
            {
                float x = -w / 2;
                while (x < w / 2 - 0.05f)
                {
                    float len = Mathf.Min(1.6f + (float)rng.NextDouble() * 2.6f, w / 2 - x);
                    // Mid is the dominant shade; dark and light are accents.
                    var m = shades[new[] { 0, 1, 1, 2 }[rng.Next(4)]];
                    Box("Floorboard", boards, new Vector3(x + len / 2, 0f, z), new Vector3(len - 0.01f, 0.01f, MansionKitStyle.BoardWidth - 0.01f), m, false);
                    x += len;
                }
            }

            Box("Plaster ceiling", room, new Vector3(0, h + 0.09f, 0), new Vector3(w, 0.18f, d), MansionKitStyle.Mat("Surface", "Plaster"), true);

            Wall("North wall", room, new Vector3(0, 0, d / 2), 0f, w);
            Wall("South wall", room, new Vector3(0, 0, -d / 2), 180f, w);
            Wall("East wall", room, new Vector3(w / 2, 0, 0), 90f, d);
            Wall("West wall", room, new Vector3(-w / 2, 0, 0), -90f, d);
        }

        // Wall layering measured from the reference scene's West wall. Room side is local -Z.
        static void Wall(string name, Transform parent, Vector3 pos, float yaw, float length)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0, yaw, 0);
            var trim = MansionKitStyle.Mat("Wood", "Trim");
            var walnut = MansionKitStyle.Mat("Wood", "Walnut");
            float h = MansionKitStyle.CeilingHeight;
            Box("Wallpaper", t, new Vector3(0, h / 2, 0), new Vector3(length, h, MansionKitStyle.WallThickness), MansionKitStyle.Mat("Surface", "PlumWallpaper"), true);
            Box("Wainscot", t, new Vector3(0, 0.53f, -0.11f), new Vector3(length, 1.06f, 0.07f), walnut, false);
            Box("Baseboard", t, new Vector3(0, 0.10f, -0.17f), new Vector3(length, 0.18f, 0.07f), trim, false);
            Box("Chair rail", t, new Vector3(0, 1.07f, -0.17f), new Vector3(length, 0.10f, 0.08f), trim, false);
            Box("Crown molding", t, new Vector3(0, h - 0.13f, -0.14f), new Vector3(length, 0.19f, 0.14f), trim, false);
            Box("Crown molding bead", t, new Vector3(0, h - 0.24f, -0.18f), new Vector3(length, 0.03f, 0.04f), MansionKitStyle.Mat("Metal", "AgedBrass"), false);

            int panels = Mathf.Max(1, Mathf.FloorToInt((length - 0.4f) / 1.66f));
            float pitch = (length - 0.4f) / panels;
            for (int i = 0; i < panels; i++)
            {
                float x = -length / 2 + 0.2f + pitch * (i + 0.5f);
                Box("Inset panel", t, new Vector3(x, 0.55f, -0.15f), new Vector3(pitch * 0.87f, 0.64f, 0.04f), trim, false);
                Box("Inset field", t, new Vector3(x, 0.55f, -0.17f), new Vector3(pitch * 0.81f, 0.54f, 0.03f), walnut, false);
            }
        }

        static void BuildRig(float w, float d)
        {
            var rig = new GameObject("Room Lighting").transform;

            var fill = new GameObject("Ambient fill").AddComponent<Light>();
            fill.transform.SetParent(rig, false);
            fill.transform.rotation = MansionKitStyle.MoonFillRotation;
            fill.type = LightType.Directional;
            fill.color = MansionKitStyle.MoonFill;
            fill.intensity = 0.2f;
            fill.shadows = LightShadows.None;

            // Key light: the chandelier prefab (fixture + light) if it has been extracted, else a bare light.
            var chandelierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{MansionKitStyle.PrefabRoot}/Lighting/P_Chandelier.prefab");
            Light key;
            if (chandelierPrefab != null)
            {
                var c = (GameObject)PrefabUtility.InstantiatePrefab(chandelierPrefab);
                c.transform.SetParent(rig, false);
                key = c.GetComponentInChildren<Light>();
            }
            else
            {
                key = new GameObject("Chandelier warm light").AddComponent<Light>();
                key.transform.SetParent(rig, false);
                key.transform.localPosition = new Vector3(0, 3.26f, 0);
                key.type = LightType.Point;
                key.color = MansionKitStyle.Chandelier;
                key.intensity = 12.5f;
                key.shadows = LightShadows.None;
            }
            if (key != null) key.range = Mathf.Max(11.2f, 0.85f * Mathf.Max(w, d));

            var start = new GameObject("Player Start").transform;
            start.position = new Vector3(0, 0.02f, -d / 2 + 0.6f);
            var cam = new GameObject("Preview Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.transform.SetParent(start, false);
            cam.transform.localPosition = new Vector3(0, MansionKitStyle.EyeHeight - 0.02f, 0);
            cam.transform.localRotation = Quaternion.Euler(-1.75f, 0, 0);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = MansionKitStyle.CameraBackground;
            cam.fieldOfView = MansionKitStyle.CameraFov;
            cam.nearClipPlane = MansionKitStyle.CameraNear;
            cam.farClipPlane = MansionKitStyle.CameraFar;
            cam.allowHDR = true;
            cam.tag = "MainCamera";
        }

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
    }

    public static class MansionKitValidator
    {
        [MenuItem("Tools/Mansion Kit/Validate Active Scene")]
        static void ValidateMenu()
        {
            var (e, w) = Validate();
            EditorUtility.DisplayDialog("Mansion Kit", $"{e} errors, {w} warnings.\nDetails are in the Console.", "OK");
        }

        public static (int errors, int warnings) Validate()
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();

            // Materials must come from the kit.
            var offKit = new HashSet<string>();
            foreach (var r in roots.SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) { errors.Add($"Missing material on {PathOf(r.transform)}"); continue; }
                    var p = AssetDatabase.GetAssetPath(m);
                    if (!p.StartsWith(MansionKitStyle.MaterialRoot)) offKit.Add($"{m.name} ({(p == "" ? "built-in/instance" : p)})");
                    if (m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > 0.001f && !m.IsKeywordEnabled("_EMISSION"))
                        warnings.Add($"{m.name}: emission color set but _EMISSION keyword is off, so it will not glow");
                }
            foreach (var m in offKit) errors.Add($"Material outside the kit: {m}");

            // Lights: warm (r>=g>=b) or moonlight (b>r). No neutral white, no saturated colors.
            var lights = roots.SelectMany(g => g.GetComponentsInChildren<Light>(true)).ToList();
            foreach (var l in lights)
            {
                var c = l.color;
                bool warm = c.r >= c.g && c.g >= c.b && c.b > 0.2f;
                bool moon = c.b > c.r && c.b > c.g && c.r > 0.45f;
                if (!warm && !moon) errors.Add($"Light '{l.name}' color #{ColorUtility.ToHtmlStringRGB(c)} is neither candle-warm nor moonlight");
                if (l.type == LightType.Directional && l.intensity > 0.3f) warnings.Add($"Directional '{l.name}' intensity {l.intensity} > 0.3; interiors are lit by practicals, not the sky");
                if (l.type == LightType.Point && l.intensity > 15f) warnings.Add($"Point light '{l.name}' intensity {l.intensity} exceeds the chandelier key (12.5)");
            }
            int dirs = lights.Count(l => l.type == LightType.Directional);
            if (dirs != 1) warnings.Add($"Expected exactly 1 directional fill light, found {dirs}");

            // Environment
            if (RenderSettings.ambientMode != AmbientMode.Flat || !Near(RenderSettings.ambientLight, MansionKitStyle.AmbientFlat))
                warnings.Add($"Ambient should be Flat #{ColorUtility.ToHtmlStringRGB(MansionKitStyle.AmbientFlat)}");
            if (RenderSettings.fog) warnings.Add("Fog is on; the reference look has fog off");
            var cams = roots.SelectMany(g => g.GetComponentsInChildren<Camera>(true)).ToList();
            foreach (var cam in cams)
            {
                if (cam.clearFlags != CameraClearFlags.SolidColor || !Near(cam.backgroundColor, MansionKitStyle.CameraBackground))
                    warnings.Add($"Camera '{cam.name}' should clear to solid #{ColorUtility.ToHtmlStringRGB(MansionKitStyle.CameraBackground)}");
                if (Mathf.Abs(cam.fieldOfView - MansionKitStyle.CameraFov) > 5f) warnings.Add($"Camera '{cam.name}' FOV {cam.fieldOfView} (kit: {MansionKitStyle.CameraFov})");
            }

            // Non-uniform scale on a parent stretches every child (bevel-less primitives hide it, spheres don't).
            foreach (var t in roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)))
            {
                if (t.childCount == 0) continue;
                var s = t.localScale;
                if (Mathf.Abs(s.x - s.y) > 0.01f || Mathf.Abs(s.y - s.z) > 0.01f)
                    if (t.GetComponentsInChildren<MeshFilter>(true).Any(f => f.sharedMesh != null && f.sharedMesh.name == "Sphere"))
                        warnings.Add($"Non-uniform parent scale on '{PathOf(t)}' squashes child spheres");
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Mansion Kit validation: {errors.Count} errors, {warnings.Count} warnings");
            foreach (var e in errors) sb.AppendLine("  ERROR  " + e);
            foreach (var w in warnings) sb.AppendLine("  WARN   " + w);
            if (errors.Count > 0) Debug.LogError(sb.ToString());
            else if (warnings.Count > 0) Debug.LogWarning(sb.ToString());
            else Debug.Log(sb.ToString());
            LastReport = sb.ToString();
            return (errors.Count, warnings.Count);
        }

        public static string LastReport { get; private set; } = "";

        static bool Near(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.03f;

        static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    }
}
