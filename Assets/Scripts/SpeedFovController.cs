using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class SpeedFovController : MonoBehaviour
{
    [SerializeField] private PlayerRunner runner;
    [SerializeField, Range(20f, 100f)] private float minimumFov = 58f;
    [SerializeField, Range(20f, 100f)] private float maximumFov = 68f;
    [SerializeField, Min(1f)] private float speedForMaximumFov = 10f;
    [SerializeField, Min(0.1f)] private float smoothness = 5f;

    private Camera controlledCamera;

    public void Configure(PlayerRunner playerRunner)
    {
        runner = playerRunner;
    }

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
        runner ??= FindAnyObjectByType<PlayerRunner>();
    }

    private void LateUpdate()
    {
        if (runner == null)
        {
            return;
        }

        float normalizedSpeed = Mathf.Clamp01(
            runner.CurrentForwardSpeed / speedForMaximumFov);
        float targetFov = Mathf.Lerp(minimumFov, maximumFov, normalizedSpeed);
        float blend = 1f - Mathf.Exp(-smoothness * Time.unscaledDeltaTime);
        controlledCamera.fieldOfView = Mathf.Lerp(
            controlledCamera.fieldOfView,
            targetFov,
            blend);
    }
}
