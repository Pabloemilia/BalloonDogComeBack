using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds all campaign levels from code. Layouts are deterministic and every
/// obstacle row leaves at least one full lane open, with visible pickup hints.
/// </summary>
[DefaultExecutionOrder(-180)]
public sealed class BalloonDogLevelDirector : MonoBehaviour
{
    private const string RuntimeName = "__BalloonDogLevelDirector";
    // Uses the baked root name so the legacy bootstrap sees an active release
    // level and never creates its obsolete prototype track.
    private const string RuntimeLevelName = "BalloonDog_Level";
    private const float RoadHalfWidth = 4.65f;

    private static readonly float[] Lanes = { -2.65f, 0f, 2.65f };
    private static readonly Color[] LevelColors =
    {
        new Color(0.32f, 0.10f, 0.78f), new Color(0.43f, 0.08f, 0.88f),
        new Color(0.16f, 0.35f, 0.88f), new Color(0.10f, 0.58f, 0.82f),
        new Color(0.95f, 0.28f, 0.08f), new Color(0.14f, 0.68f, 0.76f),
        new Color(0.64f, 0.10f, 0.76f), new Color(0.12f, 0.66f, 0.43f),
        new Color(0.72f, 0.08f, 0.82f), new Color(0.15f, 0.46f, 0.92f),
        new Color(0.88f, 0.20f, 0.16f), new Color(0.96f, 0.58f, 0.08f)
    };

    private static readonly Dictionary<string, Material> Materials =
        new Dictionary<string, Material>();

    public static int CurrentLevel => BalloonDogCampaign.CurrentLevel;
    public static float FinishZ { get; private set; } = 145f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeDirector()
    {
        if (GameObject.Find(RuntimeName) != null)
        {
            return;
        }

        GameObject runtime = new GameObject(RuntimeName);
        runtime.AddComponent<BalloonDogLevelDirector>();
    }

    public static void RebuildSelectedLevel()
    {
        BalloonDogLevelDirector director =
            FindAnyObjectByType<BalloonDogLevelDirector>();
        if (director != null)
        {
            director.BuildSelectedLevel();
        }
    }

    private void Awake()
    {
        BuildSelectedLevel();
    }

    private void BuildSelectedLevel()
    {
        DisableBakedWorld();

        int level = BalloonDogCampaign.CurrentLevel;
        int challengeCount = 8 + level;
        float spacing = Mathf.Lerp(14.5f, 12.5f, (level - 1f) / (BalloonDogCampaign.LevelCount - 1f));
        FinishZ = 28f + challengeCount * spacing + 18f;

        GameObject root = new GameObject(RuntimeLevelName);
        Transform environment = CreateGroup(root.transform, "Environment");
        Transform obstacles = CreateGroup(root.transform, "Obstacles");
        Transform pickups = CreateGroup(root.transform, "AirPickups");
        Transform decorations = CreateGroup(root.transform, "Decorations");
        Transform finish = CreateGroup(root.transform, "Finish");

        Color theme = LevelColors[level - 1];
        BuildEnvironment(environment, decorations, theme, FinishZ);

        System.Random random = new System.Random(20260807 + level * 997);
        int previousSafeLane = 1;
        for (int index = 0; index < challengeCount; index++)
        {
            float z = 28f + index * spacing;
            int safeLane = SelectSafeLane(random, previousSafeLane, index);
            previousSafeLane = safeLane;
            int difficultyBand = Mathf.Clamp((level - 1) / 3, 0, 3);
            int pattern = SelectPattern(random, level, index);
            BuildChallenge(obstacles, pickups, z, safeLane, pattern, difficultyBand, theme, index);

            if (index % 3 == 2)
            {
                CreateAirPickup(pickups, Lanes[safeLane], z + spacing * 0.48f, 14f + difficultyBand * 2f);
            }
        }

        CreateAirPickup(pickups, Lanes[0], FinishZ - 10f, 10f);
        CreateAirPickup(pickups, Lanes[1], FinishZ - 10f, 10f);
        CreateAirPickup(pickups, Lanes[2], FinishZ - 10f, 10f);
        BuildFinish(finish, FinishZ, theme);
        ConfigurePlayerForLevel(level, FinishZ);
    }

