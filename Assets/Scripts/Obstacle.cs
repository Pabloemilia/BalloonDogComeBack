using System.Collections;
using UnityEngine;

public enum ObstacleType
{
    Normal,
    HeavyBrickWall
}

[RequireComponent(typeof(Collider))]
public sealed class Obstacle : MonoBehaviour
{
    [Header("Obstacle Type")]
    [SerializeField] private ObstacleType obstacleType;

    [Header("Normal Obstacle")]
    [SerializeField, Min(0f)] private float normalAirDamage = 10f;
    [SerializeField, Range(0f, 1f)] private float normalSlowMultiplier = 0.4f;
    [SerializeField, Min(0f)] private float normalSlowDuration = 0.8f;

    [Header("Heavy Brick Wall")]
    [SerializeField, Min(0f)] private float requiredAir = 85f;
    [SerializeField, Min(0f)] private float breakAirCost = 25f;
    [SerializeField, Min(0f)] private float failedHitAirDamage = 15f;
    [SerializeField, Range(0f, 1f)] private float failedSlowMultiplier = 0.15f;
    [SerializeField, Min(0f)] private float failedSlowDuration = 1.2f;

    [Header("Optional Effect")]
    [SerializeField] private ParticleSystem breakEffectPrefab;

    private bool hasBeenHit;

    public ObstacleType Type => obstacleType;

    public void ConfigureNormal(
        float airDamage = 10f,
        float slowMultiplier = 0.4f,
        float slowDuration = 0.8f)
    {
        obstacleType = ObstacleType.Normal;
        normalAirDamage = airDamage;
        normalSlowMultiplier = slowMultiplier;
        normalSlowDuration = slowDuration;
        GetComponent<Collider>().isTrigger = true;
    }

    public void ConfigureHeavy(
        float minimumAir = 85f,
        float airCost = 25f,
        float failedDamage = 15f)
    {
        obstacleType = ObstacleType.HeavyBrickWall;
        requiredAir = minimumAir;
        breakAirCost = airCost;
        failedHitAirDamage = failedDamage;
        GetComponent<Collider>().isTrigger = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        ResolveContact(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        ResolveContact(other);
    }

    private void ResolveContact(Collider other)
    {
        if (hasBeenHit || other == null)
        {
            return;
        }

        // NearMissSensor büyük bir trigger alanıdır. Eski sürümde bu sensör
        // gerçek çarpışma sanıldığı için oyuncu bütün engellere vurmuş gibi oluyordu.
        if (other.GetComponent<NearMissSensor>() != null)
        {
            return;
        }

        AirController airController = other.GetComponentInParent<AirController>();
        if (airController == null)
        {
            return;
        }

        PlayerRunner playerRunner = other.GetComponentInParent<PlayerRunner>();
        other.GetComponentInParent<ComboController>()?.BreakCombo();
        other.GetComponentInParent<NearMissSensor>()?.RegisterHit(this);
        GameAudioController.PlayHit();

        switch (obstacleType)
        {
            case ObstacleType.Normal:
                HandleNormalObstacle(airController, playerRunner);
                break;

            case ObstacleType.HeavyBrickWall:
                HandleHeavyWall(airController, playerRunner);
                break;
        }
    }

    private void HandleNormalObstacle(
        AirController airController,
        PlayerRunner playerRunner)
    {
        hasBeenHit = true;
        airController.RemoveAir(normalAirDamage);

        if (playerRunner != null)
        {
            playerRunner.ApplySlow(normalSlowMultiplier, normalSlowDuration);
        }

        RuntimeVfx.SpawnBurst(
            transform.position + Vector3.up * 0.5f,
            new Color(1f, 0.48f, 0.18f, 1f),
            14,
            2.4f,
            0.13f,
            0.5f);
        CameraShakeController.ShakeGlobal(0.16f, 0.085f);
        StartCoroutine(NormalImpactRoutine());
    }

    private void HandleHeavyWall(
        AirController airController,
        PlayerRunner playerRunner)
    {
        hasBeenHit = true;

        if (airController.HasAir(requiredAir))
        {
            airController.RemoveAir(breakAirCost);
            CameraShakeController.ShakeGlobal(0.24f, 0.12f);
            BreakWall();
            return;
        }

        airController.RemoveAir(failedHitAirDamage);

        if (playerRunner != null)
        {
            playerRunner.ApplySlow(failedSlowMultiplier, failedSlowDuration);
        }

        RuntimeVfx.SpawnBurst(
            transform.position + Vector3.up * 0.75f,
            new Color(1f, 0.25f, 0.12f, 1f),
            18,
            2.8f,
            0.16f,
            0.6f);
        CameraShakeController.ShakeGlobal(0.24f, 0.14f);
        StartCoroutine(HeavyFailureRoutine());
    }

    private IEnumerator NormalImpactRoutine()
    {
        Collider obstacleCollider = GetComponent<Collider>();
        obstacleCollider.enabled = false;

        Vector3 initialScale = transform.localScale;
        float elapsed = 0f;
        const float duration = 0.25f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(initialScale, initialScale * 0.2f, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator HeavyFailureRoutine()
    {
        // Oyuncunun duvara takılı kalmaması için kısa bir süre sonra
        // çarpışmayı kapatır. Görsel duvar sahnede kalır.
        yield return new WaitForSeconds(0.55f);
        Collider obstacleCollider = GetComponent<Collider>();
        if (obstacleCollider != null)
        {
            obstacleCollider.enabled = false;
        }
    }

    private void BreakWall()
    {
        if (breakEffectPrefab != null)
        {
            ParticleSystem createdEffect = Instantiate(
                breakEffectPrefab,
                transform.position,
                transform.rotation);

            Destroy(createdEffect.gameObject, 3f);
        }

        RuntimeVfx.SpawnBurst(
            transform.position + Vector3.up,
            new Color(1f, 0.68f, 0.2f, 1f),
            28,
            4.2f,
            0.18f,
            0.8f);
        CameraShakeController.ShakeGlobal(0.2f, 0.12f);
        SpawnSimpleDebris();
        Destroy(gameObject);
    }

    private void SpawnSimpleDebris()
    {
        Renderer sourceRenderer = GetComponent<Renderer>();
        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponentInChildren<Renderer>();
        }

        Material sharedMaterial = sourceRenderer != null
            ? sourceRenderer.sharedMaterial
            : null;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = "BrickDebris";
                piece.transform.position = transform.position +
                    new Vector3(x * 0.45f, (y - 0.5f) * 0.45f, 0f);
                piece.transform.localScale = new Vector3(0.38f, 0.3f, 0.28f);

                if (sharedMaterial != null)
                {
                    piece.GetComponent<Renderer>().sharedMaterial = sharedMaterial;
                }

                Rigidbody body = piece.AddComponent<Rigidbody>();
                body.mass = 0.25f;
                body.AddExplosionForce(180f, transform.position - Vector3.forward, 4f, 1f);
                Destroy(piece, 2f);
            }
        }
    }
}
