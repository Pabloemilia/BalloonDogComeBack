using System;
using System.Reflection;
using System.Collections;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using NUnit.Framework;
using UnityEngine;

public sealed class BalloonDogEndlessTests
{
    private static Type Runtime(string name) => Type.GetType(name+", Assembly-CSharp", true);

    [Test]
    public void RunSpeedBuildsGraduallyAndStepsUpAtFiveActiveMinutesAcrossOriginShifts()
    {
        var player = new GameObject("ProgressiveSpeedPlayer");
        var host = new GameObject("ProgressiveSpeedTest");
        try
        {
            Component runner = player.AddComponent(Runtime("PlayerRunner"));
            Component endless = host.AddComponent(Runtime("BalloonDogEndlessRunner"));
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            endless.GetType().GetField("runner", flags).SetValue(endless, runner);
            MethodInfo advance = endless.GetType().GetMethod("AdvanceRunSpeed", flags);
            PropertyInfo speed = runner.GetType().GetProperty("CurrentForwardSpeed");
            advance.Invoke(endless, new object[] { 0f });
            Assert.That((float)speed.GetValue(runner), Is.EqualTo(5f));
            advance.Invoke(endless, new object[] { 55f });
            Assert.That((float)speed.GetValue(runner), Is.EqualTo(9.75f).Within(.001f));
            player.transform.position = Vector3.forward * 1200f;
            player.transform.position -= Vector3.forward * 896f;
            advance.Invoke(endless, new object[] { 0f });
            Assert.That((float)speed.GetValue(runner), Is.EqualTo(9.75f).Within(.001f));
            advance.Invoke(endless, new object[] { 54f });
            Assert.That((float)speed.GetValue(runner), Is.LessThan(14.5f));
            advance.Invoke(endless, new object[] { 1f });
            Assert.That((float)speed.GetValue(runner), Is.EqualTo(14.5f).Within(.001f));
            advance.Invoke(endless, new object[] { 189f });
            Assert.That((float)speed.GetValue(runner), Is.EqualTo(14.5f).Within(.001f));
            advance.Invoke(endless, new object[] { 1f });
            Assert.That((float)speed.GetValue(runner), Is.EqualTo(17f).Within(.001f));
            advance.Invoke(endless, new object[] { 3600f });
            Assert.That((float)speed.GetValue(runner), Is.EqualTo(17f).Within(.001f));
        }
        finally { UnityEngine.Object.DestroyImmediate(host); UnityEngine.Object.DestroyImmediate(player); }
    }

