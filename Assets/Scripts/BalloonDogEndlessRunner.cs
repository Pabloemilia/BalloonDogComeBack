using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A fixed-size ring of road chunks. Geometry and contacts are reused forever.</summary>
[DefaultExecutionOrder(100)]
public sealed class BalloonDogEndlessRunner : MonoBehaviour
{
    public const string TutorialKey = "BalloonDog.Endless.TutorialComplete.v1";
    public const int ChunkCount = 6;
    public const float ChunkLength = 56f;
    public const int RowsPerChunk = 5;
    public const float RowSpacing = ChunkLength / RowsPerChunk;
    public const float FirstObstacleDistance = 14f;
    private const float LateRunSpeedThreshold = 300f;
    private const float LateRunSpeed = 17f;

    [Header("Progressive Run Speed")]
    [SerializeField, Min(0f)] private float startingRunSpeed = 5f;
    [SerializeField, Min(0f)] private float maximumRunSpeed = 14.5f;
    [SerializeField, Min(.01f)] private float secondsToMaximumSpeed = 110f;
    private float activeRunSeconds;

    [Header("Obstacle Palette Cycle")]
    [SerializeField, Min(0f)] private float paletteCycleSpeed = 10f;
    [SerializeField, Min(1f)] private float secondsPerPalette = 20f;
    private float paletteRunSeconds;
    public int CurrentPalette { get; private set; } = 1;

    private sealed class PaletteBinding
    {
        public MeshFilter target;
        public Mesh[] meshes;
        public BalloonDogTrapHitbox hitbox;
        public void Apply(int palette)
        {
            target.sharedMesh = meshes[palette - 1];
            hitbox?.RefreshGeometry(target.sharedMesh);
        }
    }
    private readonly List<PaletteBinding> paletteBindings = new List<PaletteBinding>();
    private readonly Dictionary<int, Dictionary<string, Mesh[]>> paletteMeshes =
        new Dictionary<int, Dictionary<string, Mesh[]>>();

    private sealed class Chunk
    {
        public Transform root;
        public double start;
        public readonly BalloonDogEndlessContact[] traps = new BalloonDogEndlessContact[RowsPerChunk];
        public readonly BalloonDogEndlessContact[] companions = new BalloonDogEndlessContact[RowsPerChunk];
        public readonly BalloonDogEndlessContact[] air = new BalloonDogEndlessContact[RowsPerChunk];
    }
    private readonly List<Chunk> chunks = new List<Chunk>();
    private readonly List<BalloonDogEndlessContact> trapPool = new List<BalloonDogEndlessContact>();
    private readonly HashSet<BalloonDogEndlessContact> leasedTraps = new HashSet<BalloonDogEndlessContact>();
    private readonly Dictionary<BalloonDogEndlessContact, BalloonDogTrapPlacement> placements = new Dictionary<BalloonDogEndlessContact, BalloonDogTrapPlacement>();
    private readonly Dictionary<BalloonDogEndlessContact, Bounds> restBounds = new Dictionary<BalloonDogEndlessContact, Bounds>();
    private readonly Dictionary<BalloonDogEndlessContact, Bounds> leftRestBounds = new Dictionary<BalloonDogEndlessContact, Bounds>();
    private readonly Dictionary<BalloonDogEndlessContact, Bounds> sweptBounds = new Dictionary<BalloonDogEndlessContact, Bounds>();
    private Transform poolRoot;
    private int nextTrap;
    private readonly List<Material> ownedMaterials = new List<Material>();
    private PlayerRunner runner;
    private ScoreController score;
    private AirController air;
    private PlayerHorizontalController horizontal;
    private BalloonSizeController size;
    private PlayerFormController form;
    private System.Random random;
    private float width, floorY, roadCenter;
    private double origin;
    private double nextStart, endlessStartDistance;
    private int routeLane = 1, tutorialStep;
    private bool tutorial;
    private TMP_Text instruction;
    private Canvas tutorialCanvas;
    private float nextTutorialStepAt;
    public bool IsInitialized { get; private set; }
    public bool InitializationFailed { get; private set; }
    public float InitializationProgress { get; private set; }

