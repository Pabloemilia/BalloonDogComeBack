using UnityEngine;

/// <summary>Gentle first course using the purchased, scene-authored animated traps.</summary>
public sealed class BalloonDogEasyCourse : MonoBehaviour
{
    private void Start()
    {
        PlayerRunner runner = FindAnyObjectByType<PlayerRunner>();
        if (runner == null) return;
        runner.ConfigureForwardSpeed(5f);
        runner.GetComponent<PlayerHorizontalController>()?.ConfigureRoadLimit(2.25f);
        runner.GetComponent<DifficultyDirector>()?.Configure(BalloonDogMapFinale.FinishZ, 1.05f);

        // The placement parent is outside the Animator hierarchy. Root curves
        // can animate in local space without resetting the trap's map position.
        foreach (Transform placement in transform)
        {
            if (!placement.name.StartsWith("EasyTrap_")) continue;
            foreach (Collider oldCollider in placement.GetComponentsInChildren<Collider>())
                oldCollider.enabled = false;
            foreach (SmokeControl smoke in placement.GetComponentsInChildren<SmokeControl>())
                Destroy(smoke);
            foreach (Transform child in placement.GetComponentsInChildren<Transform>())
                if (child.name == "SmokeSpawner") child.gameObject.SetActive(false);
            foreach (ParticleSystem particles in placement.GetComponentsInChildren<ParticleSystem>())
                particles.gameObject.SetActive(false);
            foreach (Animator animator in placement.GetComponentsInChildren<Animator>())
            {
                animator.applyRootMotion = false;
                animator.speed = .55f;
            }

            // One hit owner per trap, with separate animated mesh hitboxes.
            // A single envelope collider would incorrectly block the gap
            // between the two hammers, even when that gap looks open.
            BoxCollider placeholder = placement.gameObject.AddComponent<BoxCollider>();
            placeholder.enabled = false;
            Rigidbody body = placement.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            placement.gameObject.AddComponent<Obstacle>().ConfigureNormal(8f, .75f, .25f);
            foreach (MeshFilter mesh in placement.GetComponentsInChildren<MeshFilter>())
            {
                if (mesh.sharedMesh == null || mesh.GetComponent<MeshRenderer>() == null) continue;
                Bounds bounds = mesh.sharedMesh.bounds;
                BoxCollider hitbox = mesh.gameObject.AddComponent<BoxCollider>();
                hitbox.center = bounds.center;
                hitbox.size = bounds.size * .9f;
                hitbox.isTrigger = true;
            }
        }
    }
}
