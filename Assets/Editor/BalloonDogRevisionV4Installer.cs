#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BalloonDogRevisionV4Installer
{
    private const string MenuPath = "Tools/BalloonDog/Revize V4 - 20 Yenilik";
    private const string PolishRootName = "BalloonDog_V4_Polish";
    private const float FinishZ = 185f;
    private const float RoadHalfWidth = 4.5f;

    [MenuItem(MenuPath)]
    public static void ApplyFromMenu()
    {
        ApplyToActiveScene(true);
    }

    public static void ApplyToActiveScene(bool saveScene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "BalloonDog Revize V4",
                "Önce Play modunu kapat.",
                "Tamam");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog(
                "BalloonDog Revize V4",
                "Önce Assets/Scenes/Game.unity sahnesini aç.",
                "Tamam");
            return;
        }

        GameObject player = FindSceneObject("player") ?? FindSceneObject("Player");
        GameObject levelRoot = FindSceneObject(BalloonDogPrototypeBootstrap.BakedLevelRootName);

        if (player == null || levelRoot == null)
        {
            EditorUtility.DisplayDialog(
                "BalloonDog Revize V4",
                "Sahnede player ve BalloonDog_Level bulunamadı. Önce Tools > BalloonDog > Sahneyi Düzenlenebilir Hale Getir aracını çalıştır.",
                "Tamam");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("BalloonDog V4", "V3 temeli kontrol ediliyor...", 0.05f);
            BalloonDogRevisionV3Installer.ApplyRevisionToActiveScene(false);

            GameObject previousPolish = FindSceneObject(PolishRootName);
            if (previousPolish != null)
            {
                Object.DestroyImmediate(previousPolish);
            }

            GameObject polishRoot = new GameObject(PolishRootName);
            polishRoot.transform.SetParent(levelRoot.transform, false);

            EditorUtility.DisplayProgressBar("BalloonDog V4", "Yol ve engeller düzeltiliyor...", 0.2f);
            UpgradeRoad(player, levelRoot, polishRoot.transform);
            FixObstaclePlacementAndRotation(levelRoot, polishRoot.transform);

            EditorUtility.DisplayProgressBar("BalloonDog V4", "Çevre ve arka plan hazırlanıyor...", 0.48f);
            BuildEnvironmentPolish(polishRoot.transform);

            EditorUtility.DisplayProgressBar("BalloonDog V4", "Arayüz ve geri bildirimler ekleniyor...", 0.72f);
            BuildInterfacePolish(player);
            ConfigureGameplayFeedback(player);

            EditorUtility.DisplayProgressBar("BalloonDog V4", "Bitiş pistine çarpan bölgeleri ekleniyor...", 0.9f);
            BuildLaunchMultiplierZones(polishRoot.transform);

            MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);

            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
                EditorUtility.DisplayDialog(
                    "BalloonDog Revize V4 hazır",
                    "Dönen engeller artık görünür merkezlerinin etrafında dönüyor. Harita sınırları, görseller, efektler ve 20 geliştirme sahneye kaydedildi.",
                    "Tamam");
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog(
                "BalloonDog Revize V4 uygulanamadı",
                "Console'daki ilk kırmızı hatayı kontrol et.\n\n" + exception.Message,
                "Tamam");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static void UpgradeRoad(
        GameObject player,
        GameObject levelRoot,
        Transform polishRoot)
    {
        GameObject road = FindSceneObject("Road");
        if (road != null)
        {
            road.transform.position = new Vector3(0f, -0.5f, 190f);
            road.transform.localScale = new Vector3(RoadHalfWidth * 2f, 1f, 430f);
            Renderer roadRenderer = road.GetComponent<Renderer>();
            if (roadRenderer != null)
            {
                roadRenderer.sharedMaterial = GetOrCreateMaterial(
                    "BDV4_Road",
                    new Color(0.115f, 0.16f, 0.23f),
                    0.35f);
            }
        }

        PlayerHorizontalController horizontal =
            player.GetComponent<PlayerHorizontalController>();
        if (horizontal != null)
        {
            horizontal.ConfigureRoadLimit(3.7f);
            EditorUtility.SetDirty(horizontal);
        }

        Transform roadDetails = CreateGroup(polishRoot, "RoadDetails");
        Material laneMaterial = GetOrCreateMaterial(
            "BDV4_Lane",
            new Color(0.9f, 0.92f, 0.95f),
            0.15f);
        Material curbMaterial = GetOrCreateMaterial(
            "BDV4_Curb",
            new Color(0.76f, 0.78f, 0.82f),
            0.25f);

        for (float z = 2f; z <= FinishZ; z += 7f)
        {
            CreateCube(
                roadDetails,
                $"LaneLeft_{z:000}",
                new Vector3(-1.5f, 0.025f, z),
                new Vector3(0.085f, 0.025f, 3.4f),
                laneMaterial,
                false);
            CreateCube(
                roadDetails,
                $"LaneRight_{z:000}",
                new Vector3(1.5f, 0.025f, z),
                new Vector3(0.085f, 0.025f, 3.4f),
                laneMaterial,
                false);
        }

        CreateCube(
            roadDetails,
            "LeftCurb",
            new Vector3(-4.38f, -0.08f, 190f),
            new Vector3(0.22f, 0.22f, 430f),
            curbMaterial,
            false);
        CreateCube(
            roadDetails,
            "RightCurb",
            new Vector3(4.38f, -0.08f, 190f),
            new Vector3(0.22f, 0.22f, 430f),
            curbMaterial,
            false);
    }

    private static void FixObstaclePlacementAndRotation(
        GameObject levelRoot,
        Transform polishRoot)
    {
        Transform obstacles = FindChildRecursive(levelRoot.transform, "Obstacles");
        if (obstacles == null)
        {
            return;
        }

        Transform warnings = CreateGroup(polishRoot, "ObstacleWarnings");
        Material normalWarning = GetOrCreateMaterial(
            "BDV4_WarningOrange",
            new Color(1f, 0.35f, 0.08f),
            0.2f,
            true);
        Material lethalWarning = GetOrCreateMaterial(
            "BDV4_WarningRed",
            new Color(1f, 0.08f, 0.08f),
            0.2f,
            true);

        List<Transform> directObstacles = new List<Transform>();
        for (int i = 0; i < obstacles.childCount; i++)
        {
            directObstacles.Add(obstacles.GetChild(i));
        }

        foreach (Transform obstacle in directObstacles)
        {
            MoveRendererBoundsInsideRoad(obstacle, RoadHalfWidth - 0.3f);
            ConfigureCenteredRotation(obstacle);

            if (!TryCalculateBounds(obstacle, out Bounds bounds))
            {
                continue;
            }

            bool lethal = obstacle.name.ToLowerInvariant().Contains("spike");
            float warningWidth = Mathf.Clamp(bounds.size.x, 0.9f, 2.7f);
            CreateCube(
                warnings,
                $"Warning_{obstacle.name}",
                new Vector3(bounds.center.x, 0.035f, bounds.min.z - 2.2f),
                new Vector3(warningWidth, 0.025f, 1.5f),
                lethal ? lethalWarning : normalWarning,
                false);
        }
    }

    private static void ConfigureCenteredRotation(Transform obstacle)
    {
        string lowerName = obstacle.name.ToLowerInvariant();
        Vector3 axis;
        float speed;

        if (lowerName.Contains("gear"))
        {
            axis = Vector3.forward;
            speed = 72f;
        }
        else if (lowerName.Contains("spiral"))
        {
            axis = Vector3.up;
            speed = 64f;
        }
        else if (lowerName.Contains("cylinder"))
        {
            axis = Vector3.right;
            speed = 58f;
        }
        else
        {
            return;
        }

        Transform visualTarget = GetOrCreateVisualPivot(obstacle);

        foreach (RotatingObstacle oldRotation in
                 obstacle.GetComponentsInChildren<RotatingObstacle>(true))
        {
            if (oldRotation != null && oldRotation.transform != visualTarget)
            {
                Object.DestroyImmediate(oldRotation);
            }
        }

        RotatingObstacle rotating = visualTarget.GetComponent<RotatingObstacle>();
        if (rotating == null)
        {
            rotating = visualTarget.gameObject.AddComponent<RotatingObstacle>();
        }

        rotating.Configure(axis, speed, false);
        rotating.RecalculatePivot();
        EditorUtility.SetDirty(rotating);
    }

    private static Transform GetOrCreateVisualPivot(Transform obstacle)
    {
        Renderer ownRenderer = obstacle.GetComponent<Renderer>();
        if (ownRenderer != null)
        {
            return obstacle;
        }

        Transform existing = obstacle.Find("RotationVisualPivot");
        if (existing != null)
        {
            return existing;
        }

        GameObject pivotObject = new GameObject("RotationVisualPivot");
        Transform pivot = pivotObject.transform;
        pivot.SetParent(obstacle, false);

        List<Transform> visualChildren = new List<Transform>();
        for (int i = 0; i < obstacle.childCount; i++)
        {
            Transform child = obstacle.GetChild(i);
            if (child == pivot)
            {
                continue;
            }

            if (child.GetComponentInChildren<Renderer>(true) != null)
            {
                visualChildren.Add(child);
            }
        }

        foreach (Transform child in visualChildren)
        {
            child.SetParent(pivot, true);
        }

        return pivot;
    }

    private static void MoveRendererBoundsInsideRoad(
        Transform obstacle,
        float allowedHalfWidth)
    {
        if (!TryCalculateBounds(obstacle, out Bounds bounds))
        {
            Vector3 fallback = obstacle.position;
            fallback.x = Mathf.Clamp(fallback.x, -allowedHalfWidth, allowedHalfWidth);
            obstacle.position = fallback;
            return;
        }

        if (bounds.size.x >= allowedHalfWidth * 2f)
        {
            obstacle.position += Vector3.right * -bounds.center.x;
            return;
        }

        float shift = 0f;
        if (bounds.min.x < -allowedHalfWidth)
        {
            shift += -allowedHalfWidth - bounds.min.x;
        }

        if (bounds.max.x > allowedHalfWidth)
        {
            shift += allowedHalfWidth - bounds.max.x;
        }

        obstacle.position += Vector3.right * shift;
    }

    private static void BuildEnvironmentPolish(Transform polishRoot)
    {
        Transform environment = CreateGroup(polishRoot, "EnvironmentPolish");

        Material grassMaterial = GetOrCreateMaterial(
            "BDV4_Grass",
            new Color(0.17f, 0.38f, 0.22f),
            0.65f);
        Material trunkMaterial = GetOrCreateMaterial(
            "BDV4_Trunk",
            new Color(0.28f, 0.15f, 0.08f),
            0.75f);
        Material leafMaterial = GetOrCreateMaterial(
            "BDV4_Leaves",
            new Color(0.12f, 0.48f, 0.25f),
            0.55f);
        Material rockMaterial = GetOrCreateMaterial(
            "BDV4_Rock",
            new Color(0.3f, 0.34f, 0.38f),
            0.8f);
        Material cloudMaterial = GetOrCreateMaterial(
            "BDV4_Cloud",
            new Color(0.92f, 0.96f, 1f),
            0.2f);

        CreateCube(
            environment,
            "LeftGrassField",
            new Vector3(-10.5f, -0.58f, 190f),
            new Vector3(12f, 0.35f, 430f),
            grassMaterial,
            false);
        CreateCube(
            environment,
            "RightGrassField",
            new Vector3(10.5f, -0.58f, 190f),
            new Vector3(12f, 0.35f, 430f),
            grassMaterial,
            false);

        for (int i = 0; i < 18; i++)
        {
            float z = 8f + i * 21f;
            float leftX = -7.2f - Mathf.Abs(Mathf.Sin(i * 1.7f)) * 2.4f;
            float rightX = 7.2f + Mathf.Abs(Mathf.Cos(i * 1.3f)) * 2.4f;
            CreateTree(environment, $"TreeL_{i:00}", new Vector3(leftX, 0f, z), trunkMaterial, leafMaterial, 0.85f + (i % 3) * 0.12f);
            CreateTree(environment, $"TreeR_{i:00}", new Vector3(rightX, 0f, z + 9f), trunkMaterial, leafMaterial, 0.8f + ((i + 1) % 3) * 0.14f);

            if (i % 2 == 0)
            {
                CreateRock(environment, $"RockL_{i:00}", new Vector3(leftX - 1.8f, -0.1f, z + 6f), rockMaterial, 0.65f + (i % 4) * 0.12f);
                CreateRock(environment, $"RockR_{i:00}", new Vector3(rightX + 1.6f, -0.1f, z - 5f), rockMaterial, 0.55f + ((i + 2) % 4) * 0.13f);
            }
        }

        for (int i = 0; i < 7; i++)
        {
            float x = i % 2 == 0 ? -8f - i : 8f + i;
            float y = 11f + (i % 3) * 1.2f;
            float z = 30f + i * 48f;
            CreateCloud(environment, $"Cloud_{i:00}", new Vector3(x, y, z), cloudMaterial, 1.2f + (i % 2) * 0.35f);
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.58f, 0.72f, 0.82f);
        RenderSettings.fogDensity = 0.0065f;
        RenderSettings.ambientLight = new Color(0.46f, 0.52f, 0.6f);

        Light directional = Object.FindAnyObjectByType<Light>();
        if (directional != null)
        {
            directional.color = new Color(1f, 0.9f, 0.76f);
            directional.intensity = 1.25f;
            directional.shadows = LightShadows.Soft;
            directional.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            EditorUtility.SetDirty(directional);
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = new Color(0.42f, 0.68f, 0.86f);
        }
    }

    private static void BuildInterfacePolish(GameObject player)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        Transform previous = FindChildRecursive(canvas.transform, "V4InterfacePolish");
        if (previous != null)
        {
            Object.DestroyImmediate(previous.gameObject);
        }

        GameObject interfaceRootObject = new GameObject(
            "V4InterfacePolish",
            typeof(RectTransform));
        RectTransform interfaceRoot = interfaceRootObject.GetComponent<RectTransform>();
        interfaceRoot.SetParent(canvas.transform, false);
        Stretch(interfaceRoot);
        interfaceRoot.SetAsFirstSibling();

        Slider progressSlider = CreateProgressSlider(interfaceRoot);
        TMP_Text progressText = CreateText(
            progressSlider.transform,
            "ProgressText",
            "BÖLÜM %0",
            24f,
            Color.white);
        Stretch(progressText.rectTransform);
        progressText.alignment = TextAlignmentOptions.Center;

        ProgressDisplay progressDisplay = canvas.GetComponent<ProgressDisplay>();
        if (progressDisplay == null)
        {
            progressDisplay = canvas.gameObject.AddComponent<ProgressDisplay>();
        }
        progressDisplay.Configure(player.transform, progressSlider, progressText, FinishZ);

        List<Graphic> speedLineGraphics = new List<Graphic>();
        for (int i = 0; i < 10; i++)
        {
            GameObject lineObject = new GameObject(
                $"SpeedLine_{i:00}",
                typeof(RectTransform),
                typeof(Image));
            RectTransform lineRect = lineObject.GetComponent<RectTransform>();
            lineRect.SetParent(interfaceRoot, false);
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            float side = i % 2 == 0 ? -1f : 1f;
            float vertical = -650f + (i / 2) * 310f;
            lineRect.anchoredPosition = new Vector2(side * (350f + (i % 3) * 90f), vertical);
            lineRect.sizeDelta = new Vector2(12f, 260f + (i % 4) * 60f);
            lineRect.localRotation = Quaternion.Euler(0f, 0f, side * -18f);

            Image image = lineObject.GetComponent<Image>();
            image.color = new Color(0.75f, 0.94f, 1f, 0f);
            image.raycastTarget = false;
            speedLineGraphics.Add(image);
        }

        SpeedLinesOverlay speedLines = interfaceRootObject.AddComponent<SpeedLinesOverlay>();
        speedLines.Configure(
            player.GetComponent<PlayerRunner>(),
            speedLineGraphics.ToArray());

        GameObject mainMenu = FindSceneObject("MainMenuPanel");
        if (mainMenu != null)
        {
            if (mainMenu.GetComponent<UiPanelAnimator>() == null)
            {
                mainMenu.AddComponent<UiPanelAnimator>();
            }

            TMP_Text bestText = FindChildComponent<TMP_Text>(mainMenu.transform, "BestScoreText");
            if (bestText == null)
            {
                bestText = CreateText(
                    mainMenu.transform,
                    "BestScoreText",
                    "EN İYİ: 0",
                    38f,
                    new Color(1f, 0.82f, 0.2f));
                SetRect(
                    bestText.rectTransform,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 210f),
                    new Vector2(600f, 70f));
            }

            HighScoreDisplay highScore = mainMenu.GetComponent<HighScoreDisplay>();
            if (highScore == null)
            {
                highScore = mainMenu.AddComponent<HighScoreDisplay>();
            }
            highScore.Configure(bestText);
        }

        GameObject endPanel = FindSceneObject("RevisionEndPanel") ??
                              FindSceneObject("EndPanel") ??
                              FindSceneObject("GameOverPanel");
        if (endPanel != null && endPanel.GetComponent<UiPanelAnimator>() == null)
        {
            endPanel.AddComponent<UiPanelAnimator>();
        }
    }

    private static void ConfigureGameplayFeedback(GameObject player)
    {
        Camera camera = Camera.main;
        PlayerRunner runner = player.GetComponent<PlayerRunner>();

        if (camera != null)
        {
            if (camera.GetComponent<CameraShakeController>() == null)
            {
                camera.gameObject.AddComponent<CameraShakeController>();
            }

            SpeedFovController fov = camera.GetComponent<SpeedFovController>();
            if (fov == null)
            {
                fov = camera.gameObject.AddComponent<SpeedFovController>();
            }
            fov.Configure(runner);
        }

        AirLeakFeedback leakFeedback = player.GetComponent<AirLeakFeedback>();
        if (leakFeedback == null)
        {
            leakFeedback = player.AddComponent<AirLeakFeedback>();
        }

        Transform visual = FindChildRecursive(player.transform, "BalloonDogModel") ??
                           FindChildRecursive(player.transform, "Bubble");
        leakFeedback.Configure(visual);
    }

    private static void BuildLaunchMultiplierZones(Transform polishRoot)
    {
        Transform zones = CreateGroup(polishRoot, "LaunchMultiplierZones");

        for (int i = 0; i < 8; i++)
        {
            int multiplier = i + 2;
            float z = FinishZ + 10f + i * 9f;
            Color zoneColor = Color.Lerp(
                new Color(0.2f, 0.75f, 1f),
                new Color(1f, 0.2f, 0.12f),
                i / 7f);
            Material material = GetOrCreateMaterial(
                $"BDV4_LaunchZone_{multiplier}",
                zoneColor,
                0.25f,
                true);

            CreateCube(
                zones,
                $"MultiplierZone_x{multiplier}",
                new Vector3(0f, 0.035f, z),
                new Vector3(RoadHalfWidth * 2f - 0.35f, 0.025f, 7.6f),
                material,
                false);

            GameObject labelObject = new GameObject(
                $"MultiplierLabel_x{multiplier}",
                typeof(RectTransform),
                typeof(TextMeshPro));
            labelObject.transform.SetParent(zones, false);
            labelObject.transform.position = new Vector3(0f, 0.08f, z);
            labelObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
            label.text = $"x{multiplier}";
            label.fontSize = 8f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.rectTransform.sizeDelta = new Vector2(6f, 2f);
        }
    }

    private static Slider CreateProgressSlider(Transform parent)
    {
        GameObject sliderObject = new GameObject(
            "ProgressSlider",
            typeof(RectTransform),
            typeof(Slider));
        RectTransform rect = sliderObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetRect(
            rect,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -205f),
            new Vector2(620f, 46f));

        GameObject background = new GameObject(
            "Background",
            typeof(RectTransform),
            typeof(Image));
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.SetParent(rect, false);
        Stretch(backgroundRect);
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0.04f, 0.07f, 0.11f, 0.68f);
        backgroundImage.raycastTarget = false;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.SetParent(rect, false);
        Stretch(fillAreaRect, 7f, 7f, 7f, 7f);

        GameObject fill = new GameObject(
            "Fill",
            typeof(RectTransform),
            typeof(Image));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.SetParent(fillAreaRect, false);
        Stretch(fillRect);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(0.15f, 0.78f, 1f, 0.9f);
        fillImage.raycastTarget = false;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.interactable = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static void CreateTree(
        Transform parent,
        string name,
        Vector3 position,
        Material trunkMaterial,
        Material leafMaterial,
        float scale)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = position;
        root.transform.localScale = Vector3.one * scale;

        CreateCylinder(
            root.transform,
            "Trunk",
            new Vector3(0f, 1.25f, 0f),
            new Vector3(0.38f, 1.25f, 0.38f),
            trunkMaterial);

        CreateSphere(root.transform, "LeavesA", new Vector3(0f, 3f, 0f), new Vector3(1.6f, 1.5f, 1.6f), leafMaterial);
        CreateSphere(root.transform, "LeavesB", new Vector3(-0.7f, 2.75f, 0.1f), new Vector3(1.1f, 1.05f, 1.1f), leafMaterial);
        CreateSphere(root.transform, "LeavesC", new Vector3(0.7f, 2.75f, -0.1f), new Vector3(1.1f, 1.05f, 1.1f), leafMaterial);
    }

    private static void CreateRock(
        Transform parent,
        string name,
        Vector3 position,
        Material material,
        float scale)
    {
        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = name;
        rock.transform.SetParent(parent, false);
        rock.transform.position = position;
        rock.transform.localScale = new Vector3(1.5f, 0.75f, 1.15f) * scale;
        rock.transform.rotation = Quaternion.Euler(0f, scale * 73f, scale * 8f);
        rock.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(rock.GetComponent<Collider>());
    }

    private static void CreateCloud(
        Transform parent,
        string name,
        Vector3 position,
        Material material,
        float scale)
    {
        GameObject cloud = new GameObject(name);
        cloud.transform.SetParent(parent, false);
        cloud.transform.position = position;
        cloud.transform.localScale = Vector3.one * scale;
        CreateSphere(cloud.transform, "PuffA", Vector3.zero, new Vector3(2.5f, 1.25f, 1.2f), material);
        CreateSphere(cloud.transform, "PuffB", new Vector3(1.5f, 0.2f, 0f), new Vector3(1.8f, 1.05f, 1f), material);
        CreateSphere(cloud.transform, "PuffC", new Vector3(-1.5f, 0.15f, 0f), new Vector3(1.7f, 0.95f, 1f), material);
    }

    private static GameObject CreateCube(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Material material,
        bool colliderEnabled)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = cube.GetComponent<Collider>();
        if (!colliderEnabled && collider != null)
        {
            Object.DestroyImmediate(collider);
        }
        return cube;
    }

    private static void CreateCylinder(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = localPosition;
        cylinder.transform.localScale = localScale;
        cylinder.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(cylinder.GetComponent<Collider>());
    }

    private static void CreateSphere(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = localPosition;
        sphere.transform.localScale = localScale;
        sphere.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(sphere.GetComponent<Collider>());
    }

    private static Material GetOrCreateMaterial(
        string name,
        Color color,
        float smoothness = 0.4f,
        bool emission = false)
    {
        const string folder = "Assets/Materials/BalloonDogV4";
        EnsureFolder("Assets/Materials", "BalloonDogV4");
        string path = $"{folder}/{name}.mat";

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.8f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string content,
        float fontSize,
        Color color)
    {
        GameObject textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void Stretch(
        RectTransform rect,
        float left = 0f,
        float right = 0f,
        float top = 0f,
        float bottom = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static bool TryCalculateBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        bounds = new Bounds(root.position, Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    private static T FindChildComponent<T>(Transform parent, string name)
        where T : Component
    {
        Transform child = FindChildRecursive(parent, name);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform == null || transform.name != objectName)
            {
                continue;
            }

            if (!transform.gameObject.scene.IsValid())
            {
                continue;
            }

            return transform.gameObject;
        }

        return null;
    }

    private static void MarkSceneDirty(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            EditorUtility.SetDirty(root);
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                EditorUtility.SetDirty(transform.gameObject);
                foreach (Component component in transform.GetComponents<Component>())
                {
                    if (component != null)
                    {
                        EditorUtility.SetDirty(component);
                    }
                }
            }
        }
    }
}
#endif
