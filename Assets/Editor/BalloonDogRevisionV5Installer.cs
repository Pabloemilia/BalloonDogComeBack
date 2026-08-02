#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BalloonDogRevisionV5Installer
{
    private const string MenuPath = "Tools/BalloonDog/Revize V5 - Mega Polish";
    private const string RootName = "BalloonDog_V5_MegaPolish";
    private const string MaterialFolder = "Assets/Materials/BalloonDogV5";
    private const float FinishZ = 185f;
    private const float RoadHalfWidth = 4.1f;

    [MenuItem(MenuPath)]
    public static void ApplyFromMenu()
    {
        ApplyToActiveScene(true);
    }

    public static void ApplyToActiveScene(bool saveScene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("BalloonDog V5", "Önce Play modunu kapat.", "Tamam");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("BalloonDog V5", "Önce Game.unity sahnesini aç.", "Tamam");
            return;
        }

        GameObject player = FindSceneObject("player") ?? FindSceneObject("Player");
        GameObject levelRoot = FindSceneObject(BalloonDogPrototypeBootstrap.BakedLevelRootName);
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();

        if (player == null || levelRoot == null || canvas == null)
        {
            EditorUtility.DisplayDialog(
                "BalloonDog V5",
                "player, BalloonDog_Level veya Canvas bulunamadı. Önce sahneyi düzenlenebilir hale getirip V4 aracını çalıştır.",
                "Tamam");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("BalloonDog V5", "V4 temeli uygulanıyor...", 0.04f);
            BalloonDogRevisionV4Installer.ApplyToActiveScene(false);

            GameObject oldRoot = FindSceneObject(RootName);
            if (oldRoot != null)
            {
                Object.DestroyImmediate(oldRoot);
            }

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(levelRoot.transform, false);

            EditorUtility.DisplayProgressBar("BalloonDog V5", "Dönen engeller kesin olarak düzeltiliyor...", 0.14f);
            RepairAllObstaclePivots(levelRoot.transform);

            EditorUtility.DisplayProgressBar("BalloonDog V5", "Oyuncu hissi ve zorluk eğrisi ekleniyor...", 0.28f);
            ConfigurePlayer(player, root.transform);
            ConfigurePickups();

            EditorUtility.DisplayProgressBar("BalloonDog V5", "Harita ve çevre zenginleştiriliyor...", 0.43f);
            BuildWorld(root.transform);
            BuildObstacleVariety(levelRoot.transform, root.transform);

            EditorUtility.DisplayProgressBar("BalloonDog V5", "Arayüz, combo ve pause menüsü hazırlanıyor...", 0.66f);
            BuildUi(canvas, player);
            PolishMainMenu(canvas);

            EditorUtility.DisplayProgressBar("BalloonDog V5", "Bitiş ve fırlatma alanı yenileniyor...", 0.84f);
            PolishFinish(root.transform);
            EnsureAudioSystem();

            EditorUtility.DisplayProgressBar("BalloonDog V5", "Mobil performans ayarları uygulanıyor...", 0.94f);
            ApplyPerformanceSettings();

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();

            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
                EditorUtility.DisplayDialog(
                    "BalloonDog V5 hazır",
                    "Engeller artık sabit merkezlerinde dönüyor. Combo, yakın geçiş, hızlanma, geri bildirimler, pause, countdown, yeni çevre ve bitiş geliştirmeleri sahneye kaydedildi.",
                    "Tamam");
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog(
                "BalloonDog V5 uygulanamadı",
                "Console'daki ilk kırmızı hatayı kontrol et.\n\n" + exception.Message,
                "Tamam");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static void RepairAllObstaclePivots(Transform levelRoot)
    {
        Transform obstacles = FindChildRecursive(levelRoot, "Obstacles");
        if (obstacles == null)
        {
            return;
        }

        List<Transform> obstacleList = new List<Transform>();
        for (int i = 0; i < obstacles.childCount; i++)
        {
            obstacleList.Add(obstacles.GetChild(i));
        }

        int movingIndex = 0;
        foreach (Transform obstacle in obstacleList)
        {
            ClampInsideRoad(obstacle, RoadHalfWidth - 0.25f);

            string lower = obstacle.name.ToLowerInvariant();
            Vector3 axis;
            float speed;
            if (lower.Contains("gear"))
            {
                axis = Vector3.forward;
                speed = 62f;
            }
            else if (lower.Contains("spiral"))
            {
                axis = Vector3.up;
                speed = 52f;
            }
            else if (lower.Contains("cylinder"))
            {
                axis = Vector3.right;
                speed = 48f;
            }
            else
            {
                continue;
            }

            Transform pivot = RebuildStableSpinPivot(obstacle);
            RotatingObstacle rotation = pivot.gameObject.AddComponent<RotatingObstacle>();
            rotation.Configure(axis, speed, false);
            EditorUtility.SetDirty(rotation);

            // Her dönen engel hareket etmez. Seçilmiş birkaç engel kontrollü biçimde
            // şeritler arasında kayarak bölüme çeşitlilik katar.
            if (movingIndex % 3 == 1)
            {
                MovingObstacle moving = obstacle.GetComponent<MovingObstacle>();
                if (moving == null)
                {
                    moving = obstacle.gameObject.AddComponent<MovingObstacle>();
                }
                float safeDistance = Mathf.Min(1.25f, Mathf.Max(0.45f, RoadHalfWidth - Mathf.Abs(obstacle.position.x) - 0.7f));
                moving.Configure(
                    MovingObstacle.MotionMode.Horizontal,
                    safeDistance,
                    0.16f + movingIndex * 0.012f,
                    movingIndex * 0.31f,
                    RoadHalfWidth - 0.45f);
                EditorUtility.SetDirty(moving);
            }
            else
            {
                MovingObstacle oldMoving = obstacle.GetComponent<MovingObstacle>();
                if (oldMoving != null)
                {
                    Object.DestroyImmediate(oldMoving);
                }
            }

            movingIndex++;
        }
    }

    private static Transform RebuildStableSpinPivot(Transform obstacle)
    {
        foreach (RotatingObstacle oldRotation in obstacle.GetComponentsInChildren<RotatingObstacle>(true))
        {
            Object.DestroyImmediate(oldRotation);
        }

        Transform oldStable = obstacle.Find("StableSpinPivot");
        if (oldStable != null)
        {
            // Eski pivotun çocuklarını dünya konumlarını koruyarak geri çıkar.
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < oldStable.childCount; i++)
            {
                children.Add(oldStable.GetChild(i));
            }
            foreach (Transform child in children)
            {
                child.SetParent(obstacle, true);
            }
            Object.DestroyImmediate(oldStable.gameObject);
        }

        if (!TryCalculateBounds(obstacle, out Bounds bounds))
        {
            GameObject fallback = new GameObject("StableSpinPivot");
            fallback.transform.SetParent(obstacle, false);
            return fallback.transform;
        }

        GameObject pivotObject = new GameObject("StableSpinPivot");
        Transform pivot = pivotObject.transform;
        pivot.SetParent(obstacle, false);
        pivot.position = bounds.center;
        pivot.rotation = obstacle.rotation;

        List<Transform> visualRoots = new List<Transform>();
        for (int i = 0; i < obstacle.childCount; i++)
        {
            Transform child = obstacle.GetChild(i);
            if (child == pivot)
            {
                continue;
            }

            if (child.GetComponentInChildren<Renderer>(true) != null)
            {
                visualRoots.Add(child);
            }
        }

        foreach (Transform child in visualRoots)
        {
            child.SetParent(pivot, true);
        }

        // Model mesh'i doğrudan engel root'undaysa, görsel kopyasını pivotun altına
        // alıp root renderer'ını kapatırız. Collider ve scriptler sabit root'ta kalır.
        MeshFilter rootFilter = obstacle.GetComponent<MeshFilter>();
        MeshRenderer rootRenderer = obstacle.GetComponent<MeshRenderer>();
        if (rootFilter != null && rootRenderer != null && rootRenderer.enabled)
        {
            GameObject meshVisual = new GameObject("RootMeshVisual", typeof(MeshFilter), typeof(MeshRenderer));
            meshVisual.transform.position = obstacle.position;
            meshVisual.transform.rotation = obstacle.rotation;
            meshVisual.transform.localScale = obstacle.lossyScale;
            meshVisual.transform.SetParent(pivot, true);

            meshVisual.GetComponent<MeshFilter>().sharedMesh = rootFilter.sharedMesh;
            MeshRenderer copiedRenderer = meshVisual.GetComponent<MeshRenderer>();
            copiedRenderer.sharedMaterials = rootRenderer.sharedMaterials;
            copiedRenderer.shadowCastingMode = rootRenderer.shadowCastingMode;
            copiedRenderer.receiveShadows = rootRenderer.receiveShadows;
            rootRenderer.enabled = false;
        }

        return pivot;
    }

    private static void ConfigurePlayer(GameObject player, Transform root)
    {
        PlayerRunner runner = player.GetComponent<PlayerRunner>();
        if (runner != null)
        {
            runner.ConfigureForwardSpeed(6.4f);
            runner.SetDifficultyMultiplier(1f);
            EditorUtility.SetDirty(runner);
        }

        DifficultyDirector difficulty = player.GetComponent<DifficultyDirector>();
        if (difficulty == null)
        {
            difficulty = player.AddComponent<DifficultyDirector>();
        }
        difficulty.Configure(FinishZ, 1.34f);

        ComboController combo = player.GetComponent<ComboController>();
        if (combo == null)
        {
            combo = player.AddComponent<ComboController>();
        }

        Transform visual = FindChildRecursive(player.transform, "BalloonDogModel") ??
                           FindChildRecursive(player.transform, "Bubble");
        PlayerJuiceController juice = player.GetComponent<PlayerJuiceController>();
        if (juice == null)
        {
            juice = player.AddComponent<PlayerJuiceController>();
        }
        juice.Configure(visual);

        Transform sensorTransform = player.transform.Find("NearMissSensor");
        if (sensorTransform != null)
        {
            Object.DestroyImmediate(sensorTransform.gameObject);
        }

        GameObject sensorObject = new GameObject("NearMissSensor");
        sensorObject.transform.SetParent(player.transform, false);
        sensorObject.transform.localPosition = new Vector3(0f, 0.65f, 0.2f);
        SphereCollider sensorCollider = sensorObject.AddComponent<SphereCollider>();
        sensorCollider.isTrigger = true;
        sensorCollider.radius = 1.42f;
        NearMissSensor sensor = sensorObject.AddComponent<NearMissSensor>();
        sensor.Configure(player.transform, combo);

        BuildPlayerTrail(player.transform, visual, root);
    }

    private static void BuildPlayerTrail(Transform player, Transform visual, Transform root)
    {
        Transform existing = player.Find("V5BalloonTrail");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject trailObject = new GameObject("V5BalloonTrail");
        trailObject.transform.SetParent(player, false);
        trailObject.transform.localPosition = new Vector3(0f, 0.55f, -0.62f);
        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        trail.time = 0.36f;
        trail.startWidth = 0.18f;
        trail.endWidth = 0.015f;
        trail.minVertexDistance = 0.08f;
        trail.startColor = new Color(0.2f, 0.88f, 1f, 0.55f);
        trail.endColor = new Color(0.2f, 0.88f, 1f, 0f);
        trail.material = GetOrCreateMaterial("BDV5_Trail", Color.white, true, true);
    }

    private static void ConfigurePickups()
    {
        AirPickup[] pickups = Object.FindObjectsByType<AirPickup>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (AirPickup pickup in pickups)
        {
            if (pickup.GetComponent<CollectibleMagnet>() == null)
            {
                pickup.gameObject.AddComponent<CollectibleMagnet>();
            }
        }
    }

    private static void BuildWorld(Transform root)
    {
        Transform world = CreateGroup(root, "WorldPolish");

        Material darkRoad = GetOrCreateMaterial("BDV5_DarkRoad", new Color(0.065f, 0.095f, 0.15f), false, false);
        Material cyan = GetOrCreateMaterial("BDV5_Cyan", new Color(0.1f, 0.8f, 1f), true, false);
        Material orange = GetOrCreateMaterial("BDV5_Orange", new Color(1f, 0.37f, 0.08f), true, false);
        Material purple = GetOrCreateMaterial("BDV5_Purple", new Color(0.45f, 0.24f, 0.86f), false, false);
        Material hill = GetOrCreateMaterial("BDV5_Hill", new Color(0.12f, 0.24f, 0.32f), false, false);
        Material lamp = GetOrCreateMaterial("BDV5_Lamp", new Color(1f, 0.74f, 0.18f), true, false);
        Material white = GetOrCreateMaterial("BDV5_White", new Color(0.92f, 0.96f, 1f), false, false);

        // Her bölümün başında farklı renk şeridi ve kapı bulunur.
        float[] sectionZ = { 8f, 48f, 92f, 136f, 176f };
        Color[] sectionColors =
        {
            new Color(0.1f, 0.8f, 1f),
            new Color(0.22f, 0.9f, 0.48f),
            new Color(1f, 0.58f, 0.08f),
            new Color(0.7f, 0.28f, 1f),
            new Color(1f, 0.22f, 0.18f)
        };
        string[] sectionNames = { "START", "FLOW", "BOOST", "CHAOS", "FINISH" };

        for (int i = 0; i < sectionZ.Length; i++)
        {
            Material band = GetOrCreateMaterial($"BDV5_Section_{i}", sectionColors[i], true, false);
            CreateCube(world, $"SectionBand_{i}", new Vector3(0f, 0.035f, sectionZ[i]), new Vector3(8.1f, 0.025f, 1.2f), band, false);
            CreateSectionArch(world, sectionNames[i], sectionZ[i], band, white);
        }

        // Yol kenarında ritmik ışık direkleri hız hissini artırır.
        for (int i = 0; i < 25; i++)
        {
            float z = 5f + i * 7.25f;
            CreateLampPost(world, new Vector3(-5.1f, 0f, z), i % 2 == 0 ? cyan : lamp);
            CreateLampPost(world, new Vector3(5.1f, 0f, z + 3.6f), i % 2 == 0 ? lamp : cyan);
        }

        // Uzak katmanlar: dağlar ve yüzen balon dekorları.
        for (int i = 0; i < 14; i++)
        {
            float z = 16f + i * 15f;
            float side = i % 2 == 0 ? -1f : 1f;
            CreateMountain(world, new Vector3(side * (12f + (i % 3) * 2.2f), -0.4f, z), hill, 2.4f + (i % 4) * 0.45f);

            GameObject balloon = CreateSphere(
                world,
                $"SkyBalloon_{i:00}",
                new Vector3(-side * (7f + (i % 4)), 6.5f + (i % 3) * 1.1f, z + 5f),
                Vector3.one * (0.55f + (i % 2) * 0.18f),
                i % 2 == 0 ? cyan : purple,
                false);
            AmbientFloat ambient = balloon.AddComponent<AmbientFloat>();
            ambient.Configure(new Vector3(0.45f, 0.3f, 0f), 0.08f + i * 0.004f, new Vector3(0f, 6f, 3f), i * 0.7f);
        }

        // Yolun altına koyu temel ekleyerek gri boşluk hissini azaltır.
        CreateCube(world, "RoadUnderlay", new Vector3(0f, -0.72f, 150f), new Vector3(9.6f, 0.3f, 340f), darkRoad, false);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.36f, 0.56f, 0.68f);
        RenderSettings.fogDensity = 0.0048f;
        RenderSettings.ambientLight = new Color(0.38f, 0.46f, 0.55f);

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.backgroundColor = new Color(0.22f, 0.48f, 0.68f);
            camera.farClipPlane = 650f;
        }
    }

    private static void BuildObstacleVariety(Transform levelRoot, Transform root)
    {
        Transform obstacles = FindChildRecursive(levelRoot, "Obstacles");
        if (obstacles == null)
        {
            return;
        }

        // Bazı mevcut engelleri farklı hareket profillerine çevirir.
        int index = 0;
        foreach (Transform obstacle in obstacles)
        {
            string lower = obstacle.name.ToLowerInvariant();
            if (lower.Contains("lowgate") && index % 2 == 0)
            {
                MovingObstacle moving = obstacle.GetComponent<MovingObstacle>();
                if (moving == null)
                {
                    moving = obstacle.gameObject.AddComponent<MovingObstacle>();
                }
                moving.Configure(MovingObstacle.MotionMode.Vertical, 0.65f, 0.13f, index * 0.5f, RoadHalfWidth);
            }
            index++;
        }

        Transform challenge = CreateGroup(root, "BonusChallenges");
        Material cyan = GetOrCreateMaterial("BDV5_Cyan", new Color(0.1f, 0.8f, 1f), true, false);
        Material warning = GetOrCreateMaterial("BDV5_Orange", new Color(1f, 0.37f, 0.08f), true, false);

        // Risk/ödül rotaları için zemin okları.
        float[] zPositions = { 34f, 72f, 112f, 151f };
        for (int i = 0; i < zPositions.Length; i++)
        {
            float x = i % 2 == 0 ? -2.4f : 2.4f;
            CreateCube(challenge, $"SkillLaneArrow_{i}", new Vector3(x, 0.05f, zPositions[i]), new Vector3(1.1f, 0.03f, 2.2f), i % 2 == 0 ? cyan : warning, false);
        }
    }

    private static void BuildUi(Canvas canvas, GameObject player)
    {
        Transform oldRoot = FindChildRecursive(canvas.transform, "V5Interface");
        if (oldRoot != null)
        {
            Object.DestroyImmediate(oldRoot.gameObject);
        }

        GameObject uiRootObject = new GameObject("V5Interface", typeof(RectTransform));
        RectTransform uiRoot = uiRootObject.GetComponent<RectTransform>();
        uiRoot.SetParent(canvas.transform, false);
        Stretch(uiRoot);
        uiRoot.SetAsLastSibling();

        GameObject hudObject = new GameObject("V5GameplayHud", typeof(RectTransform));
        RectTransform hud = hudObject.GetComponent<RectTransform>();
        hud.SetParent(uiRoot, false);
        Stretch(hud);

        // Üst HUD arka plakaları.
        CreateUiPanel(hud, "TopLeftCard", new Vector2(0f, 1f), new Vector2(22f, -24f), new Vector2(330f, 118f), new Color(0.03f, 0.06f, 0.12f, 0.68f), new Vector2(0f, 1f));
        CreateUiPanel(hud, "TopRightCard", new Vector2(1f, 1f), new Vector2(-22f, -24f), new Vector2(250f, 118f), new Color(0.03f, 0.06f, 0.12f, 0.68f), new Vector2(1f, 1f));

        ComboController combo = player.GetComponent<ComboController>();
        GameObject comboHudObject = new GameObject("ComboHud", typeof(RectTransform));
        RectTransform comboHud = comboHudObject.GetComponent<RectTransform>();
        comboHud.SetParent(hud, false);
        SetRect(comboHud, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -255f), new Vector2(460f, 110f));

        TMP_Text comboText = CreateText(comboHud, "ComboText", string.Empty, 48f, new Color(1f, 0.78f, 0.12f));
        Stretch(comboText.rectTransform);
        comboText.alignment = TextAlignmentOptions.Center;

        GameObject comboFillObject = new GameObject("ComboTimerFill", typeof(RectTransform), typeof(Image));
        RectTransform comboFillRect = comboFillObject.GetComponent<RectTransform>();
        comboFillRect.SetParent(comboHud, false);
        SetRect(comboFillRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(300f, 10f));
        Image comboFill = comboFillObject.GetComponent<Image>();
        comboFill.color = new Color(1f, 0.65f, 0.08f, 0.9f);
        comboFill.type = Image.Type.Filled;
        comboFill.fillMethod = Image.FillMethod.Horizontal;

        ComboDisplay comboDisplay = comboHudObject.AddComponent<ComboDisplay>();
        comboDisplay.Configure(combo, comboText, comboFill);

        TMP_Text countdownText = CreateText(hud, "CountdownText", "3", 150f, Color.white);
        SetRect(countdownText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 230f));
        countdownText.alignment = TextAlignmentOptions.Center;
        Outline countdownOutline = countdownText.GetComponent<Outline>();
        if (countdownOutline == null)
        {
            countdownOutline = countdownText.gameObject.AddComponent<Outline>();
        }
        countdownOutline.effectColor = new Color32(5, 15, 28, 220);
        countdownOutline.effectDistance = new Vector2(4f, -4f);
        countdownOutline.useGraphicAlpha = true;
        StartCountdownController countdown = canvas.GetComponent<StartCountdownController>();
        if (countdown == null)
        {
            countdown = canvas.gameObject.AddComponent<StartCountdownController>();
        }
        countdown.Configure(countdownText);

        TMP_Text sectionBanner = CreateText(hud, "SectionBanner", "ISINMA", 64f, new Color(1f, 0.82f, 0.18f));
        SetRect(sectionBanner.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 100f));
        sectionBanner.alignment = TextAlignmentOptions.Center;
        SectionBannerController sectionController = canvas.GetComponent<SectionBannerController>();
        if (sectionController == null)
        {
            sectionController = canvas.gameObject.AddComponent<SectionBannerController>();
        }
        sectionController.Configure(
            player.transform,
            sectionBanner,
            new[] { 32f, 76f, 118f, 156f },
            new[] { "ISINMA", "HIZLAN", "USTALIK", "FİNAL" });

        GameObject tutorialCard = CreateUiPanel(
            hud,
            "TutorialCard",
            new Vector2(0.5f, 0f),
            new Vector2(0f, 178f),
            new Vector2(760f, 86f),
            new Color(0.02f, 0.05f, 0.1f, 0.74f),
            new Vector2(0.5f, 0f));
        TMP_Text tutorialText = CreateText(
            tutorialCard.transform,
            "TutorialText",
            "SÜRÜKLE: KAÇ  •  ÇİFT DOKUN: HELİKOPTER  •  KÜÇÜL: HAVA HARCAr",
            26f,
            Color.white);
        Stretch(tutorialText.rectTransform);
        tutorialText.alignment = TextAlignmentOptions.Center;
        tutorialCard.AddComponent<UiAutoHide>();

        Button pauseButton = CreateButton(hud, "PauseButton", "Ⅱ", new Vector2(1f, 1f), new Vector2(-30f, -156f), new Vector2(90f, 90f), new Color(0.05f, 0.12f, 0.22f, 0.9f), new Vector2(1f, 1f));

        GameObject pausePanel = CreateUiPanel(uiRoot, "PausePanel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080f, 1920f), new Color(0.01f, 0.025f, 0.06f, 0.9f), new Vector2(0.5f, 0.5f));
        TMP_Text pauseTitle = CreateText(pausePanel.transform, "PauseTitle", "DURAKLATILDI", 78f, Color.white);
        SetRect(pauseTitle.rectTransform, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 130f));
        pauseTitle.alignment = TextAlignmentOptions.Center;
        Button resume = CreateButton(pausePanel.transform, "ResumeButton", "DEVAM ET", new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(520f, 120f), new Color(0.08f, 0.72f, 0.95f, 1f), new Vector2(0.5f, 0.5f));
        Button restart = CreateButton(pausePanel.transform, "PauseRestartButton", "YENİDEN BAŞLA", new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(520f, 110f), new Color(0.95f, 0.45f, 0.08f, 1f), new Vector2(0.5f, 0.5f));
        Button menu = CreateButton(pausePanel.transform, "PauseMenuButton", "ANA MENÜ", new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(520f, 105f), new Color(0.24f, 0.28f, 0.38f, 1f), new Vector2(0.5f, 0.5f));
        pausePanel.SetActive(false);

        PauseMenuController pauseController = canvas.GetComponent<PauseMenuController>();
        if (pauseController == null)
        {
            pauseController = canvas.gameObject.AddComponent<PauseMenuController>();
        }
        pauseController.Configure(pausePanel, pauseButton, resume, restart, menu);

        ConfigureAirHudJuice(canvas, player);
    }

    private static void ConfigureAirHudJuice(Canvas canvas, GameObject player)
    {
        AirController airController = player.GetComponent<AirController>();
        Slider slider = FindChildComponent<Slider>(canvas.transform, "AirSlider");
        TMP_Text airText = FindChildComponent<TMP_Text>(canvas.transform, "AirText");
        Image fill = null;
        if (slider != null && slider.fillRect != null)
        {
            fill = slider.fillRect.GetComponent<Image>();
        }

        AirHudJuice juice = canvas.GetComponent<AirHudJuice>();
        if (juice == null)
        {
            juice = canvas.gameObject.AddComponent<AirHudJuice>();
        }
        juice.Configure(airController, fill, airText);
    }

    private static void PolishMainMenu(Canvas canvas)
    {
        GameObject mainMenu = FindSceneObject("MainMenuPanel");
        if (mainMenu == null)
        {
            return;
        }

        Image background = mainMenu.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color(0.025f, 0.06f, 0.12f, 0.96f);
        }

        Transform oldDecoration = mainMenu.transform.Find("V5MenuDecoration");
        if (oldDecoration != null)
        {
            Object.DestroyImmediate(oldDecoration.gameObject);
        }

        GameObject decorationObject = new GameObject("V5MenuDecoration", typeof(RectTransform));
        RectTransform decoration = decorationObject.GetComponent<RectTransform>();
        decoration.SetParent(mainMenu.transform, false);
        Stretch(decoration);
        decoration.SetAsFirstSibling();

        for (int i = 0; i < 9; i++)
        {
            GameObject circle = new GameObject($"Bubble_{i}", typeof(RectTransform), typeof(Image));
            RectTransform rect = circle.GetComponent<RectTransform>();
            rect.SetParent(decoration, false);
            float side = i % 2 == 0 ? -1f : 1f;
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(side * (260f + i * 28f), -620f + i * 175f), Vector2.one * (90f + (i % 3) * 55f));
            Image image = circle.GetComponent<Image>();
            image.color = i % 2 == 0
                ? new Color(0.1f, 0.8f, 1f, 0.12f)
                : new Color(1f, 0.45f, 0.1f, 0.1f);
            image.raycastTarget = false;
        }

        TMP_Text subtitle = FindChildComponent<TMP_Text>(mainMenu.transform, "V5Subtitle");
        if (subtitle == null)
        {
            subtitle = CreateText(mainMenu.transform, "V5Subtitle", "ŞİŞ • KAÇ • UÇ • FIRLAT", 31f, new Color(0.35f, 0.88f, 1f));
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 335f), new Vector2(760f, 70f));
            subtitle.alignment = TextAlignmentOptions.Center;
        }
    }

    private static void PolishFinish(Transform root)
    {
        Transform finish = CreateGroup(root, "FinishPolish");
        Material gold = GetOrCreateMaterial("BDV5_Gold", new Color(1f, 0.65f, 0.08f), true, false);
        Material cyan = GetOrCreateMaterial("BDV5_Cyan", new Color(0.1f, 0.8f, 1f), true, false);
        Material white = GetOrCreateMaterial("BDV5_White", new Color(0.92f, 0.96f, 1f), false, false);

        for (int i = 0; i < 18; i++)
        {
            float z = FinishZ + 4f + i * 5.2f;
            Material material = i % 2 == 0 ? cyan : gold;
            CreateCube(finish, $"LaunchStripe_{i:00}", new Vector3(0f, 0.055f, z), new Vector3(8f, 0.02f, 0.35f), material, false);
        }

        CreateSectionArch(finish, "LAUNCH", FinishZ + 2.5f, gold, white);

        // Bitiş collider'ını geniş ve güvenli hale getirir.
        FinishLine finishLine = Object.FindAnyObjectByType<FinishLine>();
        if (finishLine != null)
        {
            Collider collider = finishLine.GetComponent<Collider>();
            if (collider == null)
            {
                collider = finishLine.gameObject.AddComponent<BoxCollider>();
            }
            collider.isTrigger = true;
            if (collider is BoxCollider box)
            {
                box.size = new Vector3(12f, 8f, 2.2f);
                box.center = new Vector3(0f, 2f, 0f);
            }
            Vector3 position = finishLine.transform.position;
            position.x = 0f;
            position.z = FinishZ;
            finishLine.transform.position = position;
            EditorUtility.SetDirty(finishLine);
        }
    }

    private static void EnsureAudioSystem()
    {
        GameAudioController audio = Object.FindAnyObjectByType<GameAudioController>();
        if (audio == null)
        {
            GameObject audioObject = new GameObject("GameAudioController");
            audioObject.AddComponent<GameAudioController>();
        }

        PerformanceBootstrap performance = Object.FindAnyObjectByType<PerformanceBootstrap>();
        if (performance == null)
        {
            GameObject performanceObject = new GameObject("PerformanceBootstrap");
            performanceObject.AddComponent<PerformanceBootstrap>();
        }
    }

    private static void ApplyPerformanceSettings()
    {
        QualitySettings.shadowDistance = 55f;
        QualitySettings.lodBias = 1.15f;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    private static void ClampInsideRoad(Transform obstacle, float halfWidth)
    {
        if (!TryCalculateBounds(obstacle, out Bounds bounds))
        {
            Vector3 position = obstacle.position;
            position.x = Mathf.Clamp(position.x, -halfWidth, halfWidth);
            obstacle.position = position;
            return;
        }

        float halfSize = bounds.extents.x;
        float safeCenter = Mathf.Max(0f, halfWidth - halfSize);
        float targetCenter = Mathf.Clamp(bounds.center.x, -safeCenter, safeCenter);
        obstacle.position += Vector3.right * (targetCenter - bounds.center.x);
    }

    private static void CreateSectionArch(Transform parent, string label, float z, Material archMaterial, Material labelMaterial)
    {
        CreateCube(parent, $"{label}_ArchLeft", new Vector3(-4.45f, 2.15f, z), new Vector3(0.28f, 4.3f, 0.35f), archMaterial, false);
        CreateCube(parent, $"{label}_ArchRight", new Vector3(4.45f, 2.15f, z), new Vector3(0.28f, 4.3f, 0.35f), archMaterial, false);
        CreateCube(parent, $"{label}_ArchTop", new Vector3(0f, 4.25f, z), new Vector3(9.2f, 0.34f, 0.35f), archMaterial, false);

        GameObject labelObject = new GameObject($"{label}_WorldLabel", typeof(RectTransform), typeof(TextMeshPro));
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.position = new Vector3(0f, 3.8f, z - 0.22f);
        labelObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        TextMeshPro text = labelObject.GetComponent<TextMeshPro>();
        text.text = label;
        text.fontSize = 4.4f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.rectTransform.sizeDelta = new Vector2(7f, 1.2f);
    }

    private static void CreateLampPost(Transform parent, Vector3 position, Material glowMaterial)
    {
        Material pole = GetOrCreateMaterial("BDV5_Pole", new Color(0.06f, 0.09f, 0.14f), false, false);
        CreateCylinder(parent, "LampPole", position + Vector3.up * 1.25f, new Vector3(0.09f, 1.25f, 0.09f), pole);
        CreateSphere(parent, "LampGlow", position + Vector3.up * 2.55f, Vector3.one * 0.32f, glowMaterial, false);
    }

    private static void CreateMountain(Transform parent, Vector3 position, Material material, float scale)
    {
        GameObject mountain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mountain.name = "LowPolyMountain";
        mountain.transform.SetParent(parent, false);
        mountain.transform.position = position;
        mountain.transform.localScale = new Vector3(scale * 1.8f, scale, scale * 1.8f);
        Renderer renderer = mountain.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        Collider collider = mountain.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static GameObject CreateUiPanel(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 position,
        Vector2 size,
        Color color,
        Vector2 pivot)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetRect(rect, anchor, anchor, pivot, position, size);
        Image image = panel.GetComponent<Image>();
        image.color = color;
        return panel;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchor,
        Vector2 position,
        Vector2 size,
        Color color,
        Vector2 pivot)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetRect(rect, anchor, anchor, pivot, position, size);
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateText(buttonObject.transform, "Label", label, 34f, Color.white);
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static TMP_Text CreateText(Transform parent, string name, string content, float size, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.raycastTarget = false;

        TMP_Text existing = Object.FindAnyObjectByType<TMP_Text>();
        if (existing != null && existing.font != null)
        {
            text.font = existing.font;
        }
        return text;
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool collider)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider)
        {
            Collider existing = gameObject.GetComponent<Collider>();
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }
        return gameObject;
    }

    private static GameObject CreateSphere(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool collider)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider)
        {
            Collider existing = gameObject.GetComponent<Collider>();
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }
        return gameObject;
    }

    private static GameObject CreateCylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        Collider existing = gameObject.GetComponent<Collider>();
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
        return gameObject;
    }

    private static Material GetOrCreateMaterial(string name, Color color, bool emission, bool transparent)
    {
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "BalloonDogV5");
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
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
            material.SetFloat("_Smoothness", emission ? 0.4f : 0.2f);
        }
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.7f);
        }
        if (transparent)
        {
            color.a = Mathf.Min(color.a, 0.65f);
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
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
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }
            Transform nested = FindChildRecursive(child, name);
            if (nested != null)
            {
                return nested;
            }
        }
        return null;
    }

    private static T FindChildComponent<T>(Transform parent, string name) where T : Component
    {
        Transform transform = FindChildRecursive(parent, name);
        return transform != null ? transform.GetComponent<T>() : null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform transform in transforms)
        {
            if (transform == null || transform.name != objectName || !transform.gameObject.scene.IsValid())
            {
                continue;
            }
            return transform.gameObject;
        }
        return null;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
#endif
