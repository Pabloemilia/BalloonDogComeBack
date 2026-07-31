using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class BalloonDogPrototypeBootstrap : MonoBehaviour
{
    private const string GeneratedLevelName = "__GeneratedBalloonDogLevelV2";

    private static Material cyanMaterial;
    private static Material redMaterial;
    private static Material orangeMaterial;
    private static Material darkMaterial;
    private static Material whiteMaterial;
    private static Material roadMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateBootstrap()
    {
        PlayerRunner runner = FindFirstObjectByType<PlayerRunner>();
        GameObject playerByName = GameObject.Find("player") ?? GameObject.Find("Player");

        if (runner == null && playerByName == null)
        {
            return;
        }

        if (FindFirstObjectByType<BalloonDogPrototypeBootstrap>() != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject("__BalloonDogPrototypeBootstrapV2");
        bootstrapObject.AddComponent<BalloonDogPrototypeBootstrap>();
    }

    private IEnumerator Start()
    {
        // Unity'nin sahneyi ve Resources modellerini tamamen hazırlamasını bekler.
        yield return null;
        SetupPrototype();
    }

    private void SetupPrototype()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        EnsureMaterials();

        GameObject player = ResolvePlayer();
        if (player == null)
        {
            Debug.LogError("BalloonDog kurulumu: player nesnesi bulunamadı.");
            return;
        }

        Rigidbody body = GetOrAdd<Rigidbody>(player);
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints = RigidbodyConstraints.FreezeRotation;

        PlayerRunner runner = GetOrAdd<PlayerRunner>(player);
        AirController airController = GetOrAdd<AirController>(player);
        ScoreController scoreController = GetOrAdd<ScoreController>(player);
        PlayerFormController formController = GetOrAdd<PlayerFormController>(player);
        BalloonSizeController sizeController = GetOrAdd<BalloonSizeController>(player);
        PlayerHorizontalController horizontalController =
            GetOrAdd<PlayerHorizontalController>(player);

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            GameObject managerObject = new GameObject("GameManager");
            gameManager = managerObject.AddComponent<GameManager>();
        }

        airController.Configure(gameManager);

        Transform dogVisual = SetupPlayerVisual(player.transform);
        sizeController.Configure(dogVisual, formController, airController);

        Transform rotor = CreateRotor(player.transform);
        formController.ConfigureRotor(rotor);

        RunnerCameraFollow cameraFollow = SetupCamera(player.transform);

        DisableLegacyWorldObjects();
        SetupLongLevel();

        SetupInterface(
            player,
            body,
            airController,
            scoreController,
            formController,
            sizeController,
            gameManager,
            runner,
            horizontalController,
            cameraFollow);

        EnsureEventSystem();

        Debug.Log(
            "BalloonDog V2 hazır. Sürükle: sağ/sol, çift dokun: helikopter, " +
            "alttaki butona basılı tut: küçül ve hava kaybet.");
    }

    private static GameObject ResolvePlayer()
    {
        PlayerRunner runner = FindFirstObjectByType<PlayerRunner>();
        if (runner != null)
        {
            return runner.gameObject;
        }

        return GameObject.Find("player") ?? GameObject.Find("Player");
    }

    private static Transform SetupPlayerVisual(Transform player)
    {
        foreach (Renderer renderer in player.GetComponents<Renderer>())
        {
            renderer.enabled = false;
        }

        Transform bubble = FindChildByName(player, "Bubble");
        if (bubble != null)
        {
            foreach (Renderer renderer in bubble.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }

            foreach (Collider childCollider in bubble.GetComponentsInChildren<Collider>(true))
            {
                childCollider.enabled = false;
            }
        }

        Transform existing = FindChildByName(player, "BalloonDogModel");
        if (existing != null)
        {
            return existing;
        }

        GameObject modelAsset = Resources.Load<GameObject>(
            "Models/BalloonDog/BalloonDog");

        if (modelAsset == null)
        {
            Debug.LogWarning(
                "BalloonDog modeli Resources altında bulunamadı; geçici Bubble kullanılıyor.");

            if (bubble != null)
            {
                foreach (Renderer renderer in bubble.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = true;
                }

                return bubble;
            }

            return player;
        }

        GameObject model = Instantiate(modelAsset, player);
        model.name = "BalloonDogModel";
        model.transform.localPosition = new Vector3(0f, -1f, 0f);
        model.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        model.transform.localScale = Vector3.one * 0.9f;

        foreach (Collider modelCollider in model.GetComponentsInChildren<Collider>(true))
        {
            modelCollider.enabled = false;
        }

        UpgradeImportedMaterials(model);
        return model.transform;
    }

    private static RunnerCameraFollow SetupCamera(Transform player)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.tag = "MainCamera";
        }

        RunnerCameraFollow follow = GetOrAdd<RunnerCameraFollow>(mainCamera.gameObject);
        follow.Configure(player);
        mainCamera.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
        mainCamera.farClipPlane = 600f;
        return follow;
    }

    private static void DisableLegacyWorldObjects()
    {
        string[] names =
        {
            "NormalObstacle",
            "LeftObstacle",
            "RightObstacle",
            "HeavyBrickWall",
            "LowGate",
            "PrototypeNormalObstacle",
            "GroundSpikes",
            "FinishLine",
            "AirPickup_A",
            "AirPickup_B",
            "AirPickup_C",
            GeneratedLevelName
        };

        foreach (string objectName in names)
        {
            GameObject gameObject = GameObject.Find(objectName);
            if (gameObject == null)
            {
                continue;
            }

            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    private static void SetupLongLevel()
    {
        PrepareRoad();

        GameObject levelRoot = new GameObject(GeneratedLevelName);

        // Oyunun ilk yarısı: temel kaçış ve hava toplama.
        CreateNormalObstacle(levelRoot.transform, "Bomb_15", new Vector3(-1.55f, 0f, 15f),
            "Models/ObstaclePack/Bomb/Bomb", new Vector3(1.4f, 1.65f, 1.4f),
            new Vector3(0f, 0.82f, 0f), 11f, 1f);
        CreateAirPickup(levelRoot.transform, "BlueBalloon_21", new Vector3(1.55f, 1.2f, 21f), 22f);
        CreateNormalObstacle(levelRoot.transform, "Cylinder_27", new Vector3(1.5f, 0f, 27f),
            "Models/ObstaclePack/Cylinder/Cylinder", new Vector3(1.45f, 2.5f, 1.45f),
            new Vector3(0f, 1.25f, 0f), 12f, 0.75f);
        CreateSpikeStrip(levelRoot.transform, "Spikes_35", new Vector3(0f, 0f, 35f));
        CreateAirPickupRow(levelRoot.transform, 44f, new[] { -1.55f, 0f, 1.55f }, 15f);
        CreateLowGate(levelRoot.transform, "LowGate_47", 47f);

        // Orta bölüm: ağır duvar ve karışık engeller.
        CreateHeavyBalloonWall(levelRoot.transform, "HeavyWall_52", new Vector3(-1.45f, 0f, 52f));
        CreateNormalObstacle(levelRoot.transform, "Gear_60", new Vector3(1.4f, 0f, 60f),
            "Models/ObstaclePack/Gear/Gear", new Vector3(2f, 0.8f, 2f),
            new Vector3(0f, 0.4f, 0f), 10f, 0.68f);
        CreateNormalObstacle(levelRoot.transform, "BombLeft_68", new Vector3(-1.65f, 0f, 68f),
            "Models/ObstaclePack/Bomb/Bomb", new Vector3(1.35f, 1.6f, 1.35f),
            new Vector3(0f, 0.8f, 0f), 11f, 0.95f);
        CreateNormalObstacle(levelRoot.transform, "BombRight_68", new Vector3(1.65f, 0f, 68f),
            "Models/ObstaclePack/Bomb/Bomb", new Vector3(1.35f, 1.6f, 1.35f),
            new Vector3(0f, 0.8f, 0f), 11f, 0.95f);
        CreateAirPickup(levelRoot.transform, "BlueBalloon_74", new Vector3(0f, 1.2f, 74f), 25f);
        CreateSpikeStrip(levelRoot.transform, "Spikes_82", new Vector3(0f, 0f, 82f));
        CreateLowGate(levelRoot.transform, "LowGate_88", 88f);
        CreateAirPickupRow(levelRoot.transform, 91f, new[] { -1.5f, 1.5f }, 18f);

        // Son bölüm: daha yoğun engel dizilimi.
        CreateHeavyBalloonWall(levelRoot.transform, "HeavyWall_99", new Vector3(1.45f, 0f, 99f));
        CreateNormalObstacle(levelRoot.transform, "Spiral_107", new Vector3(-1.45f, 0f, 107f),
            "Models/ObstaclePack/Spiral/Spiral", new Vector3(1.7f, 0.9f, 2.2f),
            new Vector3(0f, 0.45f, 0f), 12f, 0.65f);
        CreateNormalObstacle(levelRoot.transform, "Cylinder_115", new Vector3(1.45f, 0f, 115f),
            "Models/ObstaclePack/Cylinder/Cylinder", new Vector3(1.45f, 2.5f, 1.45f),
            new Vector3(0f, 1.25f, 0f), 13f, 0.72f);
        CreateAirPickupRow(levelRoot.transform, 122f, new[] { -1.55f, 0f, 1.55f }, 14f);
        CreateSpikeStrip(levelRoot.transform, "Spikes_130", new Vector3(0f, 0f, 130f));
        CreateHeavyBalloonWall(levelRoot.transform, "HeavyWall_140", new Vector3(-1.4f, 0f, 140f));

        CreateFinishGate(levelRoot.transform, new Vector3(0f, 0f, 150f));
        CreateSideDecorations(levelRoot.transform);
    }

    private static void PrepareRoad()
    {
        GameObject road = GameObject.Find("Road");
        if (road == null)
        {
            road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "Road";
        }

        // Oynanış 150 metrede biter; kalan kısım fırlatma mini oyununun pisti.
        road.transform.position = new Vector3(0f, -0.5f, 140f);
        road.transform.localScale = new Vector3(6f, 1f, 300f);

        Renderer renderer = road.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = roadMaterial;
        }
    }

    private static void CreateNormalObstacle(
        Transform parent,
        string name,
        Vector3 position,
        string modelPath,
        Vector3 colliderSize,
        Vector3 colliderCenter,
        float airDamage,
        float visualScale)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.size = colliderSize;
        collider.center = colliderCenter;

        Obstacle obstacle = root.AddComponent<Obstacle>();
        obstacle.ConfigureNormal(airDamage, 0.38f, 0.85f);

        GameObject visual = InstantiateResourceModel(modelPath, root.transform, name + "_Visual");
        if (visual != null)
        {
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * visualScale;
            return;
        }

        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fallback.name = name + "_Fallback";
        fallback.transform.SetParent(root.transform, false);
        fallback.transform.localPosition = colliderCenter;
        fallback.transform.localScale = colliderSize;
        fallback.GetComponent<Renderer>().material = orangeMaterial;
        Destroy(fallback.GetComponent<Collider>());
    }

    private static void CreateLowGate(
        Transform parent,
        string name,
        float z)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(0f, 0f, z);

        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 1.7f, 0f);
        trigger.size = new Vector3(5.5f, 0.65f, 0.85f);

        Obstacle obstacle = root.AddComponent<Obstacle>();
        obstacle.ConfigureNormal(9f, 0.42f, 0.75f);

        CreateGateVisualPart(root.transform,
            new Vector3(-2.65f, 1.25f, 0f),
            new Vector3(0.28f, 2.5f, 0.35f));
        CreateGateVisualPart(root.transform,
            new Vector3(2.65f, 1.25f, 0f),
            new Vector3(0.28f, 2.5f, 0.35f));
        CreateGateVisualPart(root.transform,
            new Vector3(0f, 1.7f, 0f),
            new Vector3(5.5f, 0.65f, 0.65f));
    }

    private static void CreateGateVisualPart(
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = "LowGatePart";
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().material = orangeMaterial;
        Destroy(part.GetComponent<Collider>());
    }

    private static void CreateHeavyBalloonWall(
        Transform parent,
        string name,
        Vector3 position)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.size = new Vector3(2.4f, 2.5f, 0.9f);
        collider.center = new Vector3(0f, 1.25f, 0f);

        Obstacle obstacle = root.AddComponent<Obstacle>();
        obstacle.ConfigureHeavy(85f, 25f, 16f);

        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                GameObject brick = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                brick.name = "BalloonBrick";
                brick.transform.SetParent(root.transform, false);
                brick.transform.localPosition = new Vector3(
                    (column - 1) * 0.72f + (row % 2 == 0 ? 0f : 0.18f),
                    0.35f + row * 0.62f,
                    0f);
                brick.transform.localScale = new Vector3(0.72f, 0.55f, 0.52f);
                brick.GetComponent<Renderer>().material = row % 2 == 0
                    ? redMaterial
                    : orangeMaterial;
                Destroy(brick.GetComponent<Collider>());
            }
        }
    }

    private static void CreateSpikeStrip(
        Transform parent,
        string name,
        Vector3 position)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.45f, 0f);
        trigger.size = new Vector3(5.4f, 0.95f, 3.4f);
        root.AddComponent<GroundSpikes>();

        for (int i = -1; i <= 1; i++)
        {
            GameObject visual = InstantiateResourceModel(
                "Models/ObstaclePack/SpikeBase/SpikeBase",
                root.transform,
                "SpikeModel");

            if (visual != null)
            {
                visual.transform.localPosition = new Vector3(i * 1.75f, 0.03f, 0f);
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = new Vector3(1.15f, 2.2f, 0.9f);
                continue;
            }

            GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spike.name = "SpikeFallback";
            spike.transform.SetParent(root.transform, false);
            spike.transform.localPosition = new Vector3(i * 1.7f, 0.3f, 0f);
            spike.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            spike.transform.localScale = new Vector3(0.45f, 1f, 2.8f);
            spike.GetComponent<Renderer>().material = redMaterial;
            Destroy(spike.GetComponent<Collider>());
        }
    }

    private static void CreateAirPickupRow(
        Transform parent,
        float z,
        float[] xPositions,
        float airPerPickup)
    {
        for (int i = 0; i < xPositions.Length; i++)
        {
            CreateAirPickup(
                parent,
                $"BlueBalloon_{z:0}_{i}",
                new Vector3(xPositions[i], 1.25f, z),
                airPerPickup);
        }
    }

    private static void CreateAirPickup(
        Transform parent,
        string name,
        Vector3 position,
        float airAmount)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.72f;

        AirPickup pickup = root.AddComponent<AirPickup>();
        pickup.Configure(airAmount);

        GameObject balloon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        balloon.name = "BlueBalloonVisual";
        balloon.transform.SetParent(root.transform, false);
        balloon.transform.localScale = new Vector3(0.72f, 0.9f, 0.72f);
        balloon.GetComponent<Renderer>().material = cyanMaterial;
        Destroy(balloon.GetComponent<Collider>());

        GameObject knot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        knot.name = "BalloonKnot";
        knot.transform.SetParent(root.transform, false);
        knot.transform.localPosition = new Vector3(0f, -0.58f, 0f);
        knot.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
        knot.GetComponent<Renderer>().material = cyanMaterial;
        Destroy(knot.GetComponent<Collider>());
    }

    private static void CreateFinishGate(Transform parent, Vector3 position)
    {
        GameObject root = new GameObject("FinishLine");
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 2f, 0f);
        trigger.size = new Vector3(6f, 4f, 0.7f);
        root.AddComponent<FinishLine>();

        CreateGatePart(root.transform, new Vector3(-2.75f, 2f, 0f), new Vector3(0.3f, 4f, 0.3f));
        CreateGatePart(root.transform, new Vector3(2.75f, 2f, 0f), new Vector3(0.3f, 4f, 0.3f));
        CreateGatePart(root.transform, new Vector3(0f, 3.85f, 0f), new Vector3(5.8f, 0.35f, 0.35f));
    }

    private static void CreateGatePart(Transform parent, Vector3 localPosition, Vector3 scale)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = "FinishGatePart";
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().material = whiteMaterial;
        Destroy(part.GetComponent<Collider>());
    }

    private static void CreateSideDecorations(Transform parent)
    {
        for (int z = 20; z <= 145; z += 18)
        {
            CreateDecoration(parent, new Vector3(-4.2f, 0f, z));
            CreateDecoration(parent, new Vector3(4.2f, 0f, z + 7f));
        }
    }

    private static void CreateDecoration(Transform parent, Vector3 position)
    {
        GameObject visual = InstantiateResourceModel(
            "Models/ObstaclePack/SpikeTrap/SpikeTrap",
            parent,
            "SideDecoration");

        if (visual == null)
        {
            return;
        }

        visual.transform.position = position;
        visual.transform.rotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * 0.45f;
    }

    private static void SetupInterface(
        GameObject player,
        Rigidbody body,
        AirController airController,
        ScoreController scoreController,
        PlayerFormController formController,
        BalloonSizeController sizeController,
        GameManager gameManager,
        PlayerRunner runner,
        PlayerHorizontalController horizontalController,
        RunnerCameraFollow cameraFollow)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvas = canvasObject.GetComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        DisableLegacyInterface(canvas.transform);

        TMP_Text speedText = FindText(canvas.transform, "SpeedText") ??
            CreateText(canvas.transform, "SpeedText", "0 km/h", 48f);
        SetRect(speedText.rectTransform,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-50f, -45f), new Vector2(300f, 80f));
        speedText.alignment = TextAlignmentOptions.TopRight;
        GetOrAdd<SpeedDisplay>(canvas.gameObject).Configure(body, speedText);

        Slider airSlider = FindSlider(canvas.transform, "AirSlider") ??
            CreateAirSlider(canvas.transform);
        SetRect(airSlider.GetComponent<RectTransform>(),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(50f, -45f), new Vector2(430f, 65f));

        TMP_Text airText = FindText(airSlider.transform, "AirText") ??
            CreateText(airSlider.transform, "AirText", "HAVA 100/100", 32f);
        Stretch(airText.rectTransform, 0f, 0f, 0f, 0f);
        airText.alignment = TextAlignmentOptions.Center;
        GetOrAdd<AirDisplay>(canvas.gameObject).Configure(airController, airSlider, airText);

        TMP_Text scoreText = FindText(canvas.transform, "ScoreText") ??
            CreateText(canvas.transform, "ScoreText", "SKOR 0", 44f);
        SetRect(scoreText.rectTransform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -135f), new Vector2(420f, 70f));
        scoreText.alignment = TextAlignmentOptions.Center;
        GetOrAdd<ScoreDisplay>(canvas.gameObject).Configure(scoreController, scoreText);

        TMP_Text hintText = FindText(canvas.transform, "ControlHint") ??
            CreateText(canvas.transform, "ControlHint", "", 29f);
        hintText.text = "SÜRÜKLE: SAĞ/SOL  •  ÇİFT DOKUN: HELİKOPTER";
        SetRect(hintText.rectTransform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 260f), new Vector2(960f, 75f));
        hintText.alignment = TextAlignmentOptions.Center;

        GameObject shrinkButton = CreateButton(
            canvas.transform,
            "ShrinkButton",
            "KÜÇÜL\nBASILI TUT");
        SetRect(shrinkButton.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 70f), new Vector2(360f, 150f));
        GetOrAdd<HoldShrinkButton>(shrinkButton).Configure(sizeController);

        TMP_Text launchText = CreateText(
            canvas.transform,
            "LaunchResultText",
            "",
            74f);
        SetRect(launchText.rectTransform,
            new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(900f, 260f));
        launchText.alignment = TextAlignmentOptions.Center;
        launchText.raycastTarget = false;
        launchText.gameObject.SetActive(false);

        GameObject endPanel = CreateEndPanel(
            canvas.transform,
            gameManager,
            out TMP_Text titleText,
            out TMP_Text summaryText);
        gameManager.ConfigureEndPanel(endPanel, titleText, summaryText);
        endPanel.SetActive(false);

        LaunchMinigameController launcher =
            GetOrAdd<LaunchMinigameController>(gameManager.gameObject);
        launcher.Configure(
            player,
            body,
            runner,
            horizontalController,
            formController,
            sizeController,
            airController,
            scoreController,
            cameraFollow,
            gameManager,
            launchText);
    }

    private static void DisableLegacyInterface(Transform canvas)
    {
        string[] oldNames =
        {
            "HelicopterButton",
            "ShrinkButton",
            "EndPanel",
            "RevisionEndPanel",
            "GameOverPanel",
            "LaunchResultText"
        };

        foreach (string name in oldNames)
        {
            Transform child = FindChildByName(canvas, name);
            if (child == null)
            {
                continue;
            }

            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private static Slider CreateAirSlider(Transform parent)
    {
        GameObject sliderObject = CreateUiObject("AirSlider", parent);
        Image background = sliderObject.AddComponent<Image>();
        background.color = new Color(0.05f, 0.08f, 0.12f, 0.9f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.interactable = false;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;

        GameObject fillObject = CreateUiObject("Fill", sliderObject.transform);
        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = new Color(0.15f, 0.85f, 1f, 1f);
        Stretch(fillObject.GetComponent<RectTransform>(), 8f, 8f, 8f, 8f);

        slider.fillRect = fillObject.GetComponent<RectTransform>();
        slider.targetGraphic = background;
        return slider;
    }

    private static GameObject CreateEndPanel(
        Transform parent,
        GameManager gameManager,
        out TMP_Text titleText,
        out TMP_Text summaryText)
    {
        GameObject panel = CreateUiObject("RevisionEndPanel", parent);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.86f);
        panelImage.raycastTarget = true;
        Stretch(panel.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        titleText = CreateText(panel.transform, "EndTitle", "ÖLDÜN", 76f);
        SetRect(titleText.rectTransform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 210f), new Vector2(900f, 150f));
        titleText.alignment = TextAlignmentOptions.Center;

        summaryText = CreateText(panel.transform, "EndSummary", "", 44f);
        SetRect(summaryText.rectTransform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 30f), new Vector2(850f, 220f));
        summaryText.alignment = TextAlignmentOptions.Center;

        GameObject restartButton = CreateButton(
            panel.transform,
            "RestartButton",
            "TEKRAR OYNA");
        SetRect(restartButton.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -190f), new Vector2(460f, 130f));

        Button button = restartButton.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(gameManager.RestartGame);

        panel.SetActive(false);
        return panel;
    }

    private static GameObject CreateButton(Transform parent, string name, string label)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.1f, 0.65f, 0.95f, 0.97f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text labelText = CreateText(buttonObject.transform, "Label", label, 36f);
        Stretch(labelText.rectTransform, 12f, 12f, 12f, 12f);
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.raycastTarget = false;
        return buttonObject;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
        float fontSize)
    {
        GameObject textObject = CreateUiObject(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.font = TMP_Settings.defaultFontAsset;
        return text;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = 5;
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventObject = new GameObject("EventSystem");
            eventSystem = eventObject.AddComponent<EventSystem>();
            eventObject.AddComponent<InputSystemUIInputModule>();
            return;
        }

        BaseInputModule[] inputModules =
            eventSystem.GetComponents<BaseInputModule>();

        bool hasInputSystemModule = false;
        foreach (BaseInputModule inputModule in inputModules)
        {
            if (inputModule is InputSystemUIInputModule)
            {
                hasInputSystemModule = true;
                break;
            }
        }

        if (!hasInputSystemModule)
        {
            foreach (BaseInputModule inputModule in inputModules)
            {
                inputModule.enabled = false;
            }

            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private static Transform CreateRotor(Transform player)
    {
        Transform existing = FindChildByName(player, "HelicopterRotor");
        if (existing != null)
        {
            existing.gameObject.SetActive(false);
            return existing;
        }

        GameObject rotorRoot = new GameObject("HelicopterRotor");
        rotorRoot.transform.SetParent(player, false);
        rotorRoot.transform.localPosition = new Vector3(0f, 1.2f, 0f);

        CreateRotorBlade(rotorRoot.transform, new Vector3(2.1f, 0.08f, 0.18f));
        CreateRotorBlade(rotorRoot.transform, new Vector3(0.18f, 0.08f, 2.1f));
        rotorRoot.SetActive(false);
        return rotorRoot.transform;
    }

    private static void CreateRotorBlade(Transform parent, Vector3 scale)
    {
        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "RotorBlade";
        blade.transform.SetParent(parent, false);
        blade.transform.localScale = scale;
        blade.GetComponent<Renderer>().material = darkMaterial;
        Destroy(blade.GetComponent<Collider>());
    }

    private static GameObject InstantiateResourceModel(
        string resourcePath,
        Transform parent,
        string instanceName)
    {
        GameObject modelAsset = Resources.Load<GameObject>(resourcePath);
        if (modelAsset == null)
        {
            Debug.LogWarning($"Model bulunamadı: Resources/{resourcePath}");
            return null;
        }

        GameObject instance = Instantiate(modelAsset, parent);
        instance.name = instanceName;

        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        UpgradeImportedMaterials(instance);
        return instance;
    }

    private static void UpgradeImportedMaterials(GameObject model)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            return;
        }

        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            Material[] sourceMaterials = renderer.materials;
            Material[] convertedMaterials = new Material[sourceMaterials.Length];

            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Material source = sourceMaterials[i];
                Color color = Color.white;

                if (source != null)
                {
                    if (source.HasProperty("_BaseColor"))
                    {
                        color = source.GetColor("_BaseColor");
                    }
                    else if (source.HasProperty("_Color"))
                    {
                        color = source.GetColor("_Color");
                    }
                }

                Material converted = new Material(shader);
                converted.color = color;
                convertedMaterials[i] = converted;
            }

            renderer.materials = convertedMaterials;
        }
    }

    private static void EnsureMaterials()
    {
        if (cyanMaterial != null)
        {
            return;
        }

        cyanMaterial = CreateMaterial(new Color(0.1f, 0.85f, 1f));
        redMaterial = CreateMaterial(new Color(0.95f, 0.12f, 0.16f));
        orangeMaterial = CreateMaterial(new Color(1f, 0.45f, 0.08f));
        darkMaterial = CreateMaterial(new Color(0.08f, 0.1f, 0.14f));
        whiteMaterial = CreateMaterial(new Color(0.95f, 0.95f, 0.95f));
        roadMaterial = CreateMaterial(new Color(0.16f, 0.2f, 0.25f));
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private static Transform FindChildByName(Transform parent, string name)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    private static TMP_Text FindText(Transform parent, string name)
    {
        Transform child = FindChildByName(parent, name);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Slider FindSlider(Transform parent, string name)
    {
        Transform child = FindChildByName(parent, name);
        return child != null ? child.GetComponent<Slider>() : null;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(
        RectTransform rect,
        float left,
        float right,
        float top,
        float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.localScale = Vector3.one;
    }
}
