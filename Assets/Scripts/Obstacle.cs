using UnityEngine;

public enum ObstacleType
{
    Normal,
    HeavyBrickWall
}

public sealed class Obstacle : MonoBehaviour
{
    [Header("Obstacle Type")]
    [SerializeField]
    private ObstacleType obstacleType;

    [Header("Normal Obstacle")]
    [SerializeField, Min(0f)]
    private float normalAirDamage = 10f;

    [SerializeField, Range(0f, 1f)]
    private float normalSlowMultiplier = 0.4f;

    [SerializeField, Min(0f)]
    private float normalSlowDuration = 0.8f;

    [Header("Heavy Brick Wall")]
    [SerializeField, Min(0f)]
    private float requiredAir = 85f;

    [SerializeField, Min(0f)]
    private float breakAirCost = 25f;

    [SerializeField, Min(0f)]
    private float failedHitAirDamage = 15f;

    [SerializeField, Range(0f, 1f)]
    private float failedSlowMultiplier = 0.15f;

    [SerializeField, Min(0f)]
    private float failedSlowDuration = 1.2f;

    [Header("Optional Effect")]
    [SerializeField]
    private ParticleSystem breakEffectPrefab;

    private bool hasBeenHit;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasBeenHit)
        {
            return;
        }

        AirController airController =
            collision.collider
                .GetComponentInParent<AirController>();

        if (airController == null)
        {
            return;
        }

        PlayerRunner playerRunner =
            collision.collider
                .GetComponentInParent<PlayerRunner>();

        switch (obstacleType)
        {
            case ObstacleType.Normal:
                HandleNormalObstacle(
                    airController,
                    playerRunner
                );
                break;

            case ObstacleType.HeavyBrickWall:
                HandleHeavyWall(
                    airController,
                    playerRunner
                );
                break;
        }
    }

    private void HandleNormalObstacle(
        AirController airController,
        PlayerRunner playerRunner
    )
    {
        hasBeenHit = true;

        airController.RemoveAir(
            normalAirDamage
        );

        if (playerRunner != null)
        {
            playerRunner.ApplySlow(
                normalSlowMultiplier,
                normalSlowDuration
            );
        }
    }

    private void HandleHeavyWall(
        AirController airController,
        PlayerRunner playerRunner
    )
    {
        hasBeenHit = true;

        if (airController.HasAir(requiredAir))
        {
            airController.RemoveAir(
                breakAirCost
            );

            BreakWall();
            return;
        }

        airController.RemoveAir(
            failedHitAirDamage
        );

        if (playerRunner != null)
        {
            playerRunner.ApplySlow(
                failedSlowMultiplier,
                failedSlowDuration
            );
        }
    }

    private void BreakWall()
    {
        if (breakEffectPrefab != null)
        {
            ParticleSystem createdEffect =
                Instantiate(
                    breakEffectPrefab,
                    transform.position,
                    transform.rotation
                );

            Destroy(
                createdEffect.gameObject,
                3f
            );
        }

        Destroy(gameObject);
    }
}