    private static int SelectSafeLane(System.Random random, int previous, int index)
    {
        int lane = random.Next(0, Lanes.Length);
        if (index > 0 && lane == previous)
        {
            lane = (lane + (random.Next(0, 2) == 0 ? 1 : 2)) % Lanes.Length;
        }
        return lane;
    }

    private static int SelectPattern(System.Random random, int level, int index)
    {
        int maximumPattern = level < 3 ? 1 : level < 5 ? 2 : level < 8 ? 3 : 4;
        if (index == 0)
        {
            return 0;
        }
        return random.Next(0, maximumPattern + 1);
    }

    private static void BuildChallenge(
        Transform obstacles,
        Transform pickups,
        float z,
        int safeLane,
        int pattern,
        int difficultyBand,
        Color theme,
        int index)
    {
        float damage = 7f + difficultyBand * 1.5f;
        CreateAirPickup(pickups, Lanes[safeLane], z - 4.2f, 5f);

        switch (pattern)
        {
            case 0:
            {
                int blocked = (safeLane + 1 + index % 2) % 3;
                CreateStaticObstacle(obstacles, Lanes[blocked], z, damage, theme, "Single");
                break;
            }
            case 1:
                for (int lane = 0; lane < Lanes.Length; lane++)
                {
                    if (lane != safeLane)
                    {
                        CreateStaticObstacle(obstacles, Lanes[lane], z, damage, theme, "Double");
                    }
                }
                break;
            case 2:
            {
                int blocked = (safeLane + 1) % 3;
                GameObject moving = CreateStaticObstacle(obstacles, Lanes[blocked], z, damage, theme, "Moving");
                moving.AddComponent<MovingObstacle>().Configure(
                    MovingObstacle.MotionMode.Horizontal,
                    0.55f + difficultyBand * 0.08f,
                    0.22f + difficultyBand * 0.03f,
                    index * 0.17f,
                    RoadHalfWidth - 0.8f);
                break;
            }
            case 3:
            {
                int blocked = (safeLane + 2) % 3;
                GameObject rotating = CreateStaticObstacle(obstacles, Lanes[blocked], z, damage, theme, "Rotating");
                rotating.transform.localScale = new Vector3(1.65f, 0.82f, 0.72f);
                rotating.AddComponent<RotatingObstacle>().Configure(
                    Vector3.up,
                    38f + difficultyBand * 7f,
                    true);
                break;
            }
            default:
            {
                int blocked = (safeLane + 1) % 3;
                CreateSpikeObstacle(obstacles, Lanes[blocked], z, theme);
                int secondBlocked = (safeLane + 2) % 3;
                CreateStaticObstacle(obstacles, Lanes[secondBlocked], z + 1.6f, damage, theme, "Mixed");
                break;
            }
        }
    }

