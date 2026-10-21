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
    public void EveryPatternHasAReachableClearLaneAcrossChunkBoundaries()
    {
        Type type=Runtime("BalloonDogEndlessRunner");
        var patterns=(int[][])type.GetField("Patterns").GetValue(null);
        MethodInfo safe=type.GetMethod("MakeSafeMask");
        Assert.That(patterns.Length,Is.EqualTo(18));
        foreach(var first in patterns) foreach(var next in patterns) for(int start=0;start<3;start++)
        {
            int previous=start;
            foreach(var pattern in new[]{first,next}) foreach(int source in pattern)
            {
                object[] args={source,previous,0};
                int mask=(int)safe.Invoke(null,args);int lane=(int)args[2];
                Assert.That(mask,Is.InRange(0,6));
                Assert.That(mask & (1<<lane),Is.Zero);
                Assert.That(Math.Abs(lane-previous),Is.LessThanOrEqualTo(1));
                previous=lane;
            }
        }
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
        for(int id=1;id<=29;id++)
            Assert.That(Resources.Load<GameObject>("Endless/Trap"+id),Is.Not.Null);
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
    [Test]
    public void EnlargedAnimatedEnvelopeFitsOneLaneAndKeepsAPlayerSizedEscape()
    {
        Type type=Runtime("BalloonDogAnimatedTrap");
        foreach(Vector3 extent in new[]{new Vector3(4,.4f,1),new Vector3(1,7,3),new Vector3(6,3,8)})
        {
            var bounds=new Bounds(Vector3.zero,extent);
            Vector3 scale=(Vector3)type.GetMethod("FitScale").Invoke(null,new object[]{bounds});
            Vector3 fitted=Vector3.Scale(bounds.size,scale);
            Assert.That(fitted.x,Is.LessThanOrEqualTo(1.701f));
            Assert.That(fitted.z,Is.LessThanOrEqualTo(5.201f));
            Assert.That(fitted.y,Is.InRange(.799f,4.501f));
            Assert.That(2f-fitted.x*.5f-.552f,Is.GreaterThan(.5f));
        }
    }
    [UnityTest]
    public IEnumerator GameSceneKeepsMenuCanvasAndBuildsExactlySixReusableRoadChunks()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        for(int i=0;i<15;i++) yield return null;
        Component runtime=UnityEngine.Object.FindAnyObjectByType(Runtime("BalloonDogEndlessRunner")) as Component;
        Assert.That(runtime,Is.Not.Null);
        int roads=0;
        foreach(Transform child in runtime.transform) if(child.name.StartsWith("EndlessRoad_")) roads++;
        Assert.That(roads,Is.EqualTo(6));
        Component menu=UnityEngine.Object.FindAnyObjectByType(Runtime("MainMenuController")) as Component;
        Assert.That(menu,Is.Not.Null,"Retiring level progress must not disable the shared menu canvas");
        var types=new System.Collections.Generic.HashSet<string>();
        foreach(Transform child in runtime.GetComponentsInChildren<Transform>(true))
        {
            if(!child.name.StartsWith("PooledTrap_")) continue;
            types.Add(child.name);
            var body=child.GetComponent<Rigidbody>();
            Assert.That(body,Is.Not.Null);Assert.That(body.isKinematic,Is.True);
            foreach(var box in child.GetComponentsInChildren<BoxCollider>(true))
                if(box.enabled) Assert.That(box.attachedRigidbody,Is.SameAs(body),"Moving part must report damage to its pooled contact owner");
        }
        Assert.That(types.Count,Is.EqualTo(29));
        foreach(var animator in runtime.GetComponentsInChildren<Animator>(true))
        { Assert.That(animator.enabled,Is.True);Assert.That(animator.applyRootMotion,Is.False); }
        int traps=runtime.GetComponentsInChildren(Runtime("BalloonDogEndlessContact"),true).Length;
        Assert.That(traps,Is.EqualTo(96));
        // Recycle through many ring revolutions without building more objects.
        var list=(IList)runtime.GetType().GetField("chunks",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(runtime);
        MethodInfo place=runtime.GetType().GetMethod("Place",BindingFlags.Instance|BindingFlags.NonPublic);
        for(int cycle=0;cycle<100;cycle++) foreach(var chunk in list) place.Invoke(runtime,new[]{chunk});
        Assert.That(runtime.GetComponentsInChildren(Runtime("BalloonDogEndlessContact"),true).Length,Is.EqualTo(traps));
        Time.timeScale=1;
        yield return new ExitPlayMode();
    }

}
