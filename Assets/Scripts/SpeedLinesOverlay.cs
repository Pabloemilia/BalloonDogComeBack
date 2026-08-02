using UnityEngine;
using UnityEngine.UI;

public sealed class SpeedLinesOverlay : MonoBehaviour
{
    [SerializeField] private PlayerRunner runner;
    [SerializeField] private Graphic[] lines;
    [SerializeField, Min(1f)] private float fullEffectSpeed = 10f;
    [SerializeField, Range(0f, 1f)] private float maximumAlpha = 0.22f;

    public void Configure(PlayerRunner playerRunner, Graphic[] lineGraphics)
    {
        runner = playerRunner;
        lines = lineGraphics;
    }

    private void Update()
    {
        if (runner == null || lines == null)
        {
            return;
        }

        float amount = Mathf.Clamp01(runner.CurrentForwardSpeed / fullEffectSpeed);
        float pulse = 0.75f + Mathf.Sin(Time.unscaledTime * 8f) * 0.25f;
        float alpha = maximumAlpha * amount * pulse;

        foreach (Graphic line in lines)
        {
            if (line == null)
            {
                continue;
            }

            Color color = line.color;
            color.a = alpha;
            line.color = color;
        }
    }
}
