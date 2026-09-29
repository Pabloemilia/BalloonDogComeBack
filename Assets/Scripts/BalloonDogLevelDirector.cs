using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the single hand-authored Balloon Dog course from the imported
/// obstacle pack. Every row keeps one readable escape lane.
/// </summary>
[DefaultExecutionOrder(-180)]
public sealed class BalloonDogLevelDirector : MonoBehaviour
{
    private const string RuntimeName = "__BalloonDogLevelDirector";
    private const string RuntimeLevelName = "BalloonDog_Level";
    private const string ModelRoot = "Models/ObstaclePack/";
    private const float RoadHalfWidth = 4.65f;
    private static readonly float[] Lanes = { -2.65f, 0f, 2.65f };
    private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

    private enum PackObstacle { Bomb, Cylinder, Gear, SpikeTrap, Spiral }

    private readonly struct CourseRow
    {
        public readonly float Z;
        public readonly int SafeLane;
        public readonly PackObstacle Left;
        public readonly PackObstacle Right;
        public CourseRow(float z, int safeLane, PackObstacle left, PackObstacle right)
        {
            Z = z; SafeLane = safeLane; Left = left; Right = right;
        }
    }

    private static readonly CourseRow[] Course =
    {
        new CourseRow(30f, 1, PackObstacle.Bomb,      PackObstacle.Cylinder),
        new CourseRow(45f, 2, PackObstacle.SpikeTrap, PackObstacle.Gear),
        new CourseRow(60f, 0, PackObstacle.Spiral,    PackObstacle.Bomb),
        new CourseRow(75f, 1, PackObstacle.Cylinder,  PackObstacle.SpikeTrap),
        new CourseRow(91f, 2, PackObstacle.Gear,      PackObstacle.Spiral),
        new CourseRow(107f,0, PackObstacle.Bomb,      PackObstacle.Cylinder),
        new CourseRow(123f,1, PackObstacle.SpikeTrap, PackObstacle.Gear),
        new CourseRow(139f,2, PackObstacle.Spiral,    PackObstacle.Bomb),
        new CourseRow(155f,0, PackObstacle.Cylinder,  PackObstacle.SpikeTrap),
        new CourseRow(171f,1, PackObstacle.Gear,      PackObstacle.Spiral)
    };

    public static int CurrentLevel => 1;
    public static float FinishZ { get; private set; } = 192f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeDirector()
    {
        if (GameObject.Find(RuntimeName) != null) return;
        new GameObject(RuntimeName).AddComponent<BalloonDogLevelDirector>();
    }

    public static void RebuildSelectedLevel()
    {
        BalloonDogLevelDirector director = FindAnyObjectByType<BalloonDogLevelDirector>();
        if (director != null) director.BuildSelectedLevel();
    }

    private void Awake() => BuildSelectedLevel();

    private void BuildSelectedLevel()
    {
        DisableBakedWorld();
        BalloonDogCampaign.SelectLevel(1);
        FinishZ = 192f;

        GameObject root = new GameObject(RuntimeLevelName);
        Transform environment = CreateGroup(root.transform, "Environment");
        Transform obstacles = CreateGroup(root.transform, "ObstaclePackCourse");
        Transform pickups = CreateGroup(root.transform, "AirPickups");
        Transform decorations = CreateGroup(root.transform, "Decorations");
        Transform finish = CreateGroup(root.transform, "Finish");

        Color theme = new Color(0.12f, 0.52f, 0.92f);
        BuildEnvironment(environment, decorations, theme);

        for (int rowIndex = 0; rowIndex < Course.Length; rowIndex++)
        {
            CourseRow row = Course[rowIndex];
            int firstBlocked = (row.SafeLane + 1) % 3;
            int secondBlocked = (row.SafeLane + 2) % 3;
            CreatePackObstacle(obstacles, row.Left, firstBlocked, row.Z, rowIndex);
            CreatePackObstacle(obstacles, row.Right, secondBlocked, row.Z + (rowIndex % 2 == 0 ? 1.8f : -1.8f), rowIndex + 1);
            CreateAirPickup(pickups, Lanes[row.SafeLane], row.Z - 4.5f, rowIndex % 3 == 2 ? 14f : 7f);
        }

        for (int lane = 0; lane < Lanes.Length; lane++)
            CreateAirPickup(pickups, Lanes[lane], FinishZ - 9f, 10f);

        BuildFinish(finish);
        ConfigurePlayer();
    }

