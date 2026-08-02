using UnityEngine;

public sealed class PerformanceBootstrap : MonoBehaviour
{
    [SerializeField, Min(30)] private int targetFrameRate = 60;

    private void Awake()
    {
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0;
    }
}
