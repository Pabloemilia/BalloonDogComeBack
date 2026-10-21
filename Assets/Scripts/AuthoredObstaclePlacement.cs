using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Keeps scene placement outside the purchased clips' root transform curves.</summary>
[DisallowMultipleComponent]
public sealed class AuthoredObstaclePlacement : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallScene();
    }

    // sceneLoaded runs before Start and before the first Animator evaluation.
    // The fallback also covers callers using the old obstacle setup entry point.
    public static void InstallScene()
    {
        foreach (Animator animator in Object.FindObjectsByType<Animator>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            PreservePlacement(animator);
    }

    public static Transform PreservePlacement(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return null;
        if (animator.GetComponentInParent<BalloonDogEndlessRunner>() != null) return null;
        bool purchasedTrap = false;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            if (clip != null && clip.name.StartsWith("Obstacle ")) purchasedTrap = true;
        if (!purchasedTrap) return null;
        Transform root = animator.transform;
        if (root.parent != null && root.parent.GetComponent<AuthoredObstaclePlacement>() != null)
            return root.parent;

        // Transfer the FULL authored local TRS, including a prefab's nonzero
        // offset inside EasyTrap. Nothing is fitted, resized or recentered.
        Transform previousParent = root.parent;
        int sibling = root.GetSiblingIndex();
        Vector3 position = root.localPosition;
        Quaternion rotation = root.localRotation;
        Vector3 scale = root.localScale;
        GameObject container = new GameObject("AuthoredPlacement_" + root.name);
        container.SetActive(root.gameObject.activeSelf);
        Transform placement = container.transform;
        placement.SetParent(previousParent, false);
        placement.SetLocalPositionAndRotation(position, rotation);
        placement.localScale = scale;
        placement.SetSiblingIndex(sibling);
        placement.gameObject.AddComponent<AuthoredObstaclePlacement>();
        root.SetParent(placement, false);
        root.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.localScale = Vector3.one;
        animator.applyRootMotion = false;
        animator.speed = .55f;
        if (Application.isPlaying) ConfigureHitboxes(placement);
        return placement;
    }

    private static void ConfigureHitboxes(Transform placement)
    {
        foreach (Collider collider in placement.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (SmokeControl smoke in placement.GetComponentsInChildren<SmokeControl>(true))
            Destroy(smoke);
        foreach (Transform child in placement.GetComponentsInChildren<Transform>(true))
            if (child.name == "SmokeSpawner") child.gameObject.SetActive(false);
        foreach (ParticleSystem particles in placement.GetComponentsInChildren<ParticleSystem>(true))
            particles.gameObject.SetActive(false);

        // One contact owner per prefab, even when several moving parts touch it.
        BoxCollider placeholder = placement.gameObject.AddComponent<BoxCollider>();
        placeholder.enabled = false;
        Rigidbody body = placement.gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        placement.gameObject.AddComponent<Obstacle>().ConfigureNormal(8f, .75f, .25f);
        foreach (MeshFilter mesh in placement.GetComponentsInChildren<MeshFilter>(true))
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