    private static void CreatePackObstacle(Transform parent, PackObstacle type, int lane, float z, int variant)
    {
        GameObject root = new GameObject(type + "_Lane_" + lane);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(Lanes[lane], 0f, z);

        bool spikes = type == PackObstacle.SpikeTrap;
        BoxCollider hitbox = root.AddComponent<BoxCollider>();
        hitbox.isTrigger = spikes;
        hitbox.center = spikes ? new Vector3(0f, 0.32f, 0f) : new Vector3(0f, 0.9f, 0f);
        hitbox.size = spikes ? new Vector3(1.85f, 0.7f, 2.15f) : new Vector3(1.65f, 1.8f, 1.45f);

        if (spikes) root.AddComponent<GroundSpikes>();
        else root.AddComponent<Obstacle>().ConfigureNormal(9f, 0.58f, 0.48f);

        GameObject visual = LoadVisual(type, root.transform);
        FitVisualToLane(visual, type);

        if (type == PackObstacle.Gear || type == PackObstacle.Spiral)
            root.AddComponent<RotatingObstacle>().Configure(Vector3.up, 48f + variant * 3f, true);
        else if (type == PackObstacle.Bomb && variant % 2 == 1)
            root.AddComponent<MovingObstacle>().Configure(
                MovingObstacle.MotionMode.Vertical, 0.35f, 0.28f, variant * 0.13f, RoadHalfWidth);
        else if (type == PackObstacle.Cylinder && variant % 3 == 0)
            root.AddComponent<MovingObstacle>().Configure(
                MovingObstacle.MotionMode.ForwardBackward, 0.65f, 0.22f, variant * 0.11f, RoadHalfWidth);
    }

    private static GameObject LoadVisual(PackObstacle type, Transform parent)
    {
        string name = type.ToString();
        GameObject source = Resources.Load<GameObject>(ModelRoot + name + "/" + name);
        GameObject visual;
        if (source != null)
        {
            visual = Instantiate(source, parent, false);
            visual.name = name + "Visual";
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                Destroy(collider);
        }
        else
        {
            Debug.LogError("Obstacle model missing from Resources: " + ModelRoot + name + "/" + name);
            visual = GameObject.CreatePrimitive(type == PackObstacle.Cylinder ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            visual.name = name + "Fallback";
            visual.transform.SetParent(parent, false);
            Destroy(visual.GetComponent<Collider>());
            Renderer renderer = visual.GetComponent<Renderer>();
            renderer.sharedMaterial = GetMaterial("MissingObstacle", new Color(1f, 0.18f, 0.3f));
        }
        return visual;
    }

    private static void FitVisualToLane(GameObject visual, PackObstacle type)
    {
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        float targetWidth = type == PackObstacle.SpikeTrap ? 1.85f : 1.55f;
        float targetHeight = type == PackObstacle.SpikeTrap ? 0.55f : type == PackObstacle.Gear ? 1.55f : 1.75f;
        float width = Mathf.Max(bounds.size.x, bounds.size.z);
        float scale = Mathf.Min(targetWidth / Mathf.Max(0.01f, width), targetHeight / Mathf.Max(0.01f, bounds.size.y));
        visual.transform.localScale = Vector3.one * scale;

        bounds = visual.GetComponentsInChildren<Renderer>(true)[0].bounds;
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true)) bounds.Encapsulate(renderer.bounds);
        Vector3 correction = new Vector3(
            visual.transform.parent.position.x - bounds.center.x,
            visual.transform.parent.position.y - bounds.min.y,
            visual.transform.parent.position.z - bounds.center.z);
        visual.transform.position += correction;
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

    private static void BuildEnvironment(Transform parent, Transform decorations, Color theme)
    {
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "SingleCourseRoad";
        road.transform.SetParent(parent, false);
        road.transform.position = new Vector3(0f, -0.5f, FinishZ * 0.5f);
        road.transform.localScale = new Vector3(RoadHalfWidth * 2f, 1f, FinishZ + 35f);
        road.GetComponent<Renderer>().sharedMaterial = GetMaterial("Road", new Color(0.075f, 0.11f, 0.24f));

        for (int lane = 1; lane <= 2; lane++)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "LaneGuide";
            line.transform.SetParent(parent, false);
            line.transform.position = new Vector3(-RoadHalfWidth + lane * (RoadHalfWidth * 2f / 3f), 0.015f, FinishZ * 0.5f);
            line.transform.localScale = new Vector3(0.06f, 0.025f, FinishZ + 30f);
            line.GetComponent<Renderer>().sharedMaterial = GetMaterial("Lane", new Color(0.5f, 0.88f, 1f));
            Destroy(line.GetComponent<Collider>());
        }