    private static GameObject CreateStaticObstacle(
        Transform parent,
        float x,
        float z,
        float damage,
        Color theme,
        string kind)
    {
        GameObject root = new GameObject(kind + "Obstacle");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(x, 0f, z);

        BoxCollider hitbox = root.AddComponent<BoxCollider>();
        hitbox.center = new Vector3(0f, 0.85f, 0f);
        hitbox.size = new Vector3(1.55f, 1.7f, 1.15f);
        root.AddComponent<Obstacle>().ConfigureNormal(damage, 0.62f, 0.42f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "BalloonObstacleVisual";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        body.transform.localScale = new Vector3(1.35f, 1.45f, 1.05f);
        body.GetComponent<Renderer>().sharedMaterial = GetMaterial("Obstacle_" + ColorUtility.ToHtmlStringRGB(theme), Color.Lerp(theme, Color.white, 0.18f));
        Destroy(body.GetComponent<Collider>());

        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
        band.name = "WarningBand";
        band.transform.SetParent(root.transform, false);
        band.transform.localPosition = new Vector3(0f, 0.85f, -0.55f);
        band.transform.localScale = new Vector3(1.0f, 0.22f, 0.08f);
        band.GetComponent<Renderer>().sharedMaterial = GetMaterial("Warning", new Color(1f, 0.66f, 0.05f));
        Destroy(band.GetComponent<Collider>());
        return root;
    }

    private static void CreateSpikeObstacle(Transform parent, float x, float z, Color theme)
    {
        GameObject root = new GameObject("LaneSpikes");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(x, 0f, z);
        BoxCollider trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.35f, 0f);
        trigger.size = new Vector3(1.65f, 0.75f, 2.1f);
        root.AddComponent<GroundSpikes>();

        for (int index = -1; index <= 1; index++)
        {
            GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spike.name = "SoftSpikeVisual";
            spike.transform.SetParent(root.transform, false);
            spike.transform.localPosition = new Vector3(index * 0.48f, 0.28f, 0f);
            spike.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            spike.transform.localScale = new Vector3(0.28f, 0.72f, 1.5f);
            spike.GetComponent<Renderer>().sharedMaterial = GetMaterial("Spike", Color.Lerp(theme, Color.red, 0.55f));
            Destroy(spike.GetComponent<Collider>());
        }
    }

    private static void CreateAirPickup(Transform parent, float x, float z, float amount)
    {
        GameObject root = new GameObject("AirPickup");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(x, 1.15f, z);
        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.62f;
        root.AddComponent<AirPickup>().Configure(amount);
        root.AddComponent<AmbientFloat>();

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "AirBalloonVisual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = new Vector3(0.62f, 0.78f, 0.62f);
        visual.GetComponent<Renderer>().sharedMaterial = GetMaterial("Air", new Color(0.08f, 0.84f, 1f));
        Destroy(visual.GetComponent<Collider>());
    }

    private static void BuildEnvironment(Transform parent, Transform decorations, Color theme, float finishZ)
    {
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "CampaignRoad";
        road.transform.SetParent(parent, false);
        road.transform.position = new Vector3(0f, -0.5f, finishZ * 0.5f);
        road.transform.localScale = new Vector3(RoadHalfWidth * 2f, 1f, finishZ + 35f);
        road.GetComponent<Renderer>().sharedMaterial = GetMaterial("Road", new Color(0.12f, 0.08f, 0.24f));

        for (int lane = 1; lane <= 2; lane++)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "LaneGuide";
            line.transform.SetParent(parent, false);
            line.transform.position = new Vector3(-RoadHalfWidth + lane * (RoadHalfWidth * 2f / 3f), 0.015f, finishZ * 0.5f);
            line.transform.localScale = new Vector3(0.055f, 0.025f, finishZ + 30f);
            line.GetComponent<Renderer>().sharedMaterial = GetMaterial("Lane", new Color(1f, 1f, 1f, 0.22f));
            Destroy(line.GetComponent<Collider>());
        }

        BuildSideGround(parent, -7.7f, finishZ, theme);
        BuildSideGround(parent, 7.7f, finishZ, theme);

