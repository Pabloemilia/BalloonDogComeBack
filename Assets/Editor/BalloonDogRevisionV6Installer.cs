#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BalloonDogRevisionV6Installer
{
    private const string MenuPath = "Tools/BalloonDog/Revize V6 - Oynanış ve Menü Düzeltme";
    private const string MenuPolishName = "V6MenuPolish";
    private const string WorldPolishName = "BalloonDog_V6_Polish";
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
            EditorUtility.DisplayDialog("BalloonDog V6", "Önce Play modunu kapat.", "Tamam");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("BalloonDog V6", "Önce Game.unity sahnesini aç.", "Tamam");
            return;
        }

        GameObject player = FindSceneObject("player") ?? FindSceneObject("Player");
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        GameObject levelRoot = FindSceneObject(BalloonDogPrototypeBootstrap.BakedLevelRootName);

        if (player == null || canvas == null || levelRoot == null)
        {
            EditorUtility.DisplayDialog(
                "BalloonDog V6",
                "Player, Canvas veya BalloonDog_Level bulunamadı. Önce düzenlenebilir sahne/V5 kurulumunu uygula.",
                "Tamam");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("BalloonDog V6", "Oyuncu ve hava sistemi düzeltiliyor...", 0.12f);
            ConfigurePlayer(player);

            EditorUtility.DisplayProgressBar("BalloonDog V6", "Sahte çarpışmalar ve engeller düzeltiliyor...", 0.35f);
            RepairObstacles(levelRoot.transform);

            EditorUtility.DisplayProgressBar("BalloonDog V6", "Başlangıç menüsü yenileniyor...", 0.63f);
            PolishMenus(canvas);

            EditorUtility.DisplayProgressBar("BalloonDog V6", "Çevre detayları ekleniyor...", 0.83f);
            BuildWorldPolish(levelRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();

            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
                EditorUtility.DisplayDialog(
                    "BalloonDog V6 hazır",
                    "Sahte engel çarpışmaları kaldırıldı, oyun tam havayla başlıyor, kontroller hızlandırıldı, engel pivotları sabitlendi ve menü yenilendi.",
                    "Tamam");
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog(
                "BalloonDog V6 uygulanamadı",
                "Console'daki ilk kırmızı hatayı kontrol et.\n\n" + exception.Message,
                "Tamam");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static void ConfigurePlayer(GameObject player)
    {
        AirController air = player.GetComponent<AirController>();
        if (air != null)
        {
            air.ConfigureStartAir(100f, 100f, true);
            EditorUtility.SetDirty(air);
        }

        PlayerHorizontalController horizontal = player.GetComponent<PlayerHorizontalController>();
        if (horizontal != null)
        {
            horizontal.ConfigureControls(3.7f, 10f, 17f, 95f);
            EditorUtility.SetDirty(horizontal);
        }

        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.radius = Mathf.Min(capsule.radius, 0.46f);
            capsule.height = Mathf.Clamp(capsule.height, 1.35f, 1.62f);
            EditorUtility.SetDirty(capsule);
        }

        NearMissSensor sensor = player.GetComponentInChildren<NearMissSensor>(true);
        if (sensor != null)
        {
            SphereCollider sensorCollider = sensor.GetComponent<SphereCollider>();
            if (sensorCollider != null)
            {
                sensorCollider.radius = 1.15f;
                sensorCollider.isTrigger = true;
                EditorUtility.SetDirty(sensorCollider);
            }
        }
    }

    private static void RepairObstacles(Transform levelRoot)
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

        foreach (Transform obstacle in obstacleList)
        {
            foreach (MovingObstacle moving in obstacle.GetComponentsInChildren<MovingObstacle>(true))
            {
                Object.DestroyImmediate(moving);
            }

            RemoveOldRotationSetup(obstacle);
            ClampInsideRoad(obstacle, RoadHalfWidth - 0.35f);
            TuneColliderOnce(obstacle);

            string lower = obstacle.name.ToLowerInvariant();
            if (lower.Contains("gear"))
            {
                AddStableRotation(obstacle, Vector3.forward, 44f);
            }
            else if (lower.Contains("spiral"))
            {
                AddStableRotation(obstacle, Vector3.up, 34f);
            }
            else if (lower.Contains("cylinder"))
            {
                AddStableRotation(obstacle, Vector3.right, 38f);
            }
        }
    }

    private static void RemoveOldRotationSetup(Transform obstacle)
    {
        foreach (RotatingObstacle rotating in obstacle.GetComponentsInChildren<RotatingObstacle>(true))
        {
            Object.DestroyImmediate(rotating);
        }

        UnwrapPivot(obstacle, "StableSpinPivot");
        UnwrapPivot(obstacle, "RotationVisualPivot");
    }

    private static void UnwrapPivot(Transform obstacle, string pivotName)
    {
        Transform pivot = obstacle.Find(pivotName);
        if (pivot == null)
        {
            return;
        }

        List<Transform> children = new List<Transform>();
        for (int i = 0; i < pivot.childCount; i++)
        {
            children.Add(pivot.GetChild(i));
        }

        foreach (Transform child in children)
        {
            child.SetParent(obstacle, true);
        }

        Object.DestroyImmediate(pivot.gameObject);
    }

    private static void AddStableRotation(Transform obstacle, Vector3 axis, float speed)
    {
        if (!TryCalculateBounds(obstacle, out Bounds bounds))
        {
            return;
        }

        GameObject pivotObject = new GameObject("StableSpinPivot");
        Transform pivot = pivotObject.transform;
        pivot.SetParent(obstacle, false);
        pivot.position = bounds.center;
        pivot.rotation = obstacle.rotation;
        pivot.localScale = Vector3.one;

        List<Transform> visualRoots = new List<Transform>();
        for (int i = 0; i < obstacle.childCount; i++)
        {
            Transform child = obstacle.GetChild(i);
            if (child == pivot || child.GetComponentInChildren<Renderer>(true) == null)
            {
                continue;
            }

            bool hasEnabledCollider = false;
            foreach (Collider collider in child.GetComponentsInChildren<Collider>(true))
            {
                if (collider.enabled)
                {
                    hasEnabledCollider = true;
                    break;
                }
            }

            if (!hasEnabledCollider)
            {
                visualRoots.Add(child);
            }
        }

        foreach (Transform visual in visualRoots)
        {
            visual.SetParent(pivot, true);
        }

        MeshFilter rootFilter = obstacle.GetComponent<MeshFilter>();
        MeshRenderer rootRenderer = obstacle.GetComponent<MeshRenderer>();
        if (rootFilter != null && rootRenderer != null && rootRenderer.enabled && rootFilter.sharedMesh != null)
        {
            GameObject visualObject = new GameObject("RootMeshVisual", typeof(MeshFilter), typeof(MeshRenderer));
            Transform visual = visualObject.transform;
            visual.SetParent(pivot, false);
            visual.position = obstacle.position;
            visual.rotation = obstacle.rotation;
            visual.localScale = Vector3.one;

            visualObject.GetComponent<MeshFilter>().sharedMesh = rootFilter.sharedMesh;
            MeshRenderer copy = visualObject.GetComponent<MeshRenderer>();
            copy.sharedMaterials = rootRenderer.sharedMaterials;
            copy.shadowCastingMode = rootRenderer.shadowCastingMode;
            copy.receiveShadows = rootRenderer.receiveShadows;
            rootRenderer.enabled = false;
        }

        RotatingObstacle spinner = pivotObject.AddComponent<RotatingObstacle>();
        spinner.Configure(axis, speed, false);
        spinner.RecalculatePivot();
        EditorUtility.SetDirty(spinner);
    }

    private static void TuneColliderOnce(Transform obstacle)
    {
        if (obstacle.GetComponent<ObstacleV6Tuned>() != null)
        {
            return;
        }

        BoxCollider box = obstacle.GetComponent<BoxCollider>();
        if (box != null)
        {
            Vector3 size = box.size;
            size.x *= 0.82f;
            size.z *= 0.84f;
            box.size = size;
            EditorUtility.SetDirty(box);
        }

        SphereCollider sphere = obstacle.GetComponent<SphereCollider>();
        if (sphere != null)
        {
            sphere.radius *= 0.86f;
            EditorUtility.SetDirty(sphere);
        }

        CapsuleCollider capsule = obstacle.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.radius *= 0.86f;
            EditorUtility.SetDirty(capsule);
        }

        obstacle.gameObject.AddComponent<ObstacleV6Tuned>();
    }

    private static void PolishMenus(Canvas canvas)
    {
        GameObject mainMenu = FindSceneObject("MainMenuPanel");
        GameObject settings = FindSceneObject("SettingsPanel");

        if (mainMenu != null)
        {
            Transform old = mainMenu.transform.Find(MenuPolishName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            Image background = mainMenu.GetComponent<Image>();
            if (background != null)
            {
                background.color = Color.white;
                UiVerticalGradient gradient = mainMenu.GetComponent<UiVerticalGradient>();
                if (gradient == null)
                {
                    gradient = mainMenu.AddComponent<UiVerticalGradient>();
                }
                gradient.Configure(
                    new Color(0.03f, 0.34f, 0.52f, 1f),
                    new Color(0.008f, 0.022f, 0.075f, 1f));
            }

            GameObject polish = new GameObject(MenuPolishName, typeof(RectTransform));
            RectTransform polishRect = polish.GetComponent<RectTransform>();
            polishRect.SetParent(mainMenu.transform, false);
            Stretch(polishRect);
            polishRect.SetAsFirstSibling();

            CreateMenuCard(polishRect);
            CreateMenuBubbles(polishRect);
            RestyleMainMenu(mainMenu.transform);
        }

        if (settings != null)
        {
            Image background = settings.GetComponent<Image>();
            if (background != null)
            {
                background.color = Color.white;
                UiVerticalGradient gradient = settings.GetComponent<UiVerticalGradient>();
                if (gradient == null)
                {
                    gradient = settings.AddComponent<UiVerticalGradient>();
                }
                gradient.Configure(
                    new Color(0.04f, 0.25f, 0.42f, 1f),
                    new Color(0.008f, 0.018f, 0.06f, 1f));
            }

            RestyleSettings(settings.transform);
        }
    }

    private static void RestyleMainMenu(Transform menu)
    {
        TMP_Text title = FindChildComponent<TMP_Text>(menu, "MenuTitle");
        if (title != null)
        {
            title.text = "BALLOON\nDOG";
            title.fontSize = 118f;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.25f, 0.9f, 1f);
            title.alignment = TextAlignmentOptions.Center;
            SetOutlineIfAvailable(title, 0.16f, new Color32(0, 20, 45, 230));
            SetRect(title.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0f, 45f), new Vector2(760f, 300f));
            if (title.GetComponent<MenuTitlePulse>() == null)
            {
                title.gameObject.AddComponent<MenuTitlePulse>();
            }
        }

        TMP_Text subtitle = FindChildComponent<TMP_Text>(menu, "MenuSubtitle");
        if (subtitle != null)
        {
            subtitle.text = "HAVANI KORU  •  KAÇ  •  UÇ  •  FIRLAT";
            subtitle.fontSize = 28f;
            subtitle.color = new Color(0.82f, 0.94f, 1f);
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.60f), Vector2.zero, new Vector2(880f, 80f));
        }

        TMP_Text extraSubtitle = FindChildComponent<TMP_Text>(menu, "V5Subtitle");
        if (extraSubtitle != null)
        {
            extraSubtitle.gameObject.SetActive(false);
        }

        StyleButton(menu, "PlayButton", new Vector2(0f, -35f), new Vector2(610f, 150f), new Color(0.05f, 0.78f, 1f));
        StyleButton(menu, "SettingsButton", new Vector2(0f, -215f), new Vector2(610f, 125f), new Color(0.16f, 0.28f, 0.48f));
        StyleButton(menu, "ExitButton", new Vector2(0f, -365f), new Vector2(610f, 115f), new Color(0.46f, 0.15f, 0.22f));

        TMP_Text version = FindChildComponent<TMP_Text>(menu, "VersionText");
        if (version != null)
        {
            version.text = "PROTOTİP V6  •  MOBİL RUNNER";
            version.fontSize = 23f;
            version.color = new Color(0.48f, 0.66f, 0.8f);
            SetRect(version.rectTransform, new Vector2(0.5f, 0.055f), Vector2.zero, new Vector2(600f, 55f));
        }

        TMP_Text best = FindChildComponent<TMP_Text>(menu, "V6BestScore");
        if (best == null)
        {
            GameObject bestObject = new GameObject("V6BestScore", typeof(RectTransform), typeof(TextMeshProUGUI));
            bestObject.transform.SetParent(menu, false);
            best = bestObject.GetComponent<TMP_Text>();
            best.fontSize = 30f;
            best.alignment = TextAlignmentOptions.Center;
            best.color = new Color(1f, 0.78f, 0.2f);
            SetRect(best.rectTransform, new Vector2(0.5f, 0.49f), Vector2.zero, new Vector2(500f, 65f));
            HighScoreDisplay display = bestObject.AddComponent<HighScoreDisplay>();
            display.Configure(best);
        }
    }

    private static void RestyleSettings(Transform settings)
    {
        TMP_Text title = FindChildComponent<TMP_Text>(settings, "SettingsTitle");
        if (title != null)
        {
            title.fontSize = 92f;
            title.color = new Color(0.25f, 0.9f, 1f);
            SetOutlineIfAvailable(title, 0.14f, new Color32(0, 20, 45, 230));
            SetRect(title.rectTransform, new Vector2(0.5f, 0.76f), Vector2.zero, new Vector2(760f, 140f));
        }

        StyleButton(settings, "SoundToggleButton", new Vector2(0f, 80f), new Vector2(650f, 130f), new Color(0.08f, 0.54f, 0.72f));
        StyleButton(settings, "VibrationToggleButton", new Vector2(0f, -80f), new Vector2(650f, 130f), new Color(0.08f, 0.54f, 0.72f));
        StyleButton(settings, "SettingsBackButton", new Vector2(0f, -290f), new Vector2(520f, 115f), new Color(0.21f, 0.28f, 0.42f));
    }

    private static void CreateMenuCard(RectTransform parent)
    {
        GameObject card = new GameObject("MenuGlassCard", typeof(RectTransform), typeof(Image), typeof(Outline));
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetRect(rect, new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(820f, 1080f));

        Image image = card.GetComponent<Image>();
        image.color = new Color(0.015f, 0.04f, 0.11f, 0.72f);
        image.raycastTarget = false;

        Outline outline = card.GetComponent<Outline>();
        outline.effectColor = new Color(0.15f, 0.78f, 1f, 0.3f);
        outline.effectDistance = new Vector2(3f, -3f);
    }

    private static void CreateMenuBubbles(RectTransform parent)
    {
        Color[] colors =
        {
            new Color(0.1f, 0.82f, 1f, 0.18f),
            new Color(1f, 0.45f, 0.12f, 0.14f),
            new Color(0.45f, 0.85f, 1f, 0.12f)
        };

        for (int i = 0; i < 12; i++)
        {
            GameObject bubble = new GameObject($"MenuBubble_{i:00}", typeof(RectTransform), typeof(Image));
            RectTransform rect = bubble.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            float side = i % 2 == 0 ? -1f : 1f;
            float size = 70f + (i % 4) * 34f;
            SetRect(
                rect,
                new Vector2(0.5f, 0.5f),
                new Vector2(side * (360f + (i % 3) * 85f), -720f + i * 135f),
                Vector2.one * size);

            Image image = bubble.GetComponent<Image>();
            image.color = colors[i % colors.Length];
            image.raycastTarget = false;

            MenuFloatingBubble floating = bubble.AddComponent<MenuFloatingBubble>();
            floating.Configure(16f + (i % 4) * 8f, 8f + (i % 3) * 5f, 0.42f + i * 0.025f, i * 0.7f);
        }
    }

    private static void StyleButton(
        Transform parent,
        string name,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color)
    {
        Button button = FindChildComponent<Button>(parent, name);
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        SetRect(rect, new Vector2(0.5f, 0.36f), anchoredPosition, size);

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.fontSize = name == "PlayButton" ? 44f : 36f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
        }

        Outline outline = button.GetComponent<Outline>();
        if (outline == null)
        {
            outline = button.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0f, 0f, 0f, 0.42f);
        outline.effectDistance = new Vector2(0f, -7f);

        if (button.GetComponent<MenuPressScale>() == null)
        {
            button.gameObject.AddComponent<MenuPressScale>();
        }
    }

    private static void BuildWorldPolish(Transform levelRoot)
    {
        Transform old = levelRoot.Find(WorldPolishName);
        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        GameObject rootObject = new GameObject(WorldPolishName);
        Transform root = rootObject.transform;
        root.SetParent(levelRoot, false);

        Material cyan = GetOrCreateMaterial("BDV6_CyanGlow", new Color(0.08f, 0.75f, 1f));
        Material orange = GetOrCreateMaterial("BDV6_OrangeGlow", new Color(1f, 0.42f, 0.08f));
        Material dark = GetOrCreateMaterial("BDV6_Dark", new Color(0.035f, 0.07f, 0.12f));

        for (int i = 0; i < 14; i++)
        {
            float z = 12f + i * 13f;
            float side = i % 2 == 0 ? -1f : 1f;
            CreateCube(root, $"RoadsidePost_{i:00}", new Vector3(side * 5.15f, 0.8f, z), new Vector3(0.12f, 1.6f, 0.12f), dark);
            CreateSphere(root, $"RoadsideLight_{i:00}", new Vector3(side * 5.15f, 1.75f, z), Vector3.one * 0.28f, i % 3 == 0 ? orange : cyan);
        }
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

        float safeCenter = Mathf.Max(0f, halfWidth - bounds.extents.x);
        float targetCenter = Mathf.Clamp(bounds.center.x, -safeCenter, safeCenter);
        obstacle.position += Vector3.right * (targetCenter - bounds.center.x);
    }

    private static bool TryCalculateBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool initialized = false;
        bounds = new Bounds(root.position, Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return initialized;
    }

    private static GameObject FindSceneObject(string name)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform transform in transforms)
        {
            if (transform != null && transform.name == name && transform.gameObject.scene.IsValid())
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == name)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static T FindChildComponent<T>(Transform parent, string name) where T : Component
    {
        Transform found = FindChildRecursive(parent, name);
        return found != null ? found.GetComponent<T>() : null;
    }


    private static void SetOutlineIfAvailable(TMP_Text text, float width, Color32 color)
    {
        if (text == null)
        {
            return;
        }

        // TMP'nin outlineWidth özelliği, font materyali henüz oluşturulmadan
        // çağrıldığında Unity 6.5'te NullReferenceException üretebiliyor.
        // Materyale dokunmak yerine güvenli uGUI Outline bileşeni kullanılır.
        Outline outline = text.GetComponent<Outline>();
        if (outline == null)
        {
            outline = text.gameObject.AddComponent<Outline>();
        }

        float distance = Mathf.Clamp(width * 24f, 1f, 5f);
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
        outline.useGraphicAlpha = true;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Material GetOrCreateMaterial(string name, Color color)
    {
        string path = $"Assets/Materials/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        material.SetFloat("_Smoothness", 0.35f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(gameObject.GetComponent<Collider>());
        return gameObject;
    }

    private static GameObject CreateSphere(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(gameObject.GetComponent<Collider>());
        return gameObject;
    }
}
#endif
