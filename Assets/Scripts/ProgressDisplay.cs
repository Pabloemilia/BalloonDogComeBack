using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ProgressDisplay : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text label;
    [SerializeField] private float startZ;
    [SerializeField] private float finishZ = 185f;

    public void Configure(
        Transform playerTransform,
        Slider progressSlider,
        TMP_Text progressLabel,
        float levelFinishZ)
    {
        player = playerTransform;
        slider = progressSlider;
        label = progressLabel;
        startZ = player != null ? player.position.z : 0f;
        finishZ = Mathf.Max(startZ + 1f, levelFinishZ);
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        float progress = Mathf.InverseLerp(startZ, finishZ, player.position.z);
        if (slider != null)
        {
            slider.value = progress;
        }

        if (label != null)
        {
            label.text = $"BÖLÜM %{progress * 100f:0}";
        }
    }
}
