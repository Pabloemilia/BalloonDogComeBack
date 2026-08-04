using System.Collections;
using TMPro;
using UnityEngine;

public sealed class LaunchMinigameController : MonoBehaviour
{
    [Header("Launch Tuning")]
    [SerializeField, Min(1f)] private float minimumForwardSpeed = 9f;
    [SerializeField, Min(1f)] private float maximumForwardSpeed = 50f;
    [SerializeField, Min(1f)] private float minimumUpwardSpeed = 5.2f;
    [SerializeField, Min(1f)] private float maximumUpwardSpeed = 14.5f;
    [SerializeField, Min(1f)] private float maximumFlightTime = 8f;
    [SerializeField, Min(1f)] private float metersPerMultiplier = 13.5f;
    [SerializeField, Min(1)] private int maximumMultiplier = 10;
    [SerializeField, Min(0f)] private float launchOffsetAfterFinish = 2.25f;
    [SerializeField, Min(0.1f)] private float launchHeight = 1.35f;

    [Header("Final Multiplier Track")]
    [SerializeField, Min(4f)] private float finalTrackWidth = 6.4f;
    [SerializeField, Min(1f)] private float finalWallHeight = 5f;
    [SerializeField, Min(0.25f)] private float finalWallThickness = 1.1f;
    [SerializeField, Min(0.01f)] private float finalTrackSurfaceHeight = 0.08f;

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
    private Transform launchPoint;
    private bool sequenceStarted;
    private float finalWallZ = float.PositiveInfinity;

    private void Awake()
    {
        maximumMultiplier = 10;
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
        EnsureFinalMultiplierTrack(ResolveLaunchStartPosition());
    }

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
        ResolveReferences();

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

    public void ConfigureMultiplierTrack(float distancePerMultiplier, int maxMultiplier)
    {
        metersPerMultiplier = Mathf.Max(1f, distancePerMultiplier);
        maximumMultiplier = Mathf.Max(1, maxMultiplier);
    }

