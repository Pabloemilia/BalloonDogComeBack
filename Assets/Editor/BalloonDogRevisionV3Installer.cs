#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BalloonDogRevisionV3Installer
{
    private const string MenuPath = "Tools/BalloonDog/Revize V3 - Menü, Harita ve Bitiş";
    private const float FinishZ = 185f;
    private const float RoadHalfWidth = 4f;

    [MenuItem(MenuPath)]
    public static void ApplyRevisionFromMenu()
    {
        ApplyRevisionToActiveScene(true);
    }

    public static void ApplyRevisionToActiveScene(bool saveScene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "BalloonDog Revize V3",
                "Önce Play modunu kapat.",
                "Tamam");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog(
                "BalloonDog Revize V3",
                "Önce Assets/Scenes/Game.unity sahnesini aç.",
                "Tamam");
            return;
        }

        GameObject player = FindSceneObject("player") ?? FindSceneObject("Player");
        GameObject levelRoot = FindSceneObject(BalloonDogPrototypeBootstrap.BakedLevelRootName);

        if (player == null || levelRoot == null)
        {
            EditorUtility.DisplayDialog(
                "BalloonDog Revize V3",
                "Sahnede player ve BalloonDog_Level bulunamadı. Önce Tools > BalloonDog > Sahneyi Düzenlenebilir Hale Getir aracını çalıştır.",
                "Tamam");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("BalloonDog Revize V3", "Harita genişletiliyor...", 0.15f);
            UpgradeRoadAndMovement(player, levelRoot);

            EditorUtility.DisplayProgressBar("BalloonDog Revize V3", "Engeller düzenleniyor...", 0.35f);
            FixAndExtendObstacles(levelRoot);

            EditorUtility.DisplayProgressBar("BalloonDog Revize V3", "Bitiş sistemi düzeltiliyor...", 0.55f);
            FixFinish(levelRoot);

            EditorUtility.DisplayProgressBar("BalloonDog Revize V3", "Arka plan hazırlanıyor...", 0.72f);
            BuildBackground(levelRoot);

            EditorUtility.DisplayProgressBar("BalloonDog Revize V3", "Başlangıç menüsü hazırlanıyor...", 0.88f);
            BuildStartMenu();

            MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);

            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
                EditorUtility.DisplayDialog(
                    "BalloonDog Revize V3 hazır",
                    "Bitiş düzeltildi, başlangıç menüsü eklendi, yol genişletildi, engeller hizalandı ve dönen engeller etkinleştirildi.",
                    "Tamam");
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog(
                "BalloonDog Revize V3 uygulanamadı",
                "Console'daki ilk kırmızı hatayı kontrol et.\n\n" + exception.Message,
                "Tamam");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static void UpgradeRoadAndMovement(GameObject player, GameObject levelRoot)
    {
        Transform environment = FindChildRecursive(levelRoot.transform, "Environment");
        GameObject road = FindSceneObject("Road");

        if (road == null)
        {
            road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "Road";
            road.transform.SetParent(environment != null ? environment : levelRoot.transform, false);
        }

        road.transform.position = new Vector3(0f, -0.5f, 170f);
        road.transform.localScale = new Vector3(RoadHalfWidth * 2f, 1f, 380f);
        road.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
            "BDV3_Road",
            new Color(0.15f, 0.20f, 0.28f));

        PlayerHorizontalController horizontal =
            player.GetComponent<PlayerHorizontalController>();
        if (horizontal != null)
        {
            horizontal.ConfigureRoadLimit(3.25f);
            EditorUtility.SetDirty(horizontal);
        }
    }

    private static void FixAndExtendObstacles(GameObject levelRoot)
    {
        Transform obstacles = FindChildRecursive(levelRoot.transform, "Obstacles");
        Transform collectibles = FindChildRecursive(levelRoot.transform, "Collectibles");
        Transform decorations = FindChildRecursive(levelRoot.transform, "Decorations");

        if (obstacles == null)
        {
            return;
        }

        foreach (Transform obstacle in obstacles.GetComponentsInChildren<Transform>(true))
        {
            if (obstacle == obstacles)
            {
                continue;
            }

            if (obstacle.parent == obstacles)
            {
                Vector3 position = obstacle.position;
                position.x = Mathf.Clamp(position.x, -2.45f, 2.45f);
                obstacle.position = position;
            }

            ConfigureRotationForObstacle(obstacle);
        }

        if (decorations != null)
        {
            for (int i = decorations.childCount - 1; i >= 0; i--)
            {
                Transform child = decorations.GetChild(i);
                if (child.name.Contains("SideDecoration"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        CloneSceneObject("Gear_60", "Gear_151", obstacles, new Vector3(-2.1f, 0f, 151f));
        CloneSceneObject("Cylinder_115", "Cylinder_160", obstacles, new Vector3(2.05f, 0f, 160f));
        CloneSceneObject("Spiral_107", "Spiral_169", obstacles, new Vector3(-1.9f, 0f, 169f));
        CloneSceneObject("BlueBalloon_74", "BlueBalloon_156", collectibles, new Vector3(0f, 1.2f, 156f));
        CloneSceneObject("BlueBalloon_74", "BlueBalloon_174_Left", collectibles, new Vector3(-1.8f, 1.2f, 174f));
        CloneSceneObject("BlueBalloon_74", "BlueBalloon_174_Right", collectibles, new Vector3(1.8f, 1.2f, 174f));

        GameObject clonedGear = FindSceneObject("Gear_151");
        if (clonedGear != null)
        {
            ConfigureRotationForObstacle(clonedGear.transform);
        }

        GameObject clonedCylinder = FindSceneObject("Cylinder_160");
        if (clonedCylinder != null)
        {
            ConfigureRotationForObstacle(clonedCylinder.transform);
        }

        GameObject clonedSpiral = FindSceneObject("Spiral_169");
        if (clonedSpiral != null)
        {
            ConfigureRotationForObstacle(clonedSpiral.transform);
        }
    }

    private static void ConfigureRotationForObstacle(Transform obstacle)
    {
        string lowerName = obstacle.name.ToLowerInvariant();
        if (!lowerName.Contains("gear") &&
            !lowerName.Contains("spiral") &&
            !lowerName.Contains("cylinder"))
        {
            return;
        }

        Transform visual = obstacle;
        foreach (Transform child in obstacle.GetComponentsInChildren<Transform>(true))
        {
            if (child != obstacle && child.GetComponentInChildren<Renderer>() != null)
            {
                visual = child;
                break;
            }
        }

        RotatingObstacle rotating = visual.GetComponent<RotatingObstacle>();
        if (rotating == null)
        {
            rotating = visual.gameObject.AddComponent<RotatingObstacle>();
        }

        if (lowerName.Contains("gear"))
        {
            rotating.Configure(Vector3.forward, 150f);
        }
        else if (lowerName.Contains("spiral"))
        {
            rotating.Configure(Vector3.up, 130f);
        }
        else
        {
            rotating.Configure(Vector3.right, 110f);
        }

        EditorUtility.SetDirty(rotating);
    }

    private static void CloneSceneObject(
        string sourceName,
        string cloneName,
        Transform parent,
        Vector3 position)
    {
        if (parent == null || FindSceneObject(cloneName) != null)
        {
            return;
        }

        GameObject source = FindSceneObject(sourceName);
        if (source == null)
        {
            return;
        }

        GameObject clone = Object.Instantiate(source, parent);
        clone.name = cloneName;
        clone.transform.position = position;
        clone.SetActive(true);
    }

    private static void FixFinish(GameObject levelRoot)
    {
        GameObject finish = FindSceneObject("FinishLine");
        if (finish == null)
        {
            Transform finishRoot = FindChildRecursive(levelRoot.transform, "FinishSystem");
            finish = new GameObject("FinishLine");
            finish.transform.SetParent(finishRoot != null ? finishRoot : levelRoot.transform, false);
        }

        finish.transform.position = new Vector3(0f, 0f, FinishZ);

        BoxCollider trigger = finish.GetComponent<BoxCollider>();
        if (trigger == null)
        {
            trigger = finish.AddComponent<BoxCollider>();
        }

        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 2f, 0f);
        trigger.size = new Vector3(8f, 5f, 4f);

        if (finish.GetComponent<FinishLine>() == null)
        {
            finish.AddComponent<FinishLine>();
        }

        Transform stripeRoot = FindChildRecursive(finish.transform, "FinishStripe");
        if (stripeRoot != null)
        {
            Object.DestroyImmediate(stripeRoot.gameObject);
        }

        GameObject stripe = new GameObject("FinishStripe");
        stripe.transform.SetParent(finish.transform, false);

        Material white = GetOrCreateMaterial("BDV3_FinishWhite", Color.white);
        Material dark = GetOrCreateMaterial("BDV3_FinishDark", new Color(0.04f, 0.05f, 0.07f));

        for (int x = 0; x < 8; x++)
        {
            for (int z = 0; z < 2; z++)
            {
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = "FinishTile";
                tile.transform.SetParent(stripe.transform, false);
                tile.transform.localPosition = new Vector3(-3.5f + x, 0.04f, -0.55f + z * 1.1f);
                tile.transform.localScale = new Vector3(1f, 0.08f, 1.1f);
                tile.GetComponent<Renderer>().sharedMaterial = (x + z) % 2 == 0 ? white : dark;
                Object.DestroyImmediate(tile.GetComponent<Collider>());
            }
        }
    }

    private static void BuildBackground(GameObject levelRoot)
    {
        GameObject oldBackground = FindSceneObject("BalloonDog_Background");
        if (oldBackground != null)
        {
            Object.DestroyImmediate(oldBackground);
        }

        GameObject background = new GameObject("BalloonDog_Background");
        background.transform.SetParent(levelRoot.transform, false);

        Material grass = GetOrCreateMaterial("BDV3_Grass", new Color(0.22f, 0.46f, 0.32f));
        Material curb = GetOrCreateMaterial("BDV3_Curb", new Color(0.90f, 0.92f, 0.96f));
        Material hill = GetOrCreateMaterial("BDV3_Hill", new Color(0.18f, 0.34f, 0.30f));
        Material trunk = GetOrCreateMaterial("BDV3_Trunk", new Color(0.32f, 0.19f, 0.10f));
        Material leaves = GetOrCreateMaterial("BDV3_Leaves", new Color(0.18f, 0.55f, 0.28f));

        CreatePrimitive(
            "LeftGround",
            PrimitiveType.Cube,
            background.transform,
            new Vector3(-10f, -0.78f, 170f),
            new Vector3(12f, 0.55f, 380f),
            grass,
            true);
        CreatePrimitive(
            "RightGround",
            PrimitiveType.Cube,
            background.transform,
            new Vector3(10f, -0.78f, 170f),
            new Vector3(12f, 0.55f, 380f),
            grass,
            true);

        CreatePrimitive(
            "LeftCurb",
            PrimitiveType.Cube,
            background.transform,
            new Vector3(-4.1f, -0.10f, 170f),
            new Vector3(0.20f, 0.75f, 380f),
            curb,
            false);
        CreatePrimitive(
            "RightCurb",
            PrimitiveType.Cube,
            background.transform,
            new Vector3(4.1f, -0.10f, 170f),
            new Vector3(0.20f, 0.75f, 380f),
            curb,
            false);

        GameObject markers = new GameObject("LaneMarkers");
        markers.transform.SetParent(background.transform, false);
        for (float z = 8f; z < FinishZ; z += 12f)
        {
            CreatePrimitive(
                "LaneMarker",
                PrimitiveType.Cube,
                markers.transform,
                new Vector3(0f, 0.03f, z),
                new Vector3(0.12f, 0.05f, 4.5f),
                curb,
                false);
        }

        for (int index = 0; index < 10; index++)
        {
            float z = 18f + index * 20f;
            float leftX = -7.2f - (index % 3) * 1.1f;
            float rightX = 7.2f + ((index + 1) % 3) * 1.1f;

            CreateHill(background.transform, new Vector3(leftX, 0.2f, z), hill, index);
            CreateHill(background.transform, new Vector3(rightX, 0.2f, z + 8f), hill, index + 1);

            if (index % 2 == 0)
            {
                CreateTree(background.transform, new Vector3(-5.8f, 0f, z + 5f), trunk, leaves);
                CreateTree(background.transform, new Vector3(5.8f, 0f, z + 13f), trunk, leaves);
            }
        }

        Color skyColor = new Color(0.48f, 0.76f, 0.92f);
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = skyColor;
            mainCamera.farClipPlane = 650f;
            EditorUtility.SetDirty(mainCamera);
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.72f, 0.78f, 0.84f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = skyColor;
        RenderSettings.fogStartDistance = 70f;
        RenderSettings.fogEndDistance = 280f;

        Light directional = Object.FindAnyObjectByType<Light>();
        if (directional != null)
        {
            directional.color = new Color(1f, 0.92f, 0.78f);
            directional.intensity = 1.35f;
            directional.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            EditorUtility.SetDirty(directional);
        }
    }

    private static void CreateHill(Transform parent, Vector3 position, Material material, int index)
    {
        GameObject hill = CreatePrimitive(
            "Hill",
            PrimitiveType.Sphere,
            parent,
            position,
            new Vector3(5.5f + index % 3, 2.4f + (index % 2) * 0.5f, 4.5f),
            material,
            false);
        hill.transform.rotation = Quaternion.Euler(0f, index * 17f, 0f);
    }

    private static void CreateTree(
        Transform parent,
        Vector3 position,
        Material trunkMaterial,
        Material leavesMaterial)
    {
        GameObject tree = new GameObject("Tree");
        tree.transform.SetParent(parent, false);
        tree.transform.position = position;

        CreatePrimitive(
            "Trunk",
            PrimitiveType.Cylinder,
            tree.transform,
            new Vector3(0f, 1f, 0f),
            new Vector3(0.38f, 1f, 0.38f),
            trunkMaterial,
            false,
            true);
        CreatePrimitive(
            "Leaves",
            PrimitiveType.Sphere,
            tree.transform,
            new Vector3(0f, 2.7f, 0f),
            new Vector3(1.7f, 2f, 1.7f),
            leavesMaterial,
            false,
            true);
    }

    private static void BuildStartMenu()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        DestroyNamedChild(canvas.transform, "MainMenuPanel");
        DestroyNamedChild(canvas.transform, "SettingsPanel");

        GameObject mainPanel = CreatePanel(
            canvas.transform,
            "MainMenuPanel",
            new Color(0.035f, 0.075f, 0.14f, 0.98f));

        TMP_Text title = CreateText(mainPanel.transform, "MenuTitle", "BALLOON DOG", 92f);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(950f, 150f));
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.20f, 0.86f, 1f);

        TMP_Text subtitle = CreateText(
            mainPanel.transform,
            "MenuSubtitle",
            "Havayı koru • Engelleri aş • Uzağa fırlat!",
            38f);
        SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.66f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 100f));
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.color = new Color(0.88f, 0.93f, 1f);

        Button playButton = CreateButton(mainPanel.transform, "PlayButton", "OYNA", new Color(0.06f, 0.70f, 0.95f));
        SetRect(playButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.43f), new Vector2(0.5f, 0.43f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 150f));

        Button settingsButton = CreateButton(mainPanel.transform, "SettingsButton", "AYARLAR", new Color(0.19f, 0.29f, 0.46f));
        SetRect(settingsButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.31f), new Vector2(0.5f, 0.31f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 130f));

        Button exitButton = CreateButton(mainPanel.transform, "ExitButton", "ÇIKIŞ", new Color(0.38f, 0.16f, 0.20f));
        SetRect(exitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.20f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 120f));

        TMP_Text version = CreateText(mainPanel.transform, "VersionText", "PROTOTİP V3", 27f);
        SetRect(version.rectTransform, new Vector2(0.5f, 0.05f), new Vector2(0.5f, 0.05f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 60f));
        version.alignment = TextAlignmentOptions.Center;
        version.color = new Color(0.55f, 0.65f, 0.76f);

        GameObject settingsPanel = CreatePanel(
            canvas.transform,
            "SettingsPanel",
            new Color(0.035f, 0.075f, 0.14f, 0.99f));

        TMP_Text settingsTitle = CreateText(settingsPanel.transform, "SettingsTitle", "AYARLAR", 82f);
        SetRect(settingsTitle.rectTransform, new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 140f));
        settingsTitle.alignment = TextAlignmentOptions.Center;

        Button sound = CreateButton(settingsPanel.transform, "SoundToggleButton", "SES: AÇIK", new Color(0.15f, 0.52f, 0.67f), "SoundButtonLabel");
        SetRect(sound.GetComponent<RectTransform>(), new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 135f));

        Button vibration = CreateButton(settingsPanel.transform, "VibrationToggleButton", "TİTREŞİM: AÇIK", new Color(0.15f, 0.52f, 0.67f), "VibrationButtonLabel");
        SetRect(vibration.GetComponent<RectTransform>(), new Vector2(0.5f, 0.40f), new Vector2(0.5f, 0.40f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 135f));

        Button back = CreateButton(settingsPanel.transform, "SettingsBackButton", "GERİ", new Color(0.25f, 0.30f, 0.43f));
        SetRect(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0.23f), new Vector2(0.5f, 0.23f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 120f));

        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);

        if (canvas.GetComponent<MainMenuController>() == null)
        {
            canvas.gameObject.AddComponent<MainMenuController>();
        }

        GameObject endPanel = FindSceneObject("RevisionEndPanel") ??
            FindSceneObject("EndPanel") ??
            FindSceneObject("GameOverPanel");
        if (endPanel != null && FindChildRecursive(endPanel.transform, "EndMenuButton") == null)
        {
            Button menuButton = CreateButton(endPanel.transform, "EndMenuButton", "ANA MENÜ", new Color(0.24f, 0.31f, 0.46f));
            SetRect(menuButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -345f), new Vector2(460f, 115f));
        }
    }

    private static GameObject CreatePrimitive(
        string name,
        PrimitiveType type,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Material material,
        bool keepCollider,
        bool positionIsLocal = false)
    {
        GameObject created = GameObject.CreatePrimitive(type);
        created.name = name;
        created.transform.SetParent(parent, false);

        if (positionIsLocal)
        {
            created.transform.localPosition = position;
        }
        else
        {
            created.transform.position = position;
        }

        created.transform.localScale = scale;
        created.GetComponent<Renderer>().sharedMaterial = material;

        if (!keepCollider)
        {
            Object.DestroyImmediate(created.GetComponent<Collider>());
        }

        return created;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = CreateUiObject(name, parent);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        Stretch(panel.GetComponent<RectTransform>());
        return panel;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Color color,
        string labelName = "Label")
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateText(buttonObject.transform, labelName, label, 42f);
        Stretch(text.rectTransform, 16f);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return button;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float size)
    {
        GameObject textObject = CreateUiObject(name, parent);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Material GetOrCreateMaterial(string name, Color color)
    {
        const string root = "Assets/Materials";
        const string folder = "Assets/Materials/BalloonDogRevisionV3";

        if (!AssetDatabase.IsValidFolder(root))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(root, "BalloonDogRevisionV3");
        }

        string path = $"{folder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        if (material == null)
        {
            material = new Material(shader);
            material.name = name;
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
            material.color = color;
            EditorUtility.SetDirty(material);
        }

        return material;
    }

    private static void DestroyNamedChild(Transform parent, string name)
    {
        Transform child = FindChildRecursive(parent, name);
        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
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

    private static GameObject FindSceneObject(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform transform in transforms)
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

    private static void Stretch(RectTransform rect, float margin = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(margin, margin);
        rect.offsetMax = new Vector2(-margin, -margin);
        rect.localScale = Vector3.one;
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
