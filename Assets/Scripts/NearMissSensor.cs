using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyuncu engeli çarpmadan çok yakın geçtiğinde combo kazandırır.
/// Transform referansları kullandığı için Unity sürümüne bağlı instance-id API'lerine ihtiyaç duymaz.
/// </summary>
public sealed class NearMissSensor : MonoBehaviour
{
    private readonly HashSet<Transform> candidates = new HashSet<Transform>();
    private readonly HashSet<Transform> collided = new HashSet<Transform>();

    private ComboController comboController;
    private Transform player;

    public void Configure(Transform playerTransform, ComboController combo)
    {
        player = playerTransform;
        comboController = combo;
    }

    private void Awake()
    {
        player ??= transform.root;
        comboController ??= GetComponentInParent<ComboController>();
    }

    public void RegisterHit(Component obstacleComponent)
    {
        Transform obstacle = obstacleComponent != null
            ? obstacleComponent.transform
            : null;

        if (obstacle != null)
        {
            collided.Add(obstacle);
            collided.Add(obstacle.root);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Transform obstacle = ResolveObstacle(other);
        if (obstacle != null)
        {
            candidates.Add(obstacle);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Transform obstacle = ResolveObstacle(other);
        if (obstacle == null || !candidates.Remove(obstacle))
        {
            return;
        }

        if (collided.Remove(obstacle) || collided.Remove(obstacle.root))
        {
            return;
        }

        if (player != null && obstacle.position.z < player.position.z + 0.8f)
        {
            comboController?.RegisterSuccess(1, 75);
            RuntimeVfx.SpawnBurst(
                player.position + Vector3.up * 1.2f,
                new Color(1f, 0.82f, 0.18f, 1f),
                10,
                2.1f,
                0.09f,
                0.42f);
        }
    }

    private static Transform ResolveObstacle(Collider other)
    {
        if (other == null)
        {
            return null;
        }

        Obstacle obstacle = other.GetComponentInParent<Obstacle>();
        if (obstacle != null)
        {
            return obstacle.transform;
        }

        GroundSpikes spikes = other.GetComponentInParent<GroundSpikes>();
        return spikes != null ? spikes.transform : null;
    }
}