    private IEnumerator Start()
    {
        // Let the startup cover render before preparing the reusable course.
        yield return null;
        runner = FindAnyObjectByType<PlayerRunner>();
        if (runner == null) { InitializationFailed = true; enabled = false; yield break; }
        score = runner.GetComponent<ScoreController>();
        air = runner.GetComponent<AirController>();
        horizontal = runner.GetComponent<PlayerHorizontalController>();
        size = runner.GetComponent<BalloonSizeController>();
        form = runner.GetComponent<PlayerFormController>();
        var difficulty = runner.GetComponent<DifficultyDirector>();
        if (difficulty != null) difficulty.enabled = false;
        foreach (var component in FindObjectsByType<ProgressDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            component.enabled = false;
        foreach (var banner in FindObjectsByType<SectionBannerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            banner.enabled = false;
            banner.Retire();
        }
        // The floor is copied with the same mesh, material, thickness and height.
        GameObject road = GameObject.Find("Road");
        if (road == null || road.GetComponent<MeshRenderer>() == null)
        {
            Debug.LogError("Endless requires the scene's Road mesh.", this);
            InitializationFailed = true; enabled = false; yield break;
        }
        Bounds bounds = road.GetComponent<Renderer>().bounds;
        width = bounds.size.x; floorY = bounds.max.y; roadCenter = bounds.center.x;
        Mesh mesh = road.GetComponent<MeshFilter>().sharedMesh;
        Material material = road.GetComponent<Renderer>().sharedMaterial;
        tutorial = PlayerPrefs.GetInt(TutorialKey, 0) == 0;
        random = new System.Random(Guid.NewGuid().GetHashCode());
        poolRoot=new GameObject("TrapPool").transform;
        poolRoot.SetParent(transform,false);
        int prepared = 0;
        int preparationCount = BalloonDogTrapPlacement.All.Length * 4 + ChunkCount;
        // All four color/model variants of each allowed trap are reusable.
        foreach(var profile in BalloonDogTrapPlacement.All)
            for(int variant=1;variant<=4;variant++)
            {
                var trap=CreateTrap(poolRoot,profile.id,variant);
                trap.ResetContact(false);trapPool.Add(trap);
                InitializationProgress = (float)++prepared / preparationCount;
                // Keep rendering actual completed work instead of freezing on
                // the expensive mesh/animation preparation for all 80 models.
                if(variant==4) yield return null;
            }
        for(int i=trapPool.Count-1;i>0;i--)
        {
            int other=random.Next(i+1);var value=trapPool[i];trapPool[i]=trapPool[other];trapPool[other]=value;
        }
        horizontal?.ConfigureRoadLimit(width * .5f - .6f);
        runner.SetDifficultyMultiplier(1f);
        activeRunSeconds = 0f;
        AdvanceRunSpeed(0f);
        nextStart = -ChunkLength;
        for (int i=0; i<ChunkCount; i++)
        {
            var c = new Chunk();
            c.root = new GameObject("EndlessRoad_" + i).transform;
            c.root.SetParent(transform, false);
            GameObject floor = new GameObject("SameGround", typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
            floor.transform.SetParent(c.root, false);
            floor.transform.localPosition = new Vector3(bounds.center.x, floorY - bounds.size.y*.5f, ChunkLength*.5f);
            floor.transform.localScale = new Vector3(width, bounds.size.y, ChunkLength);
            floor.GetComponent<MeshFilter>().sharedMesh = mesh;
            floor.GetComponent<MeshRenderer>().sharedMaterial = material;
            floor.GetComponent<BoxCollider>().size = Vector3.one;
            for(int row=0;row<RowsPerChunk;row++)
            {
                c.air[row] = CreateAir(c.root);
            }
            Place(c); chunks.Add(c);
            InitializationProgress = (float)++prepared / preparationCount;
            yield return null;
        }
        road.SetActive(false);
        if(tutorial) BuildTutorial();
        InitializationProgress = 1f;
        IsInitialized = true;
    }

    private void Place(Chunk c)
    {
        c.start = nextStart; nextStart += ChunkLength;
        c.root.localPosition = Vector3.forward * (float)(c.start-origin);
        Populate(c);
    }

    private void Populate(Chunk c)
    {
        // Wide mechanisms keep their authored position. Only measured narrow
        // traps may pair up, leaving a player-sized corridor.
        for(int row=0;row<RowsPerChunk;row++)
        {
            ReleaseTrap(c.traps[row]);ReleaseTrap(c.companions[row]);
            c.traps[row]=null;c.companions[row]=null;
        }
        for(int row=0;row<RowsPerChunk;row++)
        {
            double distance=c.start+FirstObstacleDistance+row*RowSpacing;
            var trap=TakeTrap();c.traps[row]=trap;
            var profile=placements[trap];
            trap.transform.SetParent(c.root,false);
            bool right=random.Next(2)==1;
            trap.transform.Find("AuthoredTransform").localRotation=Quaternion.Euler(0,profile.Yaw(right),0);
            Bounds reference=!right && profile.id==8 ? leftRestBounds[trap] : restBounds[trap];
            trap.transform.localPosition=profile.Position(roadCenter,floorY,
                FirstObstacleDistance+row*RowSpacing,width,reference,right);
            int clearLane=1;
            if(!tutorial && distance>=endlessStartDistance+56 && random.NextDouble()<.75
                && sweptBounds.ContainsKey(trap))
            {
                var companion=TakeCompanion(trap);
                if(companion!=null)
                {
                    c.companions[row]=companion;
                    companion.transform.SetParent(c.root,false);
                    var otherProfile=placements[companion];
                    // Change the route without demanding an instant edge-to-edge turn.
                    clearLane=routeLane==1 ? random.Next(2)*2 : 1;
                    Vector2 pair=BalloonDogTrapPlacement.PairX(sweptBounds[trap],sweptBounds[companion],roadCenter,width,clearLane,right);
                    companion.transform.localPosition=new Vector3(pair.y,
                        floorY+otherProfile.position.y,trap.transform.localPosition.z);
                    Vector3 primaryPosition=trap.transform.localPosition;
                    primaryPosition.x=pair.x;
                    trap.transform.localPosition=primaryPosition;
                    companion.ResetContact(true);
                }
            }
            trap.ResetContact(!tutorial && distance>=endlessStartDistance+FirstObstacleDistance);
            // Place the following pickup toward the other side of the road.
            routeLane=c.companions[row]!=null ? clearLane : trap.transform.localPosition.x>=roadCenter ? 0 : 2;
            var pickup=c.air[row];
            pickup.transform.localPosition=new Vector3(LaneX(routeLane),floorY+1,FirstObstacleDistance+row*RowSpacing+5);
            pickup.ResetContact(!tutorial && distance>=endlessStartDistance+FirstObstacleDistance && row==0);
        }
    }
    private void ReleaseTrap(BalloonDogEndlessContact trap)
    {
        if(trap==null) return;
        trap.ResetContact(false);leasedTraps.Remove(trap);
        trap.transform.SetParent(poolRoot,false);
    }
    private BalloonDogEndlessContact TakeCompanion(BalloonDogEndlessContact primary)
    {
        for(int offset=0;offset<trapPool.Count;offset++)
        {
            var candidate=trapPool[(nextTrap+offset)%trapPool.Count];
            if(leasedTraps.Contains(candidate) || !sweptBounds.TryGetValue(candidate,out Bounds other)) continue;
            if(!BalloonDogTrapPlacement.CanCombine(sweptBounds[primary],other,width)) continue;
            leasedTraps.Add(candidate);return candidate;
        }
        return null;
    }
    private BalloonDogEndlessContact TakeTrap()
    {
        for(int attempt=0;attempt<trapPool.Count;attempt++)
        {
            var trap=trapPool[nextTrap];nextTrap=(nextTrap+1)%trapPool.Count;
            if(leasedTraps.Add(trap)) return trap;
        }
        throw new InvalidOperationException("Endless trap pool is exhausted");
    }
    private float LaneX(int lane) => roadCenter+(lane-1)*width/3f;

    private void Update()
    {
        if(!IsInitialized || runner==null || chunks.Count==0) return;
        bool running=IsRunActive();
        if(tutorialCanvas!=null) tutorialCanvas.gameObject.SetActive(tutorial && running);
        if(!running) return;
        if(tutorial) AdvanceTutorial();
        AdvanceRunSpeed(Time.deltaTime);
        AdvancePalette(Time.deltaTime);
        runner.SetDifficultyMultiplier(1f);
    }

    private bool IsRunActive() => runner!=null && Time.timeScale>0 && runner.enabled
        && runner.MovementEnabled && horizontal!=null && horizontal.enabled
        && (GameManager.Instance==null || !GameManager.Instance.IsGameOver);

    private void FixedUpdate()
    {
        if(!IsInitialized || chunks.Count==0 || !IsRunActive()) return;
        Rigidbody body=runner.GetComponent<Rigidbody>();
        // Use the physical pose, never the interpolated render pose, to decide
        // which road is behind us. Move the world before the next physics step.
        if(body.position.z>1024f) ShiftOrigin(896f);
        double behind=origin+body.position.z-24d;
        bool recycled=false;
        while(true)
        {
            Chunk oldest=chunks[0];
            foreach(Chunk chunk in chunks) if(chunk.start<oldest.start) oldest=chunk;
            if(oldest.start+ChunkLength>=behind) break;
            Place(oldest);
            recycled=true;
        }
        if(recycled) Physics.SyncTransforms();
    }

    private void AdvanceRunSpeed(float deltaTime)
    {
        // Update calls this only during active play. Tutorial, menus, ads and
        // pauses do not build up speed; origin shifts and continues keep it.
        if (tutorial)
        {
            runner.ConfigureForwardSpeed(3.5f);
            return;
        }
        activeRunSeconds += Mathf.Max(0f, deltaTime);
        // Five minutes in the same run unlocks a direct second speed tier.
        float speed = activeRunSeconds >= LateRunSpeedThreshold
            ? LateRunSpeed
            : Mathf.Lerp(startingRunSpeed, Mathf.Max(startingRunSpeed, maximumRunSpeed),
                Mathf.Clamp01(activeRunSeconds / Mathf.Max(.01f, secondsToMaximumSpeed)));
        runner.ConfigureForwardSpeed(speed);
    }

    private void AdvancePalette(float deltaTime)
    {
        // Update only calls this during active play. Continue, pause and world
        // rebasing retain the same palette and do not advance its timer.
        if (tutorial || runner == null || runner.CurrentForwardSpeed < paletteCycleSpeed) return;
        paletteRunSeconds += Mathf.Max(0f, deltaTime);
        int next = 1 + Mathf.FloorToInt(paletteRunSeconds / Mathf.Max(1f, secondsPerPalette)) % 4;
        if (next == CurrentPalette) return;
        CurrentPalette = next;
        // Update visible AND pooled traps together, including companion traps.
        // Mesh swaps preserve transforms, animator phase and contact consumption.
        foreach (var binding in paletteBindings) binding.Apply(CurrentPalette);
    }

    private Dictionary<string, Mesh[]> GetPaletteMeshes(int artId)
    {
        if (paletteMeshes.TryGetValue(artId, out var parts)) return parts;
        parts = new Dictionary<string, Mesh[]>();
        for (int variant = 1; variant <= 4; variant++)
        {
            var prefab = Resources.Load<GameObject>(BalloonDogTrapPlacement.ResourcePath(artId, variant));
            if (prefab == null) throw new InvalidOperationException("Missing trap palette: " + artId + " V" + variant);
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                string path = PalettePartPath(filter.transform, prefab.transform);
                if (!parts.TryGetValue(path, out var meshes))
                {
                    meshes = new Mesh[4];
                    parts.Add(path, meshes);
                }
                meshes[variant - 1] = filter.sharedMesh;
            }
        }
        foreach (var pair in parts)
            foreach (var mesh in pair.Value)
                if (mesh == null) throw new InvalidOperationException("Incomplete trap palette: " + artId + "/" + pair.Key);
        paletteMeshes.Add(artId, parts);
        return parts;
    }

    private static string PalettePartPath(Transform part, Transform root)
    {
        string path = string.Empty;
        for (Transform current = part; current != root; current = current.parent)
            path = path.Length == 0 ? current.name : current.name + "/" + path;
        return path;
    }

    private void ShiftOrigin(float amount)
    {
        Rigidbody body=runner.GetComponent<Rigidbody>();
        Vector3 shift=Vector3.back*amount;
        Vector3 playerPosition=body.position+shift;
        RigidbodyInterpolation interpolation=body.interpolation;
        body.interpolation=RigidbodyInterpolation.None;

        // Kinematic trap bodies also own physics poses. Record their intended
        // translated transforms before moving their road parents.
        var trapBodies=new List<Rigidbody>();
        var trapPositions=new List<Vector3>();
        foreach(Chunk chunk in chunks)
            foreach(Rigidbody trap in chunk.root.GetComponentsInChildren<Rigidbody>(true))
            {
                trapBodies.Add(trap);
                trapPositions.Add(trap.transform.position+shift);
            }
        origin+=amount;
        foreach(Chunk chunk in chunks)
        {
            Vector3 position=chunk.root.position;
            position.z=(float)(chunk.start-origin);
            chunk.root.position=position;
        }
        for(int i=0;i<trapBodies.Count;i++)
        {
            trapBodies[i].position=trapPositions[i];
            trapBodies[i].transform.position=trapPositions[i];
        }
        body.position=playerPosition;
        runner.transform.position=playerPosition;
        score?.ShiftOrigin(amount);
        foreach(var camera in FindObjectsByType<RunnerCameraFollow>(FindObjectsSortMode.None))
            camera.ApplyWorldShift(runner.transform,shift);
        Physics.SyncTransforms();
        body.interpolation=interpolation;
    }

    private void BuildTutorial()
    {
        var go=new GameObject("EndlessTutorial",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        go.transform.SetParent(transform,false);
        tutorialCanvas=go.GetComponent<Canvas>(); tutorialCanvas.renderMode=RenderMode.ScreenSpaceOverlay; tutorialCanvas.sortingOrder=510;
        var scaler=go.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1080,1920); scaler.matchWidthOrHeight=.5f;
        var card=new GameObject("Instruction",typeof(RectTransform),typeof(Image)); card.transform.SetParent(go.transform,false);
        var rect=card.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=new Vector2(.5f,.7f); rect.sizeDelta=new Vector2(870,150);
        card.GetComponent<Image>().color=new Color(.035f,.14f,.29f,.94f); card.GetComponent<Image>().raycastTarget=false;
        var label=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI)); label.transform.SetParent(card.transform,false);
        var lr=label.GetComponent<RectTransform>(); lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=new Vector2(24,12);lr.offsetMax=new Vector2(-24,-12);
        instruction=label.GetComponent<TMP_Text>(); instruction.fontSize=35;instruction.alignment=TextAlignmentOptions.Center;instruction.color=new Color(.65f,1,.94f);instruction.raycastTarget=false;
        nextTutorialStepAt=Time.time+.5f;
    }
    private void AdvanceTutorial()
    {
        // No danger or air penalty while learning. Each instruction waits for
        // the actual action; elapsed time alone never completes a step.
        if(air!=null && air.NormalizedAir<.95f) air.ResetToFull();
        string[] prompts={"DRAG LEFT to move", "DRAG RIGHT to move", "TAP SHRINK to get smaller", "DOUBLE TAP to fly", "DOUBLE TAP again to land"};
        instruction.text=prompts[tutorialStep];
        if(Time.time<nextTutorialStepAt) return;
        bool done=tutorialStep==0 ? runner.transform.position.x < -width*.22f :
            tutorialStep==1 ? runner.transform.position.x > width*.22f :
            tutorialStep==2 ? size!=null && size.IsShrinkRequested :
            tutorialStep==3 ? form!=null && form.IsHelicopterActive :
            form!=null && !form.IsHelicopterActive && runner.transform.position.y<floorY+1.4f;
        if(!done) return;
        tutorialStep++;nextTutorialStepAt=Time.time+.5f;
        if(tutorialStep<5) return;
        tutorial=false;PlayerPrefs.SetInt(TutorialKey,1);PlayerPrefs.Save();air?.ResetToFull();
        endlessStartDistance=score!=null ? score.DistanceTravelled : origin+runner.transform.position.z;
        // Update individual rows, including the current chunk. Filtering by
        // chunk start used to leave up to a hundred metres empty after learning.
        var ahead=new List<Chunk>(chunks);ahead.Sort((a,b)=>a.start.CompareTo(b.start));
        routeLane=Mathf.Clamp(Mathf.RoundToInt(runner.transform.position.x/(width/3f))+1,0,2);
        foreach(var chunk in ahead) Populate(chunk);
        if(tutorialCanvas!=null) tutorialCanvas.gameObject.SetActive(false);
    }

    private BalloonDogEndlessContact CreateTrap(Transform parent,int artId,int variant)
    {
        var root=new GameObject("PooledTrap");root.transform.SetParent(parent,false);
        var contact=root.AddComponent<BalloonDogEndlessContact>();
        contact.ConfigureObstacle(artId);
        var ownerBody=root.AddComponent<Rigidbody>();
        ownerBody.isKinematic=true;ownerBody.useGravity=false;
        root.name="PooledTrap_"+artId+"_V"+variant;
        var profile=BalloonDogTrapPlacement.For(artId);
        GameObject prefab=Resources.Load<GameObject>(BalloonDogTrapPlacement.ResourcePath(artId,variant));
        if(prefab==null) throw new InvalidOperationException("Missing endless trap resource: "+artId+" V"+variant);
        // Unity TRS order: rotation * authored scale, on one parent outside the
        // animation root. No automatic fit, centering or ground snap is applied.
        Transform mount=new GameObject("AuthoredTransform").transform;
        mount.SetParent(root.transform,false);
        mount.localRotation=Quaternion.Euler(0,profile.yaw,0);
        mount.localScale=profile.scale;
        GameObject view=Instantiate(prefab,mount,false);
        view.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);
        view.transform.localScale=Vector3.one;
        Animator[] animators=view.GetComponentsInChildren<Animator>(true);
        foreach(var animator in animators)
        {
            animator.applyRootMotion=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.enabled=true;animator.Rebind();animator.Update(0);
        }
        // Keep the package's flame timer. Legacy smoke collision callbacks use
        // a different ground setup; the animation-phase controller below owns dust.
        foreach(var behaviour in view.GetComponentsInChildren<MonoBehaviour>(true))
            if(!(behaviour is FlameControl)) behaviour.enabled=false;
        foreach(var col in view.GetComponentsInChildren<Collider>(true)) col.enabled=false;
        foreach(var body in view.GetComponentsInChildren<Rigidbody>(true))
        { body.isKinematic=true;body.detectCollisions=false;Destroy(body); }
        placements.Add(contact,profile);
        restBounds.Add(contact,BalloonDogAnimatedTrap.ReferenceBounds(view,root.transform));
        if(artId==8)
        {
            mount.localRotation=Quaternion.Euler(0,profile.Yaw(false),0);
            leftRestBounds.Add(contact,BalloonDogAnimatedTrap.ReferenceBounds(view,root.transform));
            mount.localRotation=Quaternion.Euler(0,profile.Yaw(true),0);
        }
        if(artId==6 || artId==18 || artId==19 || artId==20)
            sweptBounds.Add(contact,BalloonDogAnimatedTrap.MeasureFootprint(view,root.transform,animators));
        foreach(var animator in animators)
        {
            animator.enabled=true;animator.Rebind();animator.Update(0);
            if(animator.runtimeAnimatorController!=null && animator.layerCount>0)
                animator.Play(0,0,(float)random.NextDouble());
            // The six-second saw clip completes one full out-and-back in two
            // seconds. Other obstacle animations retain their existing speed.
            animator.speed=artId==3 ? 3f : .8f;
            BalloonDogTrapImpactVfx.Install(animator);
        }
        // Contact boxes follow the actual animated parts, not an invisible solid
        // rectangle spanning the entire animation's empty space.
        var palettes = GetPaletteMeshes(artId);
        foreach(var mesh in view.GetComponentsInChildren<MeshFilter>(true))
        {
            if(mesh.sharedMesh==null || mesh.GetComponent<MeshRenderer>()==null) continue;
            BalloonDogTrapHitbox hitbox = null;
            if (profile.CanDamage(mesh.transform,view.transform))
            {
                var proxy=new GameObject("Damage_"+mesh.name);
                proxy.transform.SetParent(root.transform,false);
                hitbox = proxy.AddComponent<BalloonDogTrapHitbox>();
                hitbox.Configure(mesh,contact);
            }
            // This asset pack encodes V1-V4 colors in the mesh UVs, while all
            // variants share one atlas material. A material tint cannot select them.
            var binding = new PaletteBinding
            {
                target = mesh,
                meshes = palettes[PalettePartPath(mesh.transform, view.transform)],
                hitbox = hitbox
            };
            binding.Apply(CurrentPalette);
            paletteBindings.Add(binding);
        }
        // VFX particles are not meshes and need their own emission-aware contact.
        BalloonDogFlameHazard.Install(view);
        var motion=root.AddComponent<BalloonDogAnimatedTrap>();
        motion.Configure(animators,(float)random.NextDouble());
        return contact;
    }
    private BalloonDogEndlessContact CreateAir(Transform parent)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="PooledAir";go.transform.SetParent(parent,false);go.transform.localScale=Vector3.one*.45f;
        go.GetComponent<Collider>().isTrigger=true;
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));mat.color=new Color(.2f,1f,.8f);ownedMaterials.Add(mat);go.GetComponent<Renderer>().sharedMaterial=mat;
        var contact=go.AddComponent<BalloonDogEndlessContact>();contact.IsPickup=true;return contact;
    }
    private void OnDestroy() { foreach(var material in ownedMaterials) if(material!=null) Destroy(material); }
}
