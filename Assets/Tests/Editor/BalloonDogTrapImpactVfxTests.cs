using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public sealed class BalloonDogTrapImpactVfxTests
{
    private static Type Runtime => Type.GetType("BalloonDogTrapImpactVfx, Assembly-CSharp", true);
    private static bool Crossed(float previous, float current, float phase) => (bool)Runtime
        .GetMethod("CrossedImpact", BindingFlags.Static | BindingFlags.NonPublic)
        .Invoke(null, new object[] { previous, current, phase });

    [Test]
    public void AllSeventeenDustEmittersStartStoppedAndFlamesKeepTheirOwnStartup()
    {
        int count = 0;
        foreach (int id in new[] { 8, 22 })
            for (int variant = 1; variant <= 4; variant++)
            {
                string resource = "Endless/Trap" + id + (variant == 1 ? "" : "V" + variant);
                GameObject prefab = Resources.Load<GameObject>(resource);
                Assert.That(prefab, Is.Not.Null, resource);
                foreach (VisualEffect effect in prefab.GetComponentsInChildren<VisualEffect>(true))
                {
                    Assert.That(effect.visualEffectAsset.name, Is.EqualTo("SmokePoof"));
                    Assert.That(effect.initialEventName, Is.EqualTo("OnStop"), resource);
                    count++;
                }
            }
        Assert.That(count, Is.EqualTo(17));
        foreach (VisualEffect flame in Resources.Load<GameObject>("Endless/Trap28").GetComponentsInChildren<VisualEffect>(true))
            Assert.That(flame.initialEventName, Is.EqualTo("OnPlay"), "The flame timer must remain independent of dust.");
    }

    [TestCase(8)]
    [TestCase(22)]
    public void TheMarkerMatchesTheAuthoredGroundStrikeAndDoesNotRepeatDuringTheHold(int id)
    {
        string path = "Assets/Downtown Game Studio/LOW POLY ASSETS - Animated Traps & Obstacles + VFX/Animations/Obstacle " + id + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        Assert.That(clip, Is.Not.Null);
        var root = new GameObject("ImpactPoseTest");
        try
        {
            Transform part = new GameObject("O" + id + " Part 2").transform;
            part.SetParent(root.transform, false);
            clip.SampleAnimation(root, .25f);
            Assert.That(Quaternion.Angle(part.localRotation, Quaternion.Euler(-90f, 0f, 0f)), Is.LessThan(.01f));
            float marker = .25f / clip.length;
            Assert.That(Crossed(.24f / clip.length, .25f / clip.length, marker), Is.True);
            Assert.That(Crossed(.25f / clip.length, 1f / clip.length, marker), Is.False,
                "Holding the hammer on the floor must not produce a continuous cloud.");
            Assert.That(Crossed(1f / clip.length, 2.5f / clip.length, marker), Is.False,
                "Raising the hammer must not emit dust.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    [TestCase(30)]
    [TestCase(50)]
    [TestCase(120)]
    public void EachLoopEmitsOnceAtTheAnimationPlaybackSpeed(int frequency)
    {
        const float marker = .25f / 3f;
        float previous = .62f; // A pooled trap starts partway through its clip.
        int count = 0;
        for (int frame = 1; frame <= 60 * frequency; frame++)
        {
            float current = .62f + frame * .8f / (frequency * 3f);
            if (Crossed(previous, current, marker)) count++;
            Assert.That(Crossed(current, current, marker), Is.False, "Pause must not replay a strike.");
            previous = current;
        }
        Assert.That(count, Is.EqualTo(16));
        Assert.That(Crossed(9.8f, .7f, marker), Is.False, "Resetting a pooled pose must not emit dust.");
        Assert.That(Crossed(.7f, 1.02f, marker), Is.False, "Loop wrap is not the impact point.");
        Assert.That(Crossed(1.02f, 1.09f, marker), Is.True, "The next loop must emit again.");
    }
}
