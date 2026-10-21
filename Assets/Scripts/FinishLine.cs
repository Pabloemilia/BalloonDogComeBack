using UnityEngine;

/// <summary>Compatibility component for authored scenes; endless runs have no finish trigger.</summary>
public sealed class FinishLine : MonoBehaviour
{
    private void Awake() { enabled = false; }
}
