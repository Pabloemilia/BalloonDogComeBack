using UnityEngine;

/// <summary>
/// Bölüm ilerledikçe koşu hızını kontrollü biçimde artırır.
/// </summary>
[RequireComponent(typeof(PlayerRunner))]
public sealed class DifficultyDirector : MonoBehaviour
{
    [SerializeField] private float startZ;
    [SerializeField] private float finishZ = 185f;
    [SerializeField, Range(1f, 2f)] private float maximumSpeedMultiplier = 1.32f;
    [SerializeField, Min(0.1f)] private float smoothness = 2.5f;

    private PlayerRunner runner;
    private float currentMultiplier = 1f;

    public void Configure(float levelFinishZ, float maxMultiplier = 1.32f)
    {
        startZ = transform.position.z;
        finishZ = Mathf.Max(startZ + 1f, levelFinishZ);
        maximumSpeedMultiplier = Mathf.Max(1f, maxMultiplier);
    }

    private void Awake()
    {
        runner = GetComponent<PlayerRunner>();
        startZ = transform.position.z;
    }

    private void Update()
    {
        if (runner == null || !runner.MovementEnabled)
        {
            return;
        }

        float progress = Mathf.InverseLerp(startZ, finishZ, transform.position.z);
        float easedProgress = progress * progress * (3f - 2f * progress);
        float target = Mathf.Lerp(1f, maximumSpeedMultiplier, easedProgress);
        float blend = 1f - Mathf.Exp(-smoothness * Time.deltaTime);
        currentMultiplier = Mathf.Lerp(currentMultiplier, target, blend);
        runner.SetDifficultyMultiplier(currentMultiplier);
    }
}
