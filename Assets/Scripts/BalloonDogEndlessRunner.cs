using System;
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
    public const float RowSpacing = 14f;
    public const float FirstObstacleDistance = 14f;
    // Bits are blocked lanes, left to right. Each pattern has four rows.
    public static readonly int[][] Patterns = {
        new[]{1,2,4,2}, new[]{4,2,1,2}, new[]{1,1,2,4}, new[]{4,4,2,1},
        new[]{2,1,2,4}, new[]{2,4,2,1}, new[]{1,4,1,4}, new[]{4,1,4,1},
        new[]{3,1,2,6}, new[]{6,4,2,3}, new[]{5,1,3,2}, new[]{5,4,6,2},
        new[]{3,5,6,4}, new[]{6,5,3,1}, new[]{1,3,5,2}, new[]{4,6,5,2},
        new[]{5,3,1,6}, new[]{5,6,4,3}
    };
    private sealed class Chunk
    {
        public Transform root;
        public double start;
        public readonly BalloonDogEndlessContact[,] traps = new BalloonDogEndlessContact[4,3];
        public readonly BalloonDogEndlessContact[] air = new BalloonDogEndlessContact[4];
    }
    private readonly List<Chunk> chunks = new List<Chunk>();
    private readonly Dictionary<int, Bounds> animationEnvelopes = new Dictionary<int, Bounds>();
    private readonly List<Material> ownedMaterials = new List<Material>();
    private PlayerRunner runner;
    private ScoreController score;
    private AirController air;
    private PlayerHorizontalController horizontal;
    private BalloonSizeController size;
    private PlayerFormController form;
    private System.Random random;
    private float width, floorY;
    private double origin;
    private double nextStart, endlessStartDistance;
    private int routeLane = 1, tutorialStep;
    private bool tutorial;
    private TMP_Text instruction;
    private Canvas tutorialCanvas;
    private float nextTutorialStepAt;
    private readonly int[] artOrder = new int[29];

    private void Start()
    {
        runner = FindAnyObjectByType<PlayerRunner>();
        if (runner == null) { enabled = false; return; }
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
            enabled = false; return;
        }
        Bounds bounds = road.GetComponent<Renderer>().bounds;
        width = bounds.size.x; floorY = bounds.max.y;
        Mesh mesh = road.GetComponent<MeshFilter>().sharedMesh;
        Material material = road.GetComponent<Renderer>().sharedMaterial;
        tutorial = PlayerPrefs.GetInt(TutorialKey, 0) == 0;
        random = new System.Random(Guid.NewGuid().GetHashCode());
        for (int i=0;i<artOrder.Length;i++) artOrder[i]=i+1;
        for (int i=artOrder.Length-1;i>0;i--)
        {
            int other=random.Next(i+1);int value=artOrder[i];artOrder[i]=artOrder[other];artOrder[other]=value;
        }
        horizontal?.ConfigureRoadLimit(width * .5f - .6f);
        runner.SetDifficultyMultiplier(1f);
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
            for(int row=0;row<4;row++)
            {
                for(int lane=0;lane<3;lane++) c.traps[row,lane] = CreateTrap(c.root, (i*12+row*3+lane)%artOrder.Length);
                c.air[row] = CreateAir(c.root);
            }
            Place(c); chunks.Add(c);
        }
        road.SetActive(false);
        if(tutorial) BuildTutorial();
    }

    // The chosen clear lane moves by at most one lane between rows, including
    // pattern and chunk boundaries. Early runs use only single-lane obstacles.
    public static int MakeSafeMask(int mask, int previousLane, out int safeLane)
    {
        mask &= 7;
        safeLane = Mathf.Clamp(previousLane, 0, 2);
        if ((mask & (1 << safeLane)) == 0) return mask;
        for(int lane=0;lane<3;lane++)
            if(Mathf.Abs(lane-safeLane)<=1 && (mask & (1<<lane))==0) { safeLane=lane; return mask; }
        return mask & ~(1 << safeLane);
    }

    private void Place(Chunk c)
    {
        c.start = nextStart; nextStart += ChunkLength;
        c.root.localPosition = Vector3.forward * (float)(c.start-origin);
        Populate(c);
    }

    private void Populate(Chunk c)
    {
        int[] pattern = Patterns[random.Next(c.start-endlessStartDistance<300 ? 8 : Patterns.Length)];
        for(int row=0;row<4;row++)
        {
            double distance = c.start + 14 + row*RowSpacing;
            int mask = MakeSafeMask(pattern[row], routeLane, out routeLane);
            bool safe = tutorial || distance < endlessStartDistance+FirstObstacleDistance;
            for(int lane=0;lane<3;lane++)
            {
                var trap=c.traps[row,lane];
                trap.transform.localPosition=new Vector3(LaneX(lane),floorY,14+row*RowSpacing);
                trap.ResetContact(!safe && (mask & (1<<lane)) != 0);
            }
            var pickup=c.air[row];
            pickup.transform.localPosition=new Vector3(LaneX(routeLane),floorY+1,14+row*RowSpacing+5);
            pickup.ResetContact(!tutorial && distance>=endlessStartDistance+FirstObstacleDistance && row%2==0);
        }
    }
    private float LaneX(int lane) => (lane-1)*width/3f;

    private void Update()
    {
        if(runner==null || chunks.Count==0) return;
        bool running=Time.timeScale>0 && runner.enabled && runner.MovementEnabled && horizontal!=null && horizontal.enabled
            && (GameManager.Instance==null || !GameManager.Instance.IsGameOver);
        if(tutorialCanvas!=null) tutorialCanvas.gameObject.SetActive(tutorial && running);
        if(!running) return;
        if(tutorial) AdvanceTutorial();
        double distance=score!=null ? score.DistanceTravelled : origin+runner.transform.position.z;
        runner.ConfigureForwardSpeed(tutorial ? 3.5f : 5f+4f*(1f-Mathf.Exp(-(float)Math.Max(0,distance-endlessStartDistance)/1500f)));
        runner.SetDifficultyMultiplier(1f);
        foreach(Chunk c in chunks)
            if(c.start+ChunkLength < origin+runner.transform.position.z-24) Place(c);
        if(runner.transform.position.z>1024) ShiftOrigin(896);
    }

    private void ShiftOrigin(float amount)
    {
        origin+=amount;
        foreach(Chunk c in chunks) c.root.position-=Vector3.forward*amount;
        Rigidbody body=runner.GetComponent<Rigidbody>();
        body.position-=Vector3.forward*amount;
        score?.ShiftOrigin(amount);
        foreach(var camera in FindObjectsByType<RunnerCameraFollow>(FindObjectsSortMode.None)) camera.SnapToTarget();
        foreach(var trail in runner.GetComponentsInChildren<TrailRenderer>()) trail.Clear();
        Physics.SyncTransforms();
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

    private BalloonDogEndlessContact CreateTrap(Transform parent,int artIndex)
    {
        var root=new GameObject("PooledTrap");root.transform.SetParent(parent,false);
        var contact=root.AddComponent<BalloonDogEndlessContact>();
        var ownerBody=root.AddComponent<Rigidbody>();
        ownerBody.isKinematic=true;ownerBody.useGravity=false;
        int artId=artOrder[artIndex];
        root.name="PooledTrap_"+artId;
        GameObject prefab=Resources.Load<GameObject>("Endless/Trap"+artId);
        if(prefab==null) throw new InvalidOperationException("Missing endless trap resource: "+artId);
        // Placement, fitting and the animated prefab each own a separate transform.
        // Clip curves can animate their root without overwriting the lane or scale.
        Transform mount=new GameObject("AnimationFit").transform;
        mount.SetParent(root.transform,false);
        GameObject view=Instantiate(prefab,mount,false);
        view.transform.localPosition=Vector3.zero;
        Animator[] animators=view.GetComponentsInChildren<Animator>(true);
        foreach(var animator in animators)
        {
            animator.applyRootMotion=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.enabled=true;animator.Rebind();animator.Update(0);
        }
        // Keep the package's flame timer and VFX; smoke contact scripts require
        // a different ground/collision setup and do not own gameplay damage here.
        foreach(var behaviour in view.GetComponentsInChildren<MonoBehaviour>(true))
            if(!(behaviour is FlameControl)) behaviour.enabled=false;
        foreach(var col in view.GetComponentsInChildren<Collider>(true)) col.enabled=false;
        foreach(var body in view.GetComponentsInChildren<Rigidbody>(true))
        { body.isKinematic=true;body.detectCollisions=false;Destroy(body); }
        Bounds envelope;
        if(!animationEnvelopes.TryGetValue(artId,out envelope))
        {
            envelope=BalloonDogAnimatedTrap.MeasureAnimation(view,mount,animators);
            animationEnvelopes.Add(artId,envelope);
        }
        Vector3 fittedScale=BalloonDogAnimatedTrap.FitScale(envelope);
        mount.localScale=fittedScale;
        mount.localPosition=-Vector3.Scale(new Vector3(envelope.center.x,envelope.min.y,envelope.center.z),fittedScale);
        foreach(var animator in animators)
        {
            animator.enabled=true;animator.Rebind();animator.Update(0);
            if(animator.runtimeAnimatorController!=null && animator.layerCount>0)
                animator.Play(0,0,(float)random.NextDouble());
            animator.speed=.8f;
        }
        // Contact boxes follow the actual animated parts, not an invisible solid
        // rectangle spanning the entire animation's empty space.
        foreach(var mesh in view.GetComponentsInChildren<MeshFilter>(true))
        {
            if(mesh.sharedMesh==null || mesh.GetComponent<MeshRenderer>()==null) continue;
            var box=mesh.gameObject.AddComponent<BoxCollider>();box.isTrigger=true;
            box.center=mesh.sharedMesh.bounds.center;
            box.size=mesh.sharedMesh.bounds.size*.88f;
        }
        var motion=root.AddComponent<BalloonDogAnimatedTrap>();
        motion.Configure(view,animators,(float)random.NextDouble());
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
