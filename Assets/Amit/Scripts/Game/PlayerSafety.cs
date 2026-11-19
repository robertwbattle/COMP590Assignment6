using System.Collections;
using UnityEngine;

namespace MansionGame
{
    /// <summary>
    /// Keeps the player on solid ground. Room-scale tracking (and WASD in the XR Simulator) moves the head away from
    /// the rig's origin, and the body capsule follows the head; that offset survives a scene reload, so a restart can
    /// drop the body over a gap with no floor. On every load this recentres tracking so the player stands exactly at
    /// the start, and if they ever fall below the floor it puts them back at the start.
    /// </summary>
    public sealed class PlayerSafety : MonoBehaviour
    {
        [SerializeField, Tooltip("Where to put the player back. Defaults to the player's position at load.")]
        private Transform startPoint;
        [SerializeField, Tooltip("Below this world height the player is rescued.")]
        private float fallLimitY = -2.5f;

        CharacterController body;
        Vector3 startPos;
        Quaternion startRot;

        IEnumerator Start()
        {
            var player = FindAnyObjectByType<OVRPlayerController>();
            if (player == null) yield break;
            body = player.GetComponent<CharacterController>();
            startPos = startPoint != null ? startPoint.position : player.transform.position;
            startRot = startPoint != null ? startPoint.rotation : player.transform.rotation;

            // Wait for OVRManager, then reset the tracking origin so the head is over the start spot.
            float waited = 0f;
            while (!OVRManager.OVRManagerinitialized && waited < 5f) { waited += Time.deltaTime; yield return null; }
            yield return null;
            Recenter();
        }

        void Recenter()
        {
            if (OVRManager.isHmdPresent && OVRManager.display != null)
                OVRManager.display.RecenterPose();
        }

        void LateUpdate()
        {
            if (body == null || body.transform.position.y > fallLimitY) return;
            // Fell through the world: back to the start, standing still, tracking recentred.
            body.enabled = false;
            body.transform.SetPositionAndRotation(startPos, startRot);
            body.enabled = true;
            var opc = body.GetComponent<OVRPlayerController>();
            if (opc != null) opc.Stop();
            Recenter();
        }
    }
}
