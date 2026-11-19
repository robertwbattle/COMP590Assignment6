using UnityEngine;

/// <summary>Lets either tracked Touch controller point at the existing candle puzzle.</summary>
[RequireComponent(typeof(LineRenderer))]
public sealed class MansionCandlePointer : MonoBehaviour
{
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;
    [SerializeField, Min(0.1f)] private float reach = 2.25f;
    [SerializeField] private LayerMask interactionLayers = Physics.DefaultRaycastLayers;

    private LineRenderer pointer;

    private void Awake()
    {
        pointer = GetComponent<LineRenderer>();
        pointer.enabled = false;
    }

    private void Update()
    {
        bool tracked = OVRManager.isHmdPresent
            && OVRManager.hasInputFocus
            && OVRInput.IsControllerConnected(controller)
            && OVRInput.GetControllerPositionTracked(controller)
            && OVRInput.GetControllerOrientationTracked(controller);

        pointer.enabled = tracked;
        if (!tracked)
            return;

        var ray = new Ray(transform.position, transform.forward);
        bool hitSomething = Physics.Raycast(ray, out var hit, reach, interactionLayers,
            QueryTriggerInteraction.Ignore);
        var candle = hitSomething ? hit.collider.GetComponentInParent<Puzzle1_1>() : null;
        bool canExtinguish = candle != null && candle.IsLit;

        pointer.SetPosition(0, ray.origin);
        pointer.SetPosition(1, hitSomething ? hit.point : ray.GetPoint(reach));
        Color color = canExtinguish ? new Color(1f, 0.92f, 0.66f) : new Color(0.64f, 0.49f, 0.26f);
        pointer.startColor = color;
        pointer.endColor = color;

        if (canExtinguish && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller))
            TryExtinguish();
    }

    /// <summary>Uses the same occluded, distance-limited ray as the controller trigger.</summary>
    public bool TryExtinguish()
    {
        if (!Physics.Raycast(transform.position, transform.forward, out var hit, reach,
            interactionLayers, QueryTriggerInteraction.Ignore))
            return false;

        var candle = hit.collider.GetComponentInParent<Puzzle1_1>();
        if (candle == null || !candle.enabled || !candle.IsLit
            || candle.bookcase == null || candle.bookcase.IsAnimating)
            return false;

        candle.Extinguish();
        return true;
    }

    private void OnDisable()
    {
        if (pointer != null)
            pointer.enabled = false;
    }
}
