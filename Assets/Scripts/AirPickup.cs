using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class AirPickup : MonoBehaviour
{
    [SerializeField, Min(1f)] private float airAmount = 22f;
    [SerializeField, Min(0f)] private float rotationSpeed = 80f;
    [SerializeField, Min(0f)] private float bobHeight = 0.22f;
    [SerializeField, Min(0f)] private float bobSpeed = 3f;
    [SerializeField, Range(0f, 0.4f)] private float pulseAmount = 0.1f;

    private Vector3 startPosition;
    private Vector3 startScale;
    private bool collected;

    private void Awake()
    {
        startPosition = transform.position;
        startScale = transform.localScale;
        GetComponent<Collider>().isTrigger = true;
    }


    public void MoveAnimatedBaseTowards(Vector3 target, float maxDistanceDelta)
    {
        startPosition = Vector3.MoveTowards(startPosition, target, Mathf.Max(0f, maxDistanceDelta));
    }

    public void Configure(float amount)
    {
        airAmount = Mathf.Max(1f, amount);
    }

    private void Update()
    {
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);

        float phase = Time.time * bobSpeed + transform.position.z * 0.07f;
        float bob = Mathf.Sin(phase) * bobHeight;
        transform.position = startPosition + Vector3.up * bob;

        float pulse = 1f + Mathf.Sin(phase * 1.35f) * pulseAmount;
        transform.localScale = startScale * pulse;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || other == null)
        {
            return;
        }

        if (other.GetComponent<NearMissSensor>() != null)
        {
            return;
        }

        AirController airController = other.GetComponentInParent<AirController>();
        if (airController == null)
        {
            return;
        }

        collected = true;
        airController.AddAir(airAmount);

        ScoreController scoreController =
            other.GetComponentInParent<ScoreController>();

        if (scoreController != null)
        {
            scoreController.AddBonus(100);
        }

        ComboController comboController = other.GetComponentInParent<ComboController>();
        comboController?.RegisterSuccess(1, 60);
        GameAudioController.PlayPickup();

        RuntimeVfx.SpawnBurst(
            transform.position,
            new Color(0.2f, 0.82f, 1f, 1f),
            22,
            3.2f,
            0.15f,
            0.65f);
        CameraShakeController.ShakeGlobal(0.08f, 0.025f);
        Destroy(gameObject);
    }
}
