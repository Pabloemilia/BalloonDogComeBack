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
    public IEnumerator DogStaysAtFinishAndPauseFreezesTheSeparateBalloon()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        // Allow the existing menu/UI rebuild to finish while time is frozen.
        for (int i = 0; i < 15; i++) yield return null;
        Component menu = UnityEngine.Object.FindAnyObjectByType(RuntimeType("MainMenuController")) as Component;
        Call(menu.GetType(), menu, "StartGame");
        yield return new WaitForSecondsRealtime(3f);
        Component runner = UnityEngine.Object.FindAnyObjectByType(RuntimeType("PlayerRunner")) as Component;
        Rigidbody body = runner.GetComponent<Rigidbody>();
        body.position = new Vector3(0f, 1f, 65f);
        runner.transform.position = body.position;
        yield return new WaitForSeconds(.65f);
        GameObject balloon = GameObject.Find("AirBonusBalloon");
        Assert.That(balloon, Is.Not.Null);
        Assert.That(runner.transform.position.z, Is.EqualTo(65f).Within(.01f));
        Assert.That(GameObject.Find("GameplayPauseButton"), Is.Not.Null);
        Component ui = UnityEngine.Object.FindAnyObjectByType(RuntimeType("BalloonDogModernUI")) as Component;
        Call(ui.GetType(), ui, "PauseGame");
        Vector3 pausedPosition = balloon.transform.position;
        yield return new WaitForSecondsRealtime(.3f);
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(Vector3.Distance(balloon.transform.position, pausedPosition), Is.LessThan(.001f));
        Call(ui.GetType(), ui, "ResumeGame");
        // Pause-screen exit uses unscaled animation; do not advance the run until it closes.
        float deadline = Time.realtimeSinceStartup + 3f;
        while (Time.timeScale == 0f && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        deadline = Time.realtimeSinceStartup + 7f;
        Component manager = UnityEngine.Object.FindAnyObjectByType(RuntimeType("GameManager")) as Component;
        while (!(bool)manager.GetType().GetProperty("IsGameOver").GetValue(manager) &&
               Time.realtimeSinceStartup < deadline)
        {
            Assert.That(balloon.transform.position.z, Is.LessThan(128f));
            Assert.That(runner.transform.position.z, Is.EqualTo(65f).Within(.01f));
            yield return null;
        }
        Assert.That((bool)manager.GetType().GetProperty("LastRunCompleted").GetValue(manager), Is.True);
        Assert.That(balloon.transform.position.z, Is.EqualTo(125f).Within(.01f));
        Time.timeScale = 1f;
        yield return new ExitPlayMode();
    }
}
