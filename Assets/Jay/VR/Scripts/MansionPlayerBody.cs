using UnityEngine;

/// <summary>Keeps the SDK character capsule beneath the tracked headset during room-scale movement.</summary>
[RequireComponent(typeof(OVRPlayerController), typeof(CharacterController))]
public sealed class MansionPlayerBody : MonoBehaviour
{
    [SerializeField] private OVRCameraRig cameraRig;

    private OVRPlayerController player;
    private CharacterController body;

    private void Awake()
    {
        player = GetComponent<OVRPlayerController>();
        body = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        player.PreCharacterMove += FitToHeadset;
    }

    private void OnDisable()
    {
        player.PreCharacterMove -= FitToHeadset;
    }

    private void FitToHeadset()
    {
        if (cameraRig == null || cameraRig.centerEyeAnchor == null)
            return;

        Vector3 head = transform.InverseTransformPoint(cameraRig.centerEyeAnchor.position);
        body.height = Mathf.Clamp(head.y + 0.10f, 1.0f, 2.4f);
        body.center = new Vector3(head.x, body.height / 2f, head.z);
    }
}
