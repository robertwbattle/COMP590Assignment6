using UnityEngine;

namespace MansionGame
{
    /// <summary>
    /// A mirror the moonbeam can bounce off. Only the front face of <c>surface</c> (or of the pane sitting in
    /// <c>paneSocket</c>) reflects; any other part of the mirror, or the back, blocks the beam.
    /// With a pane socket, the mirror reflects only while the socket is filled.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MoonMirror : MonoBehaviour
    {
        [SerializeField, Tooltip("Thin box collider lying on the glass face.")]
        private Collider surface;
        [SerializeField, Tooltip("Optional. If set, the mirror reflects only while this socket holds its pane.")]
        private Socket paneSocket;
        [SerializeField, Tooltip("Optional. The Turnable that rotates this mirror.")]
        private Turnable turnable;
        [SerializeField, Tooltip("Kit convention: the glass faces the room along the surface's -Z.")]
        private bool normalIsMinusZ = true;
        [SerializeField, Range(0f, 1f), Tooltip("Minimum dot(hit normal, mirror normal) for a hit to count as the front.")]
        private float frontDot = 0.5f;

        public Collider Surface => surface;
        public Socket PaneSocket => paneSocket;
        public Turnable Turnable => turnable;

        /// <summary>True while the mirror can reflect (enabled, and pane present if it needs one).</summary>
        public bool Reflects => isActiveAndEnabled && (paneSocket == null || paneSocket.IsFilled);

        /// <summary>World normal of the reflecting face.</summary>
        public Vector3 Normal
        {
            get
            {
                var t = surface != null ? surface.transform : transform;
                return normalIsMinusZ ? -t.forward : t.forward;
            }
        }

        /// <summary>True if c is the glass: the surface collider or a collider of the inserted pane.</summary>
        public bool IsSurface(Collider c)
        {
            if (c == null)
                return false;
            if (c == surface)
                return true;
            var pane = paneSocket != null ? paneSocket.Occupant : null;
            return pane != null && c.transform.IsChildOf(pane.transform);
        }

        /// <summary>Whether a raycast hit on collider c with this hit normal bounces.</summary>
        public bool IsReflectiveHit(Collider c, Vector3 hitNormal) =>
            Reflects && IsSurface(c) && Vector3.Dot(hitNormal, Normal) > frontDot;
    }
}
