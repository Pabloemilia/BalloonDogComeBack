using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BalloonDogCourseTests
{
    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(Type type, object instance, string method, params object[] args) =>
        type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static).Invoke(instance, args);

    [TestCase(-1f, 1)]
    [TestCase(0f, 1)]
    [TestCase(.099f, 1)]
    [TestCase(.1f, 2)]
    [TestCase(.5f, 6)]
    [TestCase(.9f, 10)]
    [TestCase(1f, 10)]
    [TestCase(2f, 10)]
    public void AirMapsToExpectedBonusZone(float air, int multiplier)
    {
        Type finale = RuntimeType("BalloonDogMapFinale");
        Assert.That(Call(finale, null, "MultiplierForAir", air), Is.EqualTo(multiplier));
        float landingZ = (float)Call(finale, null, "LandingZForAir", air);
        Assert.That(landingZ, Is.InRange(71f, 125f));
        Assert.That(landingZ, Is.EqualTo(68f + (multiplier - .5f) * 6f));
    }

    [Test]
    public void RootCurvesCannotMoveOrResizeTheAuthoredTrap()
    {
        GameObject parent = new GameObject("PlacementTestParent");
        AnimationClip clip = new AnimationClip { name = "Obstacle Placement Test" };
        AnimatorController controller = new AnimatorController();
        AnimatorStateMachine machine = new AnimatorStateMachine();
        try
        {
            parent.transform.position = new Vector3(3f, .2f, 11f);
            parent.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            parent.transform.localScale = new Vector3(2f, 1f, 1.25f);
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.transform.SetParent(parent.transform, false);
            root.transform.localPosition = new Vector3(.395f, 0f, -3.12f);
            root.transform.localRotation = Quaternion.Euler(90f, 15f, 0f);
            root.transform.localScale = new Vector3(1.5f, .8f, 2f);
            Transform tip = new GameObject("MovingTip").transform;
            tip.SetParent(root.transform, false);
            tip.localPosition = Vector3.up;
            foreach (string axis in new[] { "x", "y", "z" })
            {
                clip.SetCurve("", typeof(Transform), "localPosition." + axis,
                    AnimationCurve.Constant(0f, 1f, 0f));
                clip.SetCurve("", typeof(Transform), "localRotation." + axis,
                    AnimationCurve.Constant(0f, 1f, 0f));
                clip.SetCurve("", typeof(Transform), "localScale." + axis,
                    AnimationCurve.Constant(0f, 1f, 1f));
            }
            clip.SetCurve("", typeof(Transform), "localRotation.w", AnimationCurve.Constant(0f, 1f, 1f));
            clip.SetCurve("MovingTip", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0f, 1f, 1f, 2f));
            AnimatorState state = machine.AddState("Move");
            state.motion = clip;
            machine.defaultState = state;
            controller.layers = new[] { new AnimatorControllerLayer { name = "Base Layer", stateMachine = machine, defaultWeight = 1f } };
            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            Matrix4x4 before = root.transform.localToWorldMatrix;
            Type setup = RuntimeType("AuthoredObstaclePlacement");
            Transform placement = (Transform)Call(setup, null, "PreservePlacement", animator);
            clip.SampleAnimation(root, 0f);
            for (int i = 0; i < 16; i++)
                Assert.That(root.transform.localToWorldMatrix[i], Is.EqualTo(before[i]).Within(.0001f));
            Assert.That(Call(setup, null, "PreservePlacement", animator), Is.SameAs(placement));
            clip.SampleAnimation(root, 1f);
            Assert.That(tip.localPosition.y, Is.EqualTo(2f).Within(.001f));
            for (int i = 0; i < 16; i++)
                Assert.That(root.transform.localToWorldMatrix[i], Is.EqualTo(before[i]).Within(.0001f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(parent);
            UnityEngine.Object.DestroyImmediate(controller);
            UnityEngine.Object.DestroyImmediate(machine);
            UnityEngine.Object.DestroyImmediate(clip);
        }
    }

    [Test]
    public void BonusRunwayUsesTheActualRoadBoundsWithoutChangingTheRoad()
    {
        GameObject existingRoad = GameObject.Find("Road");
        GameObject road = existingRoad != null ? existingRoad : GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        Vector3 originalPosition = road.transform.position;
        Vector3 originalScale = road.transform.localScale;
        try
        {
            road.transform.position = new Vector3(0f, -.5f, 50f);
            road.transform.localScale = new Vector3(8f, 1f, 100f);
            Bounds bounds = road.GetComponent<Renderer>().bounds;
            Type finale = RuntimeType("BalloonDogMapFinale");
            Call(finale, null, "RefreshCourseLayout");
            Assert.That(finale.GetProperty("BonusStartZ").GetValue(null), Is.EqualTo(bounds.max.z));
            Assert.That(finale.GetProperty("RoadWidth").GetValue(null), Is.EqualTo(bounds.size.x));
            Assert.That(finale.GetProperty("FinishZ").GetValue(null), Is.EqualTo(bounds.max.z - 3f));
            Assert.That(road.transform.position, Is.EqualTo(new Vector3(0f, -.5f, 50f)));
            Assert.That(road.transform.localScale, Is.EqualTo(new Vector3(8f, 1f, 100f)));
        }
        finally
        {
            road.transform.position = originalPosition;
            road.transform.localScale = originalScale;
            if (existingRoad == null) UnityEngine.Object.DestroyImmediate(road);
            Call(RuntimeType("BalloonDogMapFinale"), null, "RefreshCourseLayout");
        }
    }

    [UnityTest]
    public IEnumerator DeathShowsContinueBeforeFinalResultAndExpiresOnce()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return BalloonDogSceneFlowTestUtility.WaitForReady();
        Component manager = UnityEngine.Object.FindAnyObjectByType(RuntimeType("GameManager")) as Component;
        Type type = manager.GetType();
        int ended = 0;
        Action onEnded = () => ended++;
        type.GetEvent("GameEnded").AddEventHandler(manager, onEnded);
        Call(type, manager, "TriggerGameOver", "TEST DEATH");
        Assert.That((bool)type.GetProperty("IsContinuePending").GetValue(manager), Is.True);
        Assert.That((bool)type.GetProperty("IsResultFinalized").GetValue(manager), Is.False);
        Assert.That(ended, Is.Zero);
        Assert.That(Time.timeScale, Is.Zero);
        Component prompt = manager.GetComponent(RuntimeType("BalloonDogContinuePrompt"));
        Type promptType = prompt.GetType();
        Assert.That(GameObject.Find("ContinueOfferCanvas"), Is.Not.Null);
        // Duplicate fatal contacts cannot reset the four-second offer.
        Call(promptType, prompt, "Advance", 2f);
        Call(type, manager, "TriggerGameOver", "SECOND CONTACT");
        Assert.That((float)promptType.GetProperty("RemainingSeconds").GetValue(prompt), Is.EqualTo(2f).Within(.001f));
        // With no ad provider, requesting an ad must not grant a revive or reset time.
        promptType.GetField("ShowRewardedAd").SetValue(null, null);
        Call(promptType, prompt, "WatchAd");
        Assert.That((bool)type.GetProperty("IsContinuePending").GetValue(manager), Is.True);
        Call(promptType, prompt, "Advance", 1.99f);
        Assert.That(ended, Is.Zero);
        Call(promptType, prompt, "Advance", .02f);
        Assert.That((bool)type.GetProperty("IsResultFinalized").GetValue(manager), Is.True);
        Assert.That(ended, Is.EqualTo(1));
        Call(type, manager, "ExpireContinueOffer");
        Call(type, manager, "CompleteRewardedContinue");
        Assert.That(ended, Is.EqualTo(1));
        Assert.That((bool)type.GetProperty("IsGameOver").GetValue(manager), Is.True);
        type.GetEvent("GameEnded").RemoveEventHandler(manager, onEnded);
        Time.timeScale = 1f;
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator RewardCallbackAloneCanResumeAndFailurePreservesRemainingTime()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return BalloonDogSceneFlowTestUtility.WaitForReady();
        Component manager = UnityEngine.Object.FindAnyObjectByType(RuntimeType("GameManager")) as Component;
        Type type = manager.GetType();
        Call(type, manager, "TriggerGameOver", "TEST DEATH");
        Component prompt = manager.GetComponent(RuntimeType("BalloonDogContinuePrompt"));
        Type pt = prompt.GetType();
        Action<bool> reward = null;
        Action<Action<bool>> provider = callback => reward = callback;
        pt.GetField("ShowRewardedAd").SetValue(null, provider);
        Call(pt, prompt, "Advance", 1f);
        Call(pt, prompt, "WatchAd");
        Call(pt, prompt, "Advance", 5f);
        Assert.That((float)pt.GetProperty("RemainingSeconds").GetValue(prompt), Is.EqualTo(3f).Within(.001f));
        reward(false);
        Call(pt, prompt, "Advance", 0f);
        Assert.That((bool)type.GetProperty("IsContinuePending").GetValue(manager), Is.True);
        Call(pt, prompt, "WatchAd");
        reward(true);
        Call(pt, prompt, "Advance", 0f);
        Assert.That((bool)type.GetProperty("IsGameOver").GetValue(manager), Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Component air = UnityEngine.Object.FindAnyObjectByType(RuntimeType("AirController")) as Component;
        Assert.That((float)air.GetType().GetProperty("NormalizedAir").GetValue(air), Is.EqualTo(1f));
        // A duplicate callback must not revive a different, subsequent death offer.
        Call(type, manager, "TriggerGameOver", "NEXT DEATH");
        reward(true);
        Call(pt, prompt, "Advance", 0f);
        Assert.That((bool)type.GetProperty("IsContinuePending").GetValue(manager), Is.True);
        pt.GetField("ShowRewardedAd").SetValue(null, null);
        Time.timeScale = 1f;
        yield return new ExitPlayMode();
    }
}
