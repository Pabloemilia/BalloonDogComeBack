using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class AirPickup : MonoBehaviour
{
    [SerializeField, Min(1f)] private float airAmount = 22f;
    [SerializeField, Min(0f)] private float rotationSpeed = 100f;
    [SerializeField, Min(0f)] private float bobHeight = 0.2f;
    [SerializeField, Min(0f)] private float bobSpeed = 3f;

    private Vector3 startPosition;
    private bool collected;

    private void Awake()
    {
        startPosition = transform.position;
        GetComponent<Collider>().isTrigger = true;
    }

    public void Configure(float amount)
    {
        airAmount = Mathf.Max(1f, amount);
    }

    private void Update()
    {
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = startPosition + Vector3.up * bob;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
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

        Destroy(gameObject);
    }
}
