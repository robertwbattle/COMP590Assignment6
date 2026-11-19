using UnityEngine;

namespace MansionGame
{
    /// <summary>
    /// The moonbeam for puzzle 3. Every frame it traces from <c>source</c> along its forward, bouncing off the
    /// front of reflecting MoonMirrors, and stops at the first other surface. If that surface belongs to a
    /// MoonSeal the seal is illuminated. The path is drawn with a world-space LineRenderer.
    /// </summary>
    public sealed class MoonBeam : MonoBehaviour
    {
        const int MaxPoints = 16;
        const float SurfaceSkip = 0.002f;

        [SerializeField, Tooltip("The beam starts here and heads along its forward.")]
        private Transform source;
        [SerializeField, Tooltip("World-space line that draws the beam.")]
        private LineRenderer line;
        [SerializeField, Range(0, MaxPoints - 2)]
        private int maxBounces = 6;
        [SerializeField, Min(0.1f), Tooltip("Total beam length (m) across all bounces.")]
        private float maxDistance = 25f;
        [SerializeField]
        private LayerMask mask = Physics.DefaultRaycastLayers;
        [SerializeField, Tooltip("Flatten the beam onto the horizontal plane after every bounce.")]
        private bool lockToHorizontal = true;

        readonly Vector3[] points = new Vector3[MaxPoints];
        readonly RaycastHit[] hits = new RaycastHit[64];
        int pointCount;

        public bool HitsSeal => HitSeal != null;
        public MoonSeal HitSeal { get; private set; }
        public int PointCount => pointCount;
        public Vector3 GetPoint(int i) => points[i];

        /// <summary>Copy of the current path (allocates; for tests).</summary>
        public Vector3[] CurrentPath
        {
            get
            {
                var copy = new Vector3[pointCount];
                System.Array.Copy(points, copy, pointCount);
                return copy;
            }
        }

        void Awake()
        {
            if (line != null)
                line.useWorldSpace = true;
        }

        void Update()
        {
            Trace();
            DrawLine();
            if (HitSeal != null)
                HitSeal.Illuminate(Time.deltaTime);
        }

        void OnDisable()
        {
            if (line != null)
                line.positionCount = 0;
        }

        /// <summary>Recomputes the path now (no illumination). Returns whether it ends on a seal.</summary>
        public bool Trace()
        {
            HitSeal = null;
            pointCount = 0;
            if (source == null)
                return false;

            var origin = source.position;
            var dir = Flatten(source.forward, source.forward);
            float remaining = maxDistance;
            Collider last = null;
            points[pointCount++] = origin;

            int bounces = Mathf.Clamp(maxBounces, 0, MaxPoints - 2);
            for (int bounce = 0; ; bounce++)
            {
                if (!CastFirst(origin, dir, remaining, last, out var hit))
                {
                    points[pointCount++] = origin + dir * remaining;
                    break;
                }

                points[pointCount++] = hit.point;
                remaining -= hit.distance;

                var seal = hit.collider.GetComponentInParent<MoonSeal>();
                if (seal != null)
                {
                    HitSeal = seal;
                    break;
                }

                var mirror = hit.collider.GetComponentInParent<MoonMirror>();
                if (mirror == null || !mirror.IsReflectiveHit(hit.collider, hit.normal)
                    || bounce >= bounces || remaining <= 0f || pointCount >= MaxPoints)
                    break;

                var reflected = Vector3.Reflect(dir, mirror.Normal);
                dir = Flatten(reflected, reflected);
                if (dir == Vector3.zero)
                    break;
                origin = hit.point;
                last = hit.collider;
            }
            return HitSeal != null;
        }

        bool CastFirst(Vector3 origin, Vector3 dir, float distance, Collider skip, out RaycastHit best)
        {
            best = default;
            if (distance <= 0f)
                return false;
            int n = Physics.RaycastNonAlloc(origin, dir, hits, distance, mask, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                var c = h.collider;
                if (c == null || c is CharacterController || h.distance >= bestDist)
                    continue;
                if (c == skip && h.distance < 0.01f)
                    continue;
                if (h.distance < SurfaceSkip)
                    continue;
                if (source != null && c.transform.IsChildOf(source))
                    continue;
                best = h;
                bestDist = h.distance;
                found = true;
            }
            return found;
        }

        Vector3 Flatten(Vector3 v, Vector3 fallback)
        {
            if (lockToHorizontal)
                v.y = 0f;
            if (v.sqrMagnitude < 1e-8f)
                return lockToHorizontal ? Vector3.zero : fallback.normalized;
            return v.normalized;
        }

        void DrawLine()
        {
            if (line == null)
                return;
            line.positionCount = pointCount;
            for (int i = 0; i < pointCount; i++)
                line.SetPosition(i, points[i]);
        }

        void OnDrawGizmosSelected()
        {
            if (source == null)
                return;
            Gizmos.color = new Color(0.6f, 0.7f, 1f, 0.8f);
            Gizmos.DrawLine(source.position, source.position + source.forward * 1f);
        }
    }
}
