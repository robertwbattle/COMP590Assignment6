using MansionKit;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Puzzle 1: clicking the candle on the parlour coffee table puts it out, which opens the secret bookcase.
/// Extinguish() can also be called directly.
/// </summary>
public class Puzzle1_1 : MonoBehaviour
{
    public GameObject flame;
    public SecretBookcase bookcase;

    public bool IsLit { get; private set; } = true;

    void Update()
    {
        if (!IsLit || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        var cam = Camera.main;
        if (cam == null) return;
        if (Physics.Raycast(cam.ScreenPointToRay(Mouse.current.position.ReadValue()), out var hit) && hit.transform == transform)
            Extinguish();
    }

    public void Extinguish()
    {
        if (!IsLit) return;
        IsLit = false;
        if (flame != null) flame.SetActive(false);
        if (bookcase != null) bookcase.Open();
    }
}
