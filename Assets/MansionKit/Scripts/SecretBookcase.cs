using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MansionKit
{
    /// <summary>
    /// The secret bookcase in the parlour. Open() pulls the floor hatch under the bookcase back into the wall
    /// (it drops a few cm, then slides), then sinks the bookcase into the void below until its top sits flush
    /// with the floor as a new floorboard. Close() runs the same thing in reverse.
    /// An invisible barrier blocks the area only while either part is moving.
    /// </summary>
    public class SecretBookcase : MonoBehaviour
    {
        [Header("Parts")]
        public Transform bookcase;
        public Transform floorHatch;
        [Tooltip("Enabled only while the mechanism is animating.")]
        public Collider barrier;
        [Tooltip("Lights inside the secret room. They are off while the bookcase is closed and fade in as it sinks.")]
        public Light[] roomLights = Array.Empty<Light>();

        [Header("Timing (seconds)")]
        public float hatchDuration = 0.3f;
        public float bookcaseDuration = 2f;

        [Header("Motion (world space, metres)")]
        public float sinkDepth = 3.18f;
        public float hatchDrop = 0.035f;
        public Vector3 hatchSlide = new Vector3(-1.12f, 0f, 0f);
        [Range(0.05f, 0.5f), Tooltip("Share of the hatch time spent dropping before it slides.")]
        public float hatchDropShare = 0.2f;

        [Header("Room lights")]
        public float roomLightIntensity = 4f;

        [Header("Testing")]
        [Tooltip("Press K to toggle the bookcase.")]
        public bool debugKey = true;

        public bool IsOpen { get; private set; }
        public bool IsAnimating => routine != null;

        Vector3 bookcaseClosed, hatchClosed;
        Coroutine routine;

        void Awake()
        {
            bookcaseClosed = bookcase.position;
            hatchClosed = floorHatch.position;
            SetBarrier(false);
            SetLights(0f);
        }

        void Update()
        {
            if (debugKey && Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
                Toggle();
        }

        /// <summary>Opens if closed, closes if open. Ignored while a move is in progress; returns whether it started.</summary>
        public bool Toggle() => IsOpen ? Close() : Open();

        public bool Open()
        {
            if (IsAnimating || IsOpen) return false;
            routine = StartCoroutine(OpenRoutine());
            return true;
        }

        public bool Close()
        {
            if (IsAnimating || !IsOpen) return false;
            routine = StartCoroutine(CloseRoutine());
            return true;
        }

        IEnumerator OpenRoutine()
        {
            SetBarrier(true);
            yield return Animate(hatchDuration, t => floorHatch.position = HatchPosition(t));
            yield return Animate(bookcaseDuration, t =>
            {
                bookcase.position = bookcaseClosed + Vector3.down * (sinkDepth * Ease(t));
                SetLights(roomLightIntensity * t);
            });
            IsOpen = true;
            SetBarrier(false);
            routine = null;
        }

        IEnumerator CloseRoutine()
        {
            SetBarrier(true);
            yield return Animate(bookcaseDuration, t =>
            {
                bookcase.position = bookcaseClosed + Vector3.down * (sinkDepth * Ease(1f - t));
                SetLights(roomLightIntensity * (1f - t));
            });
            yield return Animate(hatchDuration, t => floorHatch.position = HatchPosition(1f - t));
            IsOpen = false;
            SetBarrier(false);
            routine = null;
        }

        // t = 0 closed (flush with the floor), t = 1 fully tucked under the secret room floor.
        Vector3 HatchPosition(float t)
        {
            var dropped = hatchClosed + Vector3.down * hatchDrop;
            if (t <= hatchDropShare) return Vector3.Lerp(hatchClosed, dropped, Ease(t / hatchDropShare));
            return dropped + hatchSlide * Ease((t - hatchDropShare) / (1f - hatchDropShare));
        }

        static IEnumerator Animate(float duration, Action<float> apply)
        {
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                apply(time / duration);
                yield return null;
            }
            apply(1f);
        }

        static float Ease(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        void SetBarrier(bool on)
        {
            if (barrier != null) barrier.enabled = on;
        }

        void SetLights(float intensity)
        {
            foreach (var l in roomLights)
            {
                if (l == null) continue;
                l.intensity = intensity;
                l.enabled = intensity > 0.001f;
            }
        }
    }
}
