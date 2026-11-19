using System.Collections;
using UnityEngine;

namespace MansionGame
{
    /// <summary>Short controller vibration pulses. Safe to call when no headset is connected.</summary>
    public sealed class Haptics : MonoBehaviour
    {
        static Haptics runner;
        static bool quitting;
        readonly Coroutine[] active = new Coroutine[3];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            runner = null;
            quitting = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        static void OnQuitting()
        {
            quitting = true;
            if (runner != null) Destroy(runner.gameObject);
        }

        static Haptics Runner
        {
            get
            {
                if (runner == null)
                {
                    // Lives across scene reloads; DontSave + DontDestroyOnLoad means it goes away when Play mode ends.
                    var go = new GameObject("Haptics Runner") { hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave };
                    DontDestroyOnLoad(go);
                    runner = go.AddComponent<Haptics>();
                }
                return runner;
            }
        }

        /// <summary>Vibrates one controller (LTouch or RTouch) for the given time.</summary>
        public static void Pulse(OVRInput.Controller controller, float amplitude, float seconds, float frequency = 0.5f)
        {
            if (!Application.isPlaying || quitting) return;
            int slot = controller == OVRInput.Controller.LTouch ? 0 : controller == OVRInput.Controller.RTouch ? 1 : 2;
            var r = Runner;
            if (r.active[slot] != null) r.StopCoroutine(r.active[slot]);
            r.active[slot] = r.StartCoroutine(r.Run(controller, Mathf.Clamp01(amplitude), seconds, Mathf.Clamp01(frequency), slot));
        }

        /// <summary>Vibrates both controllers.</summary>
        public static void PulseBoth(float amplitude, float seconds, float frequency = 0.5f)
        {
            Pulse(OVRInput.Controller.LTouch, amplitude, seconds, frequency);
            Pulse(OVRInput.Controller.RTouch, amplitude, seconds, frequency);
        }

        IEnumerator Run(OVRInput.Controller controller, float amplitude, float seconds, float frequency, int slot)
        {
            OVRInput.SetControllerVibration(frequency, amplitude, controller);
            yield return new WaitForSecondsRealtime(seconds);
            OVRInput.SetControllerVibration(0f, 0f, controller);
            active[slot] = null;
        }

        void OnDisable()
        {
            OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
            OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
        }
    }
}
