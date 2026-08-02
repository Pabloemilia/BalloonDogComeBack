using UnityEngine;

[DefaultExecutionOrder(1000)]
public sealed class CameraShakeController : MonoBehaviour
{
    private static CameraShakeController instance;

    private float remainingTime;
    private float strength;

    private void Awake()
    {
        instance = this;
    }

    private void LateUpdate()
    {
        if (remainingTime <= 0f)
        {
            return;
        }

        remainingTime -= Time.unscaledDeltaTime;
        float fade = Mathf.Clamp01(remainingTime / 0.25f);
        Vector2 offset = Random.insideUnitCircle * strength * fade;
        transform.position += new Vector3(offset.x, offset.y, 0f);
    }

    public void Shake(float duration, float amount)
    {
        remainingTime = Mathf.Max(remainingTime, duration);
        strength = Mathf.Max(strength, amount);
    }

    public static void ShakeGlobal(float duration, float amount)
    {
        if (instance == null)
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                instance = camera.GetComponent<CameraShakeController>();
                if (instance == null)
                {
                    instance = camera.gameObject.AddComponent<CameraShakeController>();
                }
            }
        }

        instance?.Shake(duration, amount);
    }
}