    [Test]
    public void RepeatedOriginShiftsKeepThePlayerOnTheRoadAndPreserveCameraAndScore()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var player = new GameObject("RebasePlayer", typeof(Rigidbody));
        var host = new GameObject("RebaseRoadTest");
        var cameraObject = new GameObject("RebaseCamera");
        bool autoSync = Physics.autoSyncTransforms;
        try
        {
            Physics.autoSyncTransforms = false;
            Component runner = player.AddComponent(Runtime("PlayerRunner"));
            Component score = player.AddComponent(Runtime("ScoreController"));
            score.GetType().GetMethod("Awake", flags).Invoke(score, null);
            Component endless = host.AddComponent(Runtime("BalloonDogEndlessRunner"));
            Type type = endless.GetType();
            type.GetField("runner", flags).SetValue(endless, runner);
            type.GetField("score", flags).SetValue(endless, score);
            var chunks = (IList)type.GetField("chunks", flags).GetValue(endless);
            Type chunkType = type.GetNestedType("Chunk", BindingFlags.NonPublic);
            var floors = new System.Collections.Generic.List<Collider>();
            for (int i = 0; i < 6; i++)
            {
                object chunk = Activator.CreateInstance(chunkType, true);
                var road = new GameObject("RebaseRoad_" + i);
                road.transform.SetParent(host.transform, false);
                double start = 1904d + i * 56d;
                road.transform.position = Vector3.forward * (float)start;
                chunkType.GetField("root").SetValue(chunk, road.transform);
                chunkType.GetField("start").SetValue(chunk, start);
                chunks.Add(chunk);
                var floor = new GameObject("SameGround", typeof(BoxCollider));
                floor.transform.SetParent(road.transform, false);
                floor.transform.localPosition = new Vector3(0f, -.5f, 28f);
                floor.GetComponent<BoxCollider>().size = new Vector3(6f, 1f, 56f);
                floors.Add(floor.GetComponent<Collider>());
            }
            var trap = new GameObject("KinematicTrap", typeof(Rigidbody), typeof(BoxCollider));
            trap.transform.SetParent(((Transform)chunkType.GetField("root").GetValue(chunks[0])), false);
            trap.transform.localPosition = new Vector3(2f, 1f, 14f);
            Rigidbody trapBody = trap.GetComponent<Rigidbody>();
            trapBody.isKinematic = true;
            Rigidbody body = player.GetComponent<Rigidbody>();
            body.useGravity = false;
            body.position = player.transform.position = new Vector3(0f, 1f, 1930f);
            body.linearVelocity = Vector3.forward * 8f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            Component follow = cameraObject.AddComponent(Runtime("RunnerCameraFollow"));
            follow.GetType().GetMethod("Configure").Invoke(follow, new object[] { player.transform });
            Physics.SyncTransforms();
            Vector3 cameraOffset = cameraObject.transform.position - player.transform.position;
            Vector3 trapOffset = trapBody.position - body.position;
            MethodInfo shift = type.GetMethod("ShiftOrigin", flags);
            for (int iteration = 0; iteration < 2; iteration++)
            {
                shift.Invoke(endless, new object[] { 896f });
                Assert.That(Vector3.Distance(body.position, player.transform.position), Is.LessThan(.001f));
                Assert.That(body.position.y, Is.EqualTo(1f).Within(.001f));
                Assert.That(body.interpolation, Is.EqualTo(RigidbodyInterpolation.Interpolate));
                Assert.That(body.linearVelocity.z, Is.EqualTo(8f).Within(.001f));
                Assert.That(Vector3.Distance(cameraObject.transform.position - player.transform.position, cameraOffset), Is.LessThan(.001f));
                Assert.That(Vector3.Distance(trapBody.position - body.position, trapOffset), Is.LessThan(.001f));
                Assert.That((double)score.GetType().GetProperty("DistanceTravelled").GetValue(score), Is.EqualTo(1930d).Within(.001d));
                bool supported = false;
                foreach (RaycastHit hit in Physics.RaycastAll(body.position, Vector3.down, 3f))
                    if (floors.Contains(hit.collider)) supported = true;
                Assert.That(supported, Is.True, "Road collision must remain directly under the player after every shift.");
            }
        }
        finally
        {
            Physics.autoSyncTransforms = autoSync;
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void TutorialTimeDoesNotMakeTheFirstRealObstaclesArriveAtHighSpeed()
    {
        var player = new GameObject("TutorialSpeedPlayer");
        var host = new GameObject("TutorialSpeedTest");
        try
        {
            Component runner = player.AddComponent(Runtime("PlayerRunner"));
            Component endless = host.AddComponent(Runtime("BalloonDogEndlessRunner"));
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            endless.GetType().GetField("runner", flags).SetValue(endless, runner);
            FieldInfo tutorial = endless.GetType().GetField("tutorial", flags);
            MethodInfo advance = endless.GetType().GetMethod("AdvanceRunSpeed", flags);
            PropertyInfo speed = runner.GetType().GetProperty("CurrentForwardSpeed");
            tutorial.SetValue(endless, true);
            advance.Invoke(endless, new object[] { 300f });
            Assert.That((float)speed.GetValue(runner), Is.EqualTo(3.5f));
            tutorial.SetValue(endless, false);
            advance.Invoke(endless, new object[] { 0f });
            Assert.That((float)speed.GetValue(runner), Is.EqualTo(5f));
        }
        finally { UnityEngine.Object.DestroyImmediate(host); UnityEngine.Object.DestroyImmediate(player); }
    }

    [Test]
    public void OriginShiftPreservesDistanceAndScore()
    {
        var go=new GameObject("EndlessScoreTest");
        try
        {
            Type type=Runtime("ScoreController");Component score=go.AddComponent(type);
            // Awake is not guaranteed in edit-mode tests.
            type.GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(score,null);
            go.transform.position=Vector3.forward*1200;
            int before=(int)type.GetProperty("CurrentScore").GetValue(score);
            go.transform.position-=Vector3.forward*896;
            type.GetMethod("ShiftOrigin").Invoke(score,new object[]{896f});
            Assert.That((int)type.GetProperty("CurrentScore").GetValue(score),Is.EqualTo(before));
            go.transform.position+=Vector3.forward*12;
            Assert.That((double)type.GetProperty("DistanceTravelled").GetValue(score),Is.EqualTo(1212d));
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
    [Test]
    public void RequiredTrapResourcesExistAndReuseContactResetsItsState()
    {
        foreach(int id in AllowedIds) for(int variant=1;variant<=4;variant++)
            Assert.That(Resources.Load<GameObject>("Endless/Trap"+id+(variant==1 ? "" : "V"+variant)),Is.Not.Null);
        var go=new GameObject("EndlessPoolTest");
        try
        {
            Type type=Runtime("BalloonDogEndlessContact");Component contact=go.AddComponent(type);
            FieldInfo state=type.GetField("consumed",BindingFlags.NonPublic|BindingFlags.Instance);
            state.SetValue(contact,true);
            type.GetMethod("ResetContact").Invoke(contact,new object[]{true});
            Assert.That((bool)state.GetValue(contact),Is.False);Assert.That(go.activeSelf,Is.True);
            type.GetMethod("ResetContact").Invoke(contact,new object[]{false});Assert.That(go.activeSelf,Is.False);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
    private static readonly int[] AllowedIds={1,2,4,5,6,7,8,10,11,13,14,16,17,18,19,20,21,22,25,28};
    [Test]
    public void CatalogContainsOnlyAllowedTrapsAndPreservesAuthoredTransforms()
    {
        Type type=Runtime("BalloonDogTrapPlacement");
        var all=(Array)type.GetProperty("All").GetValue(null);
        Assert.That(all.Length,Is.EqualTo(AllowedIds.Length));
        var scales=new[]{new Vector3(3,3,3),Vector3.one,new Vector3(3,3,3),new Vector3(3,3,3),Vector3.one,
            new Vector3(2.3f,1.6f,2),new Vector3(2,2,2),new Vector3(1.2f,1,1),Vector3.one,
            new Vector3(2,1.5f,2),Vector3.one,Vector3.one,Vector3.one,Vector3.one,Vector3.one,
            new Vector3(1.5f,1.3f,1.5f),new Vector3(1.7f,1.4f,1.4f),Vector3.one,new Vector3(2,1.3f,1),Vector3.one};
        float[] xs={0,-.18f,0,0,0,-.02f,0,0,0,0,0,0,0,0,0,0,0,2.792f,0,-2.762f};
        float[] ys={.074f,0,-1,-1.5f,-.011f,-.001f,0,0,0,0,0,-.317f,.022f,0,0,0,-.165f,-.178f,0,0};
        for(int index=0;index<AllowedIds.Length;index++)
        {
            int id=AllowedIds[index];object profile=type.GetMethod("For").Invoke(null,new object[]{id});
            Assert.That((Vector3)type.GetField("scale").GetValue(profile),Is.EqualTo(scales[index]),"scale "+id);
            Assert.That((Vector3)type.GetField("position").GetValue(profile),Is.EqualTo(new Vector3(xs[index],ys[index],0)),"position "+id);
            Assert.That((float)type.GetField("yaw").GetValue(profile),Is.EqualTo(id==13 ? 270f : id==8 || id==22 || id==28 ? 90f : 0f));
        }
        foreach(int id in new[]{3,9,12,15,23,24,26,27,29})
            Assert.Throws<TargetInvocationException>(()=>type.GetMethod("For").Invoke(null,new object[]{id}));
    }
    [Test]
    public void SupportPartsHaveNoDamageAndPartTwoScaleIsBakedInEveryVariant()
    {
        Type type=Runtime("BalloonDogTrapPlacement");
        foreach(int id in new[]{2,21,25}) for(int variant=1;variant<=4;variant++)
        {
            var prefab=Resources.Load<GameObject>("Endless/Trap"+id+(variant==1 ? "" : "V"+variant));
            var profile=type.GetMethod("For").Invoke(null,new object[]{id});
            foreach(var mesh in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                bool damage=(bool)type.GetMethod("CanDamage").Invoke(profile,new object[]{mesh.transform,prefab.transform});
                Assert.That(damage,Is.EqualTo(mesh.name=="O"+id+" Part "+(id==21 ? 2 : 1)));
                if(id==2 && mesh.name=="O2 Part 2") Assert.That(mesh.transform.localScale,Is.EqualTo(new Vector3(3.5f,1,1)));
            }
        }
    }
    [Test]
    public void EdgeAlignmentUsesExactScaledRotatedBoundsAndKeepsSpecifiedHeight()
    {
        Type type=Runtime("BalloonDogTrapPlacement");
        var bounds=new Bounds(new Vector3(.2f,1,0),new Vector3(2,2,1));
        foreach(int id in new[]{8,13,22,28}) foreach(bool right in new[]{false,true})
        {
            object profile=type.GetMethod("For").Invoke(null,new object[]{id});
            Vector3 position=(Vector3)type.GetMethod("Position").Invoke(profile,new object[]{0f,0f,14f,6f,bounds,right});
            if(id==13 || id==8 && right) Assert.That(position.x+bounds.max.x,Is.EqualTo(3).Within(.001f));
            else if(id==8) Assert.That(position.x+bounds.min.x,Is.EqualTo(-3).Within(.001f));
            else Assert.That(position.x,Is.EqualTo(id==22 ? 2.792f : -2.762f));
            Assert.That(position.z,Is.EqualTo(14));
        }
    }
    [Test]
    public void ObstacleEightFacesInwardOnEitherSideAndCombinationsKeepAnEscape()
    {
        Type type=Runtime("BalloonDogTrapPlacement");
        object profile=type.GetMethod("For").Invoke(null,new object[]{8});
        Assert.That((float)type.GetMethod("Yaw").Invoke(profile,new object[]{true}),Is.EqualTo(90));
        Assert.That((float)type.GetMethod("Yaw").Invoke(profile,new object[]{false}),Is.EqualTo(-90));
        var narrow=new Bounds(Vector3.zero,new Vector3(1.5f,2,2));
        var wide=new Bounds(Vector3.zero,new Vector3(4,2,2));
        Assert.That((bool)type.GetMethod("CanCombine").Invoke(null,new object[]{narrow,narrow,6f}),Is.True);
        Assert.That((bool)type.GetMethod("CanCombine").Invoke(null,new object[]{narrow,wide,6f}),Is.False);
        float left=(float)type.GetMethod("CombinedX").Invoke(null,new object[]{narrow,0f,6f,false});
        float right=(float)type.GetMethod("CombinedX").Invoke(null,new object[]{narrow,0f,6f,true});
        Assert.That(right+narrow.min.x-left-narrow.max.x,Is.GreaterThanOrEqualTo(1.4f));
        for(int lane=0;lane<3;lane++)
        {
            Vector2 pair=(Vector2)type.GetMethod("PairX").Invoke(null,new object[]{narrow,narrow,0f,6f,lane,true});
            float aMin=pair.x+narrow.min.x,aMax=pair.x+narrow.max.x;
            float bMin=pair.y+narrow.min.x,bMax=pair.y+narrow.max.x;
            Assert.That(Mathf.Min(aMin,bMin),Is.GreaterThanOrEqualTo(-2.801f));
            Assert.That(Mathf.Max(aMax,bMax),Is.LessThanOrEqualTo(2.801f));
            float corridor=lane==0 ? Mathf.Min(aMin,bMin)+2.8f : lane==2 ? 2.8f-Mathf.Max(aMax,bMax)
                : Mathf.Max(aMin,bMin)-Mathf.Min(aMax,bMax);
            Assert.That(corridor,Is.GreaterThanOrEqualTo(1.4f));
        }
    }
    [Test]
    public void OpenFrameAndDisabledRendererCannotCauseInvisibleDamage()
    {
        Type type=Runtime("BalloonDogTrapHitbox");
        var root=new GameObject("VisibleGeometryTest");
        var player=new GameObject("CapsuleTest");
        var mesh=new Mesh();Material material=null;
        try
        {
            root.AddComponent(Runtime("BalloonDogEndlessContact"));
            var view=new GameObject("OpenFrame");view.transform.SetParent(root.transform,false);
            var filter=view.AddComponent<MeshFilter>();var renderer=view.AddComponent<MeshRenderer>();
            var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
            // Four bars around an empty 2x2 opening, in the XY plane.
            foreach(var rect in new[]{new Rect(-2,0,1,4),new Rect(1,0,1,4),new Rect(-1,0,2,1),new Rect(-1,3,2,1)})
            {
                int v=vertices.Count;vertices.Add(new Vector3(rect.xMin,rect.yMin,0));vertices.Add(new Vector3(rect.xMax,rect.yMin,0));
                vertices.Add(new Vector3(rect.xMax,rect.yMax,0));vertices.Add(new Vector3(rect.xMin,rect.yMax,0));
                triangles.AddRange(new[]{v,v+1,v+2,v,v+2,v+3});
            }
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();filter.sharedMesh=mesh;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));renderer.sharedMaterial=material;
            var proxy=new GameObject("DamageProxy");proxy.transform.SetParent(root.transform,false);
            Component hitbox=proxy.AddComponent(type);
            type.GetMethod("Configure").Invoke(hitbox,new object[]{filter,root.GetComponent(Runtime("BalloonDogEndlessContact"))});
            var capsule=player.AddComponent<CapsuleCollider>();capsule.height=1;capsule.radius=.25f;
            player.transform.position=new Vector3(0,2,0);Physics.SyncTransforms();
            Assert.That((bool)type.GetMethod("TouchesVisibleGeometry").Invoke(hitbox,new object[]{capsule}),Is.False,"Empty opening must be safe");
            player.transform.position=new Vector3(1.15f,2,0);Physics.SyncTransforms();
            Assert.That((bool)type.GetMethod("TouchesVisibleGeometry").Invoke(hitbox,new object[]{capsule}),Is.True,"Visible bar must still hit");
            view.transform.position=Vector3.up*4;type.GetMethod("Refresh").Invoke(hitbox,null);Physics.SyncTransforms();
            Assert.That((bool)type.GetMethod("TouchesVisibleGeometry").Invoke(hitbox,new object[]{capsule}),Is.False,"Raised moving geometry must leave the space below safe");
            view.transform.position=Vector3.zero;renderer.enabled=false;type.GetMethod("Refresh").Invoke(hitbox,null);
            Assert.That(proxy.GetComponent<BoxCollider>().enabled,Is.False);
            Assert.That((bool)type.GetMethod("TouchesVisibleGeometry").Invoke(hitbox,new object[]{capsule}),Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(mesh);if(material!=null) UnityEngine.Object.DestroyImmediate(material);
        }
    }
    [UnityTest]
    public IEnumerator GameSceneKeepsMenuCanvasAndBuildsExactlySixReusableRoadChunks()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return BalloonDogSceneFlowTestUtility.WaitForReady();
        Component runtime=UnityEngine.Object.FindAnyObjectByType(Runtime("BalloonDogEndlessRunner")) as Component;
        Assert.That(runtime,Is.Not.Null);
        int roads=0;
        foreach(Transform child in runtime.transform) if(child.name.StartsWith("EndlessRoad_")) roads++;
        Assert.That(roads,Is.EqualTo(6));
        Component menu=UnityEngine.Object.FindAnyObjectByType(Runtime("MainMenuController")) as Component;
        Assert.That(menu,Is.Not.Null,"Retiring level progress must not disable the shared menu canvas");
        Component player=UnityEngine.Object.FindAnyObjectByType(Runtime("PlayerRunner")) as Component;
        Assert.That(player.GetComponentsInChildren<TrailRenderer>(true),Is.Empty,"The scene must contain no dog trail");
        Assert.That(player.GetComponent(Runtime("PlayerTrailCleanupRuntime")),Is.Not.Null);
        // A skin created later must lose its trail without losing its mesh/object.
        var lateSkin=new GameObject("LateSkin",typeof(MeshRenderer),typeof(TrailRenderer));
        lateSkin.transform.SetParent(player.transform,false);
        var hiddenTrail=new GameObject("InactiveNestedTrail",typeof(TrailRenderer));
        hiddenTrail.transform.SetParent(lateSkin.transform,false);
        hiddenTrail.SetActive(false);
        yield return null;
        yield return null;
        Assert.That(player.GetComponentsInChildren<TrailRenderer>(true),Is.Empty);
        Assert.That(lateSkin.GetComponent<MeshRenderer>(),Is.Not.Null);
        Assert.That(lateSkin.activeSelf,Is.True);
        UnityEngine.Object.Destroy(lateSkin);
        // A newly spawned runner also receives the permanent guard.
        var spawned=new GameObject("RespawnTrailCheck");
        spawned.SetActive(false);
        spawned.AddComponent<TrailRenderer>();
        var spawnedRunner=(Behaviour)spawned.AddComponent(Runtime("PlayerRunner"));
        spawnedRunner.enabled=false;
        spawned.GetComponent<Rigidbody>().isKinematic=true;
        spawned.SetActive(true);
        yield return null;
        yield return null;
        Assert.That(spawned.GetComponent(Runtime("PlayerTrailCleanupRuntime")),Is.Not.Null);
        Assert.That(spawned.GetComponentsInChildren<TrailRenderer>(true),Is.Empty);
        UnityEngine.Object.Destroy(spawned);
        var types=new System.Collections.Generic.HashSet<string>();
        foreach(Transform child in runtime.GetComponentsInChildren<Transform>(true))
        {
            if(!child.name.StartsWith("PooledTrap_")) continue;
            types.Add(child.name);
            var body=child.GetComponent<Rigidbody>();
            Assert.That(body,Is.Not.Null);Assert.That(body.isKinematic,Is.True);
            int id=int.Parse(child.name.Split('_')[1]);
            Component contactOwner=child.GetComponent(Runtime("BalloonDogEndlessContact"));
            bool heavy=(bool)contactOwner.GetType().GetField("heavyImpact",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(contactOwner);
            Assert.That(heavy,Is.EqualTo(id==4 || id==8 || id==22 || id==25),"All four variants must use the requested heavy-impact group");
            bool lethal=(bool)contactOwner.GetType().GetField("lethalImpact",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(contactOwner);
            Assert.That(lethal,Is.EqualTo(id!=4 && id!=8 && id!=22 && id!=25 && id!=28),"All other classified active traps must kill on contact");
            int flames=child.GetComponentsInChildren(Runtime("BalloonDogFlameHazard"),true).Length;
            Assert.That(flames,Is.EqualTo(id==28 ? 1 : 0),"Every active flame variant must have lethal VFX contact");
            var profileType=Runtime("BalloonDogTrapPlacement");
            object profile=profileType.GetMethod("For").Invoke(null,new object[]{id});
            Transform mount=child.Find("AuthoredTransform");
            Assert.That(mount.localScale,Is.EqualTo((Vector3)profileType.GetField("scale").GetValue(profile)));
            Assert.That(Quaternion.Angle(mount.localRotation,Quaternion.Euler(0,(float)profileType.GetField("yaw").GetValue(profile),0)),Is.LessThan(.001f));
            foreach(var box in child.GetComponentsInChildren<BoxCollider>(true))
            {
                if(!box.enabled) continue;
                Assert.That(box.GetComponent(Runtime("BalloonDogTrapHitbox")),Is.Not.Null,"Every damage proxy must verify visible geometry");
                Assert.That(box.attachedRigidbody,Is.SameAs(body),"Moving part must report damage to its pooled contact owner");
                if(id==2 || id==21 || id==25)
                    Assert.That(box.name,Is.EqualTo("Damage_O"+id+" Part "+(id==21 ? 2 : 1)),"Support must never receive a damage trigger");
            }
        }
        Assert.That(types.Count,Is.EqualTo(80));
        foreach(string name in types)
        {
            int id=int.Parse(name.Split('_')[1]);
            CollectionAssert.Contains(AllowedIds,id);
        }
        foreach(var animator in runtime.GetComponentsInChildren<Animator>(true))
        {
            Assert.That(animator.enabled,Is.True);Assert.That(animator.applyRootMotion,Is.False);
            if(animator.runtimeAnimatorController==null) continue;
            foreach(var clip in animator.runtimeAnimatorController.animationClips)
                if(clip.name=="Obstacle 8" || clip.name=="Obstacle 22")
                {
                    var impact=animator.GetComponent(Runtime("BalloonDogTrapImpactVfx"));
                    Assert.That(impact,Is.Not.Null,"Ground-striking traps must replay dust on each animation loop");
                    Assert.That(((Behaviour)impact).enabled,Is.True,"The legacy smoke callback cleanup must not disable the new controller");
                }
        }
        int traps=runtime.GetComponentsInChildren(Runtime("BalloonDogEndlessContact"),true).Length;
        Assert.That(traps,Is.EqualTo(110));
        // Recycle through many ring revolutions without building more objects.
        var list=(IList)runtime.GetType().GetField("chunks",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(runtime);
        MethodInfo place=runtime.GetType().GetMethod("Place",BindingFlags.Instance|BindingFlags.NonPublic);
        runtime.GetType().GetField("tutorial",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(runtime,false);
        runtime.GetType().GetField("random",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(runtime,new System.Random(7301));
        var footprints=(IDictionary)runtime.GetType().GetField("sweptBounds",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(runtime);
        float roadWidth=(float)runtime.GetType().GetField("width",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(runtime);
        float center=(float)runtime.GetType().GetField("roadCenter",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(runtime);
        int pairCount=0;
        for(int cycle=0;cycle<100;cycle++) foreach(var chunk in list)
        {
            place.Invoke(runtime,new[]{chunk});
            var rows=(Array)chunk.GetType().GetField("traps").GetValue(chunk);
            var unique=new System.Collections.Generic.HashSet<Component>();
            Assert.That(rows.Length,Is.EqualTo(5));
            for(int row=0;row<rows.Length;row++)
            {
                var trap=(Component)rows.GetValue(row);
                Assert.That(unique.Add(trap),Is.True,"Every row must own a different pooled trap");
                Assert.That(trap.transform.localPosition.z,Is.EqualTo(14+row*11.2f).Within(.001f));
                var impact=trap.GetComponentInChildren(Runtime("BalloonDogTrapImpactVfx"),true) as Component;
                if(impact!=null)
                {
                    var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                    Assert.That((bool)impact.GetType().GetField("hasSample",flags).GetValue(impact),Is.True,
                        "Reused dust controllers must be synchronized to the new pose before animation resumes");
                    float sampled=(float)impact.GetType().GetField("previousTime",flags).GetValue(impact);
                    float posed=impact.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime;
                    Assert.That(sampled,Is.EqualTo(posed).Within(.0001f));
                }
                var companions=(Array)chunk.GetType().GetField("companions").GetValue(chunk);
                if(companions.GetValue(row) is Component companion)
                {
                    pairCount++;
                    Assert.That(unique.Add(companion),Is.True);
                    Assert.That(companion.transform.localPosition.z,Is.EqualTo(trap.transform.localPosition.z));
                    var a=(Bounds)footprints[trap];var b=(Bounds)footprints[companion];
                    a.center+=trap.transform.localPosition;b.center+=companion.transform.localPosition;
                    float corridor=Mathf.Max(Mathf.Min(a.min.x,b.min.x)-(center-roadWidth*.5f+.2f),
                        center+roadWidth*.5f-.2f-Mathf.Max(a.max.x,b.max.x),
                        Mathf.Max(a.min.x,b.min.x)-Mathf.Min(a.max.x,b.max.x));
                    Assert.That(corridor,Is.GreaterThanOrEqualTo(1.399f),"Denser pairs must retain the full swept escape corridor");
                }
            }
        }
        Assert.That(pairCount,Is.GreaterThan(0),"Eligible narrow traps must actually generate pairs");
        var owners=new System.Collections.Generic.HashSet<Component>();
        foreach(var chunk in list)
        {
            int activeAir=0;
            foreach(Component pickup in (Array)chunk.GetType().GetField("air").GetValue(chunk))
                if(pickup.gameObject.activeSelf) activeAir++;
            Assert.That(activeAir,Is.EqualTo(1),"Each chunk now has one pickup instead of two.");
            foreach(string field in new[]{"traps","companions"})
                foreach(var item in (Array)chunk.GetType().GetField(field).GetValue(chunk))
                    if(item is Component contact) Assert.That(owners.Add(contact),Is.True,"Two rows cannot lease the same trap");
        }
        Assert.That(runtime.GetComponentsInChildren(Runtime("BalloonDogEndlessContact"),true).Length,Is.EqualTo(traps));
        Time.timeScale=1;
        yield return new ExitPlayMode();
    }

}