    public void ConfigureLaunchPoint(Transform point)
    {
        launchPoint = point;
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
            sizeController.CancelShrinkImmediately();
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

        RemoveOldLaunchObjects();

        if (launchText != null)
        {
            launchText.gameObject.SetActive(true);
            launchText.text = $"KALAN HAVA %{normalizedAir * 100f:0}\nHAZIR!";
        }

        yield return new WaitForSeconds(0.65f);

        Vector3 startPosition = ResolveLaunchStartPosition();
        EnsureFinalMultiplierTrack(startPosition);

        GameObject projectile = CreateLaunchObject(startPosition);
        Rigidbody projectileBody = projectile.GetComponent<Rigidbody>();

        Physics.SyncTransforms();

        if (cameraFollow != null)
        {
            cameraFollow.Configure(projectile.transform);
            cameraFollow.SnapToTarget();
        }

        projectileBody.isKinematic = false;
        projectileBody.interpolation = RigidbodyInterpolation.Interpolate;

        float forwardSpeed;
        float upwardSpeed;

        // 90% ve üzeri hava, pistin son duvarına ulaşabilecek kadar güçlü olsun.
        // Daha düşük hava değerleri ise eskisine göre belirgin biçimde daha kısa
        // gitsin; böylece 10X her durumda otomatik olmasın.
        if (normalizedAir >= 0.9f)
        {
            float topRange = Mathf.InverseLerp(0.9f, 1f, normalizedAir);
            forwardSpeed = Mathf.Lerp(37f, maximumForwardSpeed, topRange);
            upwardSpeed = Mathf.Lerp(11.2f, maximumUpwardSpeed, topRange);
        }
        else
        {
            float reduced = Mathf.Pow(
                Mathf.Clamp01(normalizedAir / 0.9f),
                1.18f);

            forwardSpeed = Mathf.Lerp(
                minimumForwardSpeed,
                30f,
                reduced);

            upwardSpeed = Mathf.Lerp(
                minimumUpwardSpeed,
                10.8f,
                reduced);
        }

        projectileBody.linearVelocity = new Vector3(
            0f,
            upwardSpeed,
            forwardSpeed);

        projectileBody.angularVelocity = new Vector3(8f, 5f, 3f);

        RuntimeVfx.SpawnBurst(
            startPosition,
            new Color(0.18f, 0.9f, 1f, 1f),
            32,
            5.2f,
            0.15f,
            0.75f);

        CameraShakeController.ShakeGlobal(0.24f, 0.1f);

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

            if (projectile.transform.position.z >= finalWallZ - 0.5f)
            {
                Vector3 stoppedPosition = projectile.transform.position;
                stoppedPosition.z = finalWallZ - 0.5f;
                projectile.transform.position = stoppedPosition;

                projectileBody.linearVelocity = Vector3.zero;
                projectileBody.angularVelocity = Vector3.zero;
                projectileBody.isKinematic = true;
                break;
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

        RuntimeVfx.SpawnBurst(
            projectile.transform.position,
            new Color(1f, 0.7f, 0.12f, 1f),
            28,
            4.2f,
            0.14f,
            0.7f);

        CameraShakeController.ShakeGlobal(0.28f, 0.12f);

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

    private Vector3 ResolveLaunchStartPosition()
    {
        FinishLine[] finishLines = FindObjectsByType<FinishLine>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        FinishLine furthestFinish = null;
        float furthestFinishZ = float.NegativeInfinity;

        foreach (FinishLine candidate in finishLines)
        {
            if (candidate != null && candidate.transform.position.z > furthestFinishZ)
            {
                furthestFinish = candidate;
                furthestFinishZ = candidate.transform.position.z;
            }
        }

        Vector3 anchor = player != null
            ? player.transform.position
            : new Vector3(0f, 0f, 185f);

        float requiredZ = anchor.z + launchOffsetAfterFinish;

        if (furthestFinish != null)
        {
            Vector3 finishPosition = furthestFinish.transform.position;

            if (finishPosition.z >= anchor.z - 5f)
            {
                anchor.x = finishPosition.x;
                anchor.y = finishPosition.y;
            }

            requiredZ = Mathf.Max(
                requiredZ,
                finishPosition.z + launchOffsetAfterFinish);
        }

        // Eski sahnede LaunchPoint yanlışlıkla pistin başına bağlandıysa onu yok say.
        // Yalnızca oyuncunun/bitişin gerisinde değilse kullan.
        if (launchPoint != null &&
            launchPoint.position.z >= requiredZ - launchOffsetAfterFinish - 1f)
        {
            anchor.x = launchPoint.position.x;
            anchor.y = launchPoint.position.y;
            requiredZ = Mathf.Max(requiredZ, launchPoint.position.z);
        }

        anchor.z = requiredZ;
        anchor.y = Mathf.Max(launchHeight, anchor.y + launchHeight);
        return anchor;
    }

    private void EnsureFinalMultiplierTrack(Vector3 launchStart)
    {
        GameObject existingRoot = GameObject.Find("LaunchMultiplierTrackV10");
        if (existingRoot != null)
        {
            Transform existingWall = existingRoot.transform.Find("FinalStopWall");
            if (existingWall != null)
            {
                finalWallZ =
                    existingWall.position.z - finalWallThickness * 0.5f;
            }

            return;
        }

        DisableLegacyMultiplierLabels(launchStart.z);

        GameObject root = new GameObject("LaunchMultiplierTrackV10");
        float segmentLength = Mathf.Max(1f, metersPerMultiplier);
        int segmentCount = 10;
        float totalLength = segmentLength * segmentCount;
        float trackCenterZ = launchStart.z + totalLength * 0.5f;

        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "FinalMultiplierRoad";
        road.transform.SetParent(root.transform, false);
        road.transform.position = new Vector3(
            launchStart.x,
            -0.1f,
            trackCenterZ);
        road.transform.localScale = new Vector3(
            finalTrackWidth,
            0.2f,
            totalLength + 1f);
        ApplyColor(
            road,
            new Color(0.06f, 0.16f, 0.24f, 1f));

        for (int multiplier = 1; multiplier <= 10; multiplier++)
        {
            float segmentCenterZ =
                launchStart.z +
                (multiplier - 0.5f) * segmentLength;

            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = $"MultiplierPad_{multiplier}X";
            pad.transform.SetParent(root.transform, false);
            pad.transform.position = new Vector3(
                launchStart.x,
                finalTrackSurfaceHeight,
                segmentCenterZ);
            pad.transform.localScale = new Vector3(
                finalTrackWidth - 0.24f,
                0.035f,
                segmentLength - 0.22f);

            Color padColor = Color.Lerp(
                new Color(0.08f, 0.62f, 0.94f, 1f),
                new Color(1f, 0.55f, 0.10f, 1f),
                (multiplier - 1f) / 9f);

            ApplyColor(pad, padColor);

            Collider padCollider = pad.GetComponent<Collider>();
            if (padCollider != null)
            {
                Destroy(padCollider);
            }

            CreateGroundMultiplierText(
                root.transform,
                launchStart.x,
                segmentCenterZ,
                multiplier);
        }

        CreateSideRail(
            root.transform,
            launchStart.x - finalTrackWidth * 0.5f - 0.15f,
            trackCenterZ,
            totalLength);

        CreateSideRail(
            root.transform,
            launchStart.x + finalTrackWidth * 0.5f + 0.15f,
            trackCenterZ,
            totalLength);

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "FinalStopWall";
        wall.transform.SetParent(root.transform, false);
        wall.transform.position = new Vector3(
            launchStart.x,
            finalWallHeight * 0.5f,
            launchStart.z + totalLength + finalWallThickness * 0.5f);
        wall.transform.localScale = new Vector3(
            finalTrackWidth + 1.2f,
            finalWallHeight,
            finalWallThickness);
        ApplyColor(
            wall,
            new Color(0.05f, 0.12f, 0.22f, 1f));

        finalWallZ =
            wall.transform.position.z - finalWallThickness * 0.5f;

        CreateWallLabel(wall.transform);
    }

    private static void DisableLegacyMultiplierLabels(float minimumZ)
    {
        TextMeshPro[] labels = FindObjectsByType<TextMeshPro>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (TextMeshPro label in labels)
        {
            if (label == null || label.transform.position.z < minimumZ - 5f)
            {
                continue;
            }

            string value = label.text != null
                ? label.text.Trim().ToUpperInvariant()
                : string.Empty;

            bool looksLikeMultiplier =
                value.EndsWith("X") &&
                int.TryParse(
                    value.Substring(0, value.Length - 1),
                    out int parsed) &&
                parsed >= 1 &&
                parsed <= 10;

            if (looksLikeMultiplier)
            {
                label.gameObject.SetActive(false);
            }
        }
    }

    private static void CreateGroundMultiplierText(
        Transform parent,
        float x,
        float z,
        int multiplier)
    {
        GameObject textObject = new GameObject(
            $"MultiplierText_{multiplier}X",
            typeof(RectTransform),
            typeof(TextMeshPro));

        textObject.transform.SetParent(parent, false);
        textObject.transform.position = new Vector3(x, 0.12f, z);
        textObject.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        TextMeshPro text = textObject.GetComponent<TextMeshPro>();
        text.text = $"{multiplier}X";
        text.fontSize = multiplier == 10 ? 4.6f : 5.2f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.rectTransform.sizeDelta = new Vector2(6f, 4f);
        text.raycastTarget = false;
    }

    private static void CreateSideRail(
        Transform parent,
        float x,
        float centerZ,
        float length)
    {
        GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.name = "FinalTrackRail";
        rail.transform.SetParent(parent, false);
        rail.transform.position = new Vector3(x, 0.28f, centerZ);
        rail.transform.localScale = new Vector3(0.22f, 0.55f, length + 1f);
        ApplyColor(
            rail,
            new Color(0.12f, 0.78f, 0.96f, 1f));
    }

    private static void CreateWallLabel(Transform wall)
    {
        GameObject textObject = new GameObject(
            "FinalWallLabel",
            typeof(RectTransform),
            typeof(TextMeshPro));

        textObject.transform.SetParent(wall, false);
        textObject.transform.localPosition = new Vector3(0f, 0.1f, -0.56f);
        textObject.transform.localRotation = Quaternion.identity;
        textObject.transform.localScale = new Vector3(
            0.14f,
            0.14f,
            0.14f);

        TextMeshPro text = textObject.GetComponent<TextMeshPro>();
        text.text = "10X MAX";
        text.fontSize = 8f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.rectTransform.sizeDelta = new Vector2(40f, 10f);
        text.raycastTarget = false;
    }

    private static void ApplyColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            return;
        }

        Material material = new Material(shader);
        material.color = color;
        renderer.material = material;
    }