        for (float z = 12f; z < finishZ; z += 16f)
        {
            CreateTree(decorations, -6.2f, z, theme);
            CreateTree(decorations, 6.2f, z + 7f, theme);
        }
    }

    private static void BuildSideGround(Transform parent, float x, float finishZ, Color theme)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "SideGround";
        ground.transform.SetParent(parent, false);
        ground.transform.position = new Vector3(x, -0.7f, finishZ * 0.5f);
        ground.transform.localScale = new Vector3(6f, 0.5f, finishZ + 35f);
        ground.GetComponent<Renderer>().sharedMaterial = GetMaterial("Ground_" + ColorUtility.ToHtmlStringRGB(theme), Color.Lerp(theme, Color.black, 0.28f));
    }

    private static void CreateTree(Transform parent, float x, float z, Color theme)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "RoadsideTree";
        trunk.transform.SetParent(parent, false);
        trunk.transform.position = new Vector3(x, 0.55f, z);
        trunk.transform.localScale = new Vector3(0.22f, 0.75f, 0.22f);
        trunk.GetComponent<Renderer>().sharedMaterial = GetMaterial("Trunk", new Color(0.35f, 0.16f, 0.08f));
        Destroy(trunk.GetComponent<Collider>());

        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = "TreeCrown";
        crown.transform.SetParent(parent, false);
        crown.transform.position = new Vector3(x, 2.05f, z);
        crown.transform.localScale = new Vector3(1.25f, 1.55f, 1.25f);
        crown.GetComponent<Renderer>().sharedMaterial = GetMaterial("Tree_" + ColorUtility.ToHtmlStringRGB(theme), Color.Lerp(theme, Color.green, 0.42f));
        Destroy(crown.GetComponent<Collider>());
    }

    private static void BuildFinish(Transform parent, float z, Color theme)
    {
        GameObject finish = new GameObject("CampaignFinishLine");
        finish.transform.SetParent(parent, false);
        finish.transform.position = new Vector3(0f, 0f, z);
        BoxCollider trigger = finish.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 1.5f, 0f);
        trigger.size = new Vector3(RoadHalfWidth * 2f, 3f, 1.2f);
        finish.AddComponent<BalloonDogCampaignFinish>();

        Material gold = GetMaterial("FinishGold", new Color(1f, 0.67f, 0.08f));
        CreateGatePart(finish.transform, new Vector3(-4.1f, 2.2f, 0f), new Vector3(0.35f, 4.4f, 0.35f), gold);
        CreateGatePart(finish.transform, new Vector3(4.1f, 2.2f, 0f), new Vector3(0.35f, 4.4f, 0.35f), gold);
        CreateGatePart(finish.transform, new Vector3(0f, 4.25f, 0f), new Vector3(8.5f, 0.42f, 0.42f), gold);

        for (int index = 0; index < 9; index++)
        {
            Color tileColor = index % 2 == 0 ? Color.white : Color.black;
            CreateGatePart(
                finish.transform,
                new Vector3(-4f + index, 0.025f, -0.15f),
                new Vector3(1f, 0.05f, 1.6f),
                GetMaterial(index % 2 == 0 ? "FinishWhite" : "FinishBlack", tileColor));
        }
    }

    private static void CreateGatePart(Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = "FinishVisual";
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(part.GetComponent<Collider>());
    }

    private static void ConfigurePlayerForLevel(int level, float finishZ)
    {
        PlayerRunner runner = FindAnyObjectByType<PlayerRunner>();
        if (runner == null)
        {
            return;
        }

        Vector3 playerPosition = runner.transform.position;
        playerPosition.x = 0f;
        playerPosition.z = 0f;
        runner.transform.position = playerPosition;
        Rigidbody body = runner.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = playerPosition;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        runner.ConfigureForwardSpeed(5.8f + (level - 1) * 0.08f);
        runner.GetComponent<PlayerHorizontalController>()?.ConfigureControls(3.65f, 10f, 16f, 92f);
        DifficultyDirector difficulty = runner.GetComponent<DifficultyDirector>();
        difficulty?.Configure(finishZ, 1.10f + (level - 1) * 0.012f);

    }

    private static void DisableBakedWorld()
    {
        string[] roots =
        {
            "BalloonDog_Level", "BalloonDog_V4_Polish", "BalloonDog_V5_MegaPolish",
            "BalloonDog_V6_Polish", RuntimeLevelName
        };

        foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (candidate == null || !candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            foreach (string rootName in roots)
            {
                if (candidate.name != rootName)
                {
                    continue;
                }

                candidate.gameObject.SetActive(false);
                Destroy(candidate.gameObject);
                break;
            }
        }
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static Material GetMaterial(string key, Color color)
    {
        if (Materials.TryGetValue(key, out Material existing) && existing != null)
        {
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader);
        material.name = "BDCampaign_" + key;
        material.color = color;
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.22f);
        }
        Materials[key] = material;
        return material;
    }
}