        BuildSideGround(parent, -7.7f, theme);
        BuildSideGround(parent, 7.7f, theme);
        for (float z = 14f; z < FinishZ; z += 18f)
        {
            CreateMarker(decorations, -6.1f, z, theme);
            CreateMarker(decorations, 6.1f, z + 8f, theme);
        }
    }

    private static void BuildSideGround(Transform parent, float x, Color theme)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "SideGround";
        ground.transform.SetParent(parent, false);
        ground.transform.position = new Vector3(x, -0.7f, FinishZ * 0.5f);
        ground.transform.localScale = new Vector3(6f, 0.5f, FinishZ + 35f);
        ground.GetComponent<Renderer>().sharedMaterial = GetMaterial("Ground", Color.Lerp(theme, Color.black, 0.38f));
    }

    private static void CreateMarker(Transform parent, float x, float z, Color theme)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "CourseMarker";
        post.transform.SetParent(parent, false);
        post.transform.position = new Vector3(x, 0.8f, z);
        post.transform.localScale = new Vector3(0.18f, 0.8f, 0.18f);
        post.GetComponent<Renderer>().sharedMaterial = GetMaterial("MarkerPost", new Color(0.05f, 0.58f, 0.9f));
        Destroy(post.GetComponent<Collider>());

        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "CourseMarkerGlow";
        orb.transform.SetParent(parent, false);
        orb.transform.position = new Vector3(x, 1.75f, z);
        orb.transform.localScale = Vector3.one * 0.58f;
        orb.GetComponent<Renderer>().sharedMaterial = GetMaterial("MarkerGlow", Color.Lerp(theme, Color.white, 0.25f));
        Destroy(orb.GetComponent<Collider>());
    }

    private static void BuildFinish(Transform parent)
    {
        GameObject finish = new GameObject("CampaignFinishLine");
        finish.transform.SetParent(parent, false);
        finish.transform.position = new Vector3(0f, 0f, FinishZ);
        BoxCollider trigger = finish.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 1.5f, 0f);
        trigger.size = new Vector3(RoadHalfWidth * 2f, 3f, 1.2f);
        finish.AddComponent<BalloonDogCampaignFinish>();

        Material gold = GetMaterial("FinishGold", new Color(1f, 0.67f, 0.08f));
        CreateGatePart(finish.transform, new Vector3(-4.1f, 2.2f, 0f), new Vector3(0.35f, 4.4f, 0.35f), gold);
        CreateGatePart(finish.transform, new Vector3(4.1f, 2.2f, 0f), new Vector3(0.35f, 4.4f, 0.35f), gold);
        CreateGatePart(finish.transform, new Vector3(0f, 4.25f, 0f), new Vector3(8.5f, 0.42f, 0.42f), gold);
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

    private static void ConfigurePlayer()
    {
        PlayerRunner runner = FindAnyObjectByType<PlayerRunner>();
        if (runner == null) return;
        Vector3 position = runner.transform.position;
        position.x = 0f; position.z = 0f;
        runner.transform.position = position;
        Rigidbody body = runner.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = position;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        runner.ConfigureForwardSpeed(6f);
        runner.GetComponent<PlayerHorizontalController>()?.ConfigureControls(3.65f, 10f, 16f, 92f);
        runner.GetComponent<DifficultyDirector>()?.Configure(FinishZ, 1.1f);
    }

    private static void DisableBakedWorld()
    {
        string[] roots = { "BalloonDog_Level", "BalloonDog_V4_Polish", "BalloonDog_V5_MegaPolish", "BalloonDog_V6_Polish" };
        foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (candidate == null || !candidate.gameObject.scene.IsValid()) continue;
            foreach (string rootName in roots)
            {
                if (candidate.name != rootName) continue;
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
        if (Materials.TryGetValue(key, out Material existing) && existing != null) return existing;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader) { name = "BDSingleCourse_" + key, color = color };
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.28f);
        Materials[key] = material;
        return material;
    }
}