    private static void RemoveOldLaunchObjects()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (GameObject candidate in allObjects)
        {
            if (candidate != null && candidate.name == "ScoreLaunchBall")
            {
                Destroy(candidate);
            }
        }
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            PlayerRunner foundRunner = FindAnyObjectByType<PlayerRunner>();
            if (foundRunner != null)
            {
                player = foundRunner.gameObject;
            }
        }

        if (player != null)
        {
            playerBody ??= player.GetComponent<Rigidbody>();
            runner ??= player.GetComponent<PlayerRunner>();
            horizontalController ??= player.GetComponent<PlayerHorizontalController>();
            formController ??= player.GetComponent<PlayerFormController>();
            sizeController ??= player.GetComponent<BalloonSizeController>();
            airController ??= player.GetComponent<AirController>();
            scoreController ??= player.GetComponent<ScoreController>();
        }

        cameraFollow ??= FindAnyObjectByType<RunnerCameraFollow>();
        gameManager ??= GameManager.Instance ?? FindAnyObjectByType<GameManager>();

        if (launchText == null)
        {
            TMP_Text[] texts = FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (TMP_Text text in texts)
            {
                if (text != null && text.name == "LaunchResultText")
                {
                    launchText = text;
                    break;
                }
            }
        }
    }

    private int CalculateMultiplier(float distance)
    {
        int multiplier = 1 + Mathf.FloorToInt(
            distance / Mathf.Max(1f, metersPerMultiplier));

        return Mathf.Clamp(multiplier, 1, maximumMultiplier);
    }

    private static GameObject CreateLaunchObject(Vector3 startPosition)
    {
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "ScoreLaunchBall";
        projectile.transform.SetPositionAndRotation(startPosition, Quaternion.identity);
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

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "LaunchHalo";
        ring.transform.SetParent(projectile.transform, false);
        ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ring.transform.localScale = new Vector3(1.15f, 0.035f, 1.15f);

        Renderer ringRenderer = ring.GetComponent<Renderer>();
        if (ringRenderer != null && renderer != null)
        {
            ringRenderer.material = renderer.material;
        }

        Collider ringCollider = ring.GetComponent<Collider>();
        if (ringCollider != null)
        {
            Destroy(ringCollider);
        }

        Rigidbody body = projectile.AddComponent<Rigidbody>();
        body.mass = 0.7f;
        body.position = startPosition;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.None;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearDamping = 0.26f;
        body.angularDamping = 0.18f;

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
