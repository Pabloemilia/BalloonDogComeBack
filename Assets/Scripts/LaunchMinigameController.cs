using System.Collections;
using TMPro;
using UnityEngine;

public sealed class LaunchMinigameController : MonoBehaviour
{
    [Header("Launch Tuning")]
    [SerializeField, Min(1f)] private float minimumForwardSpeed = 10f;
    [SerializeField, Min(1f)] private float maximumForwardSpeed = 34f;
    [SerializeField, Min(1f)] private float minimumUpwardSpeed = 5f;
    [SerializeField, Min(1f)] private float maximumUpwardSpeed = 14f;
    [SerializeField, Min(1f)] private float maximumFlightTime = 7f;
    [SerializeField, Min(1f)] private float metersPerMultiplier = 8f;
    [SerializeField, Min(1)] private int maximumMultiplier = 10;

    private GameObject player;
    private Rigidbody playerBody;
    private PlayerRunner runner;
    private PlayerHorizontalController horizontalController;
    private PlayerFormController formController;
    private BalloonSizeController sizeController;
    private AirController airController;
    private ScoreController scoreController;
    private RunnerCameraFollow cameraFollow;
    private GameManager gameManager;
    private TMP_Text launchText;
    private bool sequenceStarted;

    public void Configure(
        GameObject playerObject,
        Rigidbody body,
        PlayerRunner playerRunner,
        PlayerHorizontalController horizontal,
        PlayerFormController form,
        BalloonSizeController size,
        AirController air,
        ScoreController score,
        RunnerCameraFollow cameraController,
        GameManager manager,
        TMP_Text statusText)
    {
        player = playerObject;
        playerBody = body;
        runner = playerRunner;
        horizontalController = horizontal;
        formController = form;
        sizeController = size;
        airController = air;
        scoreController = score;
        cameraFollow = cameraController;
        gameManager = manager;
        launchText = statusText;

        if (launchText != null)
        {
            launchText.gameObject.SetActive(false);
        }
    }

    public void BeginLaunchSequence()
    {
        if (sequenceStarted)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            return;
        }

        sequenceStarted = true;
        StartCoroutine(LaunchRoutine());
    }

    private IEnumerator LaunchRoutine()
    {
        int baseScore = scoreController != null
            ? scoreController.CurrentScore
            : 0;

        float normalizedAir = airController != null
            ? airController.NormalizedAir
            : 0f;

        if (runner != null)
        {
            runner.SetMovementEnabled(false);
        }

        if (horizontalController != null)
        {
            horizontalController.enabled = false;
        }

        if (sizeController != null)
        {
            sizeController.SetShrinkRequested(false);
            sizeController.enabled = false;
        }

        if (formController != null)
        {
            formController.ForceBalloonForm();
            formController.enabled = false;
        }

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
            playerBody.isKinematic = true;
        }

        if (launchText != null)
        {
            launchText.gameObject.SetActive(true);
            launchText.text = $"KALAN HAVA %{normalizedAir * 100f:0}\nHAZIR!";
        }

        yield return new WaitForSeconds(0.65f);

        GameObject projectile = CreateLaunchObject();
        Vector3 startPosition = player != null
            ? player.transform.position + new Vector3(0f, 1.25f, 2f)
            : new Vector3(0f, 1.25f, 0f);

        projectile.transform.position = startPosition;

        Rigidbody projectileBody = projectile.GetComponent<Rigidbody>();
        float forwardSpeed = Mathf.Lerp(
            minimumForwardSpeed,
            maximumForwardSpeed,
            normalizedAir);
        float upwardSpeed = Mathf.Lerp(
            minimumUpwardSpeed,
            maximumUpwardSpeed,
            normalizedAir);

        projectileBody.linearVelocity = new Vector3(
            0f,
            upwardSpeed,
            forwardSpeed);
        projectileBody.angularVelocity = new Vector3(8f, 5f, 3f);

        if (cameraFollow != null)
        {
            cameraFollow.Configure(projectile.transform);
        }

        float elapsed = 0f;
        float maximumDistance = 0f;
        float lowMovementTime = 0f;

        while (elapsed < maximumFlightTime)
        {
            elapsed += Time.deltaTime;
            float distance = Mathf.Max(
                0f,
                projectile.transform.position.z - startPosition.z);
            maximumDistance = Mathf.Max(maximumDistance, distance);

            int liveMultiplier = CalculateMultiplier(maximumDistance);

            if (launchText != null)
            {
                launchText.text =
                    $"{maximumDistance:0.0} METRE\n" +
                    $"x{liveMultiplier}";
            }

            bool nearGround = projectile.transform.position.y <= 0.62f;
            bool movingSlowly = projectileBody.linearVelocity.magnitude <= 1.2f;

            lowMovementTime = nearGround && movingSlowly
                ? lowMovementTime + Time.deltaTime
                : 0f;

            if (elapsed > 1.2f && lowMovementTime >= 0.35f)
            {
                break;
            }

            yield return null;
        }

        int multiplier = CalculateMultiplier(maximumDistance);
        int finalScore = baseScore * multiplier;

        if (launchText != null)
        {
            launchText.text =
                $"{maximumDistance:0.0} METRE\n" +
                $"x{multiplier} ÇARPAN";
        }

        yield return new WaitForSeconds(0.75f);

        if (launchText != null)
        {
            launchText.gameObject.SetActive(false);
        }

        if (gameManager != null)
        {
            gameManager.TriggerLevelComplete(
                baseScore,
                maximumDistance,
                multiplier,
                finalScore);
        }
    }

    private int CalculateMultiplier(float distance)
    {
        int multiplier = 1 + Mathf.FloorToInt(
            distance / Mathf.Max(1f, metersPerMultiplier));

        return Mathf.Clamp(multiplier, 1, maximumMultiplier);
    }

    private static GameObject CreateLaunchObject()
    {
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "ScoreLaunchBall";
        projectile.transform.localScale = Vector3.one * 0.85f;

        Renderer renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = new Color(0.15f, 0.85f, 1f);
                renderer.material = material;
            }
        }

        Rigidbody body = projectile.AddComponent<Rigidbody>();
        body.mass = 0.7f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
        trail.time = 0.7f;
        trail.startWidth = 0.28f;
        trail.endWidth = 0.02f;
        trail.minVertexDistance = 0.1f;

        Shader trailShader = Shader.Find("Sprites/Default");
        if (trailShader != null)
        {
            trail.material = new Material(trailShader);
            trail.startColor = new Color(0.2f, 0.95f, 1f, 0.9f);
            trail.endColor = new Color(0.2f, 0.95f, 1f, 0f);
        }

        return projectile;
    }
}
