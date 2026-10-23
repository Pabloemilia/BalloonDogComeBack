using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public static class BalloonDogSceneFlowTestUtility
{
    public static IEnumerator WaitForReady()
    {
        Type flow = Type.GetType("BalloonDogSceneFlow, Assembly-CSharp", true);
        PropertyInfo busy = flow.GetProperty("IsBusy");
        for (int frame = 0; frame < 1200 && (bool)busy.GetValue(null); frame++) yield return null;
        Assert.That((bool)busy.GetValue(null), Is.False, "Startup/transition must finish only after game and UI preparation.");
    }
}

public sealed class BalloonDogSceneFlowTests
{
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(Type type, object target, string name, params object[] args) =>
        type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Invoke(target, args);
    private static bool Busy => (bool)Runtime("BalloonDogSceneFlow").GetProperty("IsBusy").GetValue(null);

    [Test]
    public void BuildStartsWithLightweightBootThenLoadsTheExistingGame()
    {
        var scenes = Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled);
        Assert.That(scenes.Length, Is.GreaterThanOrEqualTo(2));
        Assert.That(scenes[0].path, Is.EqualTo("Assets/Scenes/Boot.unity"));
        Assert.That(scenes[1].path, Is.EqualTo("Assets/Scenes/Game.unity"));
        Texture2D art = Resources.Load<Texture2D>("MenuRefresh/LoadingScreen");
        Assert.That(art, Is.Not.Null);
        Assert.That((float)art.width / art.height, Is.EqualTo(9f / 16f).Within(.002f));
    }

    [Test]
    public void ProgressRepresentsCompletedWorkAndNeverClaimsPrematureCompletion()
    {
        Type flow = Runtime("BalloonDogSceneFlow");
        Assert.That((float)Call(flow, null, "SceneLoadProgress", 0f), Is.EqualTo(.1f).Within(.0001f));
        Assert.That((float)Call(flow, null, "SceneLoadProgress", .9f), Is.EqualTo(.7f).Within(.0001f));
        foreach (float preparation in new[] { 0f, .25f, .5f, .75f, 1f })
        {
            float beforeUi = (float)Call(flow, null, "PreparationProgress", preparation, false);
            float afterUi = (float)Call(flow, null, "PreparationProgress", preparation, true);
            Assert.That(beforeUi, Is.LessThan(1f));
            Assert.That(afterUi, Is.LessThan(1f));
            Assert.That(afterUi, Is.GreaterThan(beforeUi));
        }
        float previous = 0f;
        for (int i = 0; i <= 100; i++)
        {
            float alpha = (float)Call(flow, null, "FadeAmount", i / 100f);
            Assert.That(alpha, Is.InRange(previous, 1f));
            previous = alpha;
        }
        Assert.That(previous, Is.EqualTo(1f));
        Assert.That((float)Call(flow, null, "FadeAmount", .5f), Is.EqualTo(.5f).Within(.0001f));
    }

    [UnityTest]
    public IEnumerator BootPresentsRealArtworkAndCompletesOnlyAfterRuntimeSetup()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        bool sawArt = false;
        float lastProgress = 0f;
        for (int frame = 0; frame < 1200 && Busy; frame++)
        {
            Component flow = UnityEngine.Object.FindAnyObjectByType(Runtime("BalloonDogSceneFlow")) as Component;
            float progress = (float)flow.GetType().GetProperty("LoadingProgress").GetValue(flow);
            Assert.That(progress, Is.InRange(lastProgress, 1f));
            lastProgress = progress;
            var image = GameObject.Find("ApprovedRedDogIllustration")?.GetComponent<RawImage>();
            sawArt |= image != null && image.texture != null;
            if (progress >= 1f)
            {
                Component course = UnityEngine.Object.FindAnyObjectByType(Runtime("BalloonDogEndlessRunner")) as Component;
                Assert.That(course, Is.Not.Null);
                Assert.That((bool)course.GetType().GetProperty("IsInitialized").GetValue(course), Is.True);
                Component ui = UnityEngine.Object.FindAnyObjectByType(Runtime("BalloonDogModernUI")) as Component;
                Assert.That((bool)ui.GetType().GetProperty("IsReady").GetValue(ui), Is.True);
                Assert.That((Scene)ui.GetType().GetProperty("PreparedScene").GetValue(ui),
                    Is.EqualTo(SceneManager.GetActiveScene()));
            }
            yield return null;
        }
        Assert.That(Busy, Is.False);
        Assert.That(sawArt, Is.True);
        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo("Assets/Scenes/Game.unity"));
        Assert.That(GameObject.Find("ModernMainScreen"), Is.Not.Null);
        Assert.That(Time.timeScale, Is.Zero);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator PausedDeathToMenuFadesBeforeReloadAndDoesNotDuplicateTheFlow()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return BalloonDogSceneFlowTestUtility.WaitForReady();
        Component manager = UnityEngine.Object.FindAnyObjectByType(Runtime("GameManager")) as Component;
        Call(manager.GetType(), manager, "TriggerGameOver", "TRANSITION TEST");
        Call(manager.GetType(), manager, "ExpireContinueOffer");
        Scene original = SceneManager.GetActiveScene();
        Call(manager.GetType(), manager, "ReturnToMainMenu");
        Component flow = UnityEngine.Object.FindAnyObjectByType(Runtime("BalloonDogSceneFlow")) as Component;
        float alpha = (float)flow.GetType().GetProperty("CoverAlpha").GetValue(flow);
        Assert.That(Busy, Is.True);
        Assert.That(alpha, Is.LessThan(1f), "The first frame must start a fade, not jump straight to black.");
        Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(original));
        Call(manager.GetType(), manager, "ReturnToMainMenu"); // Repeated input is ignored.
        bool observedBlack = false;
        for (int frame = 0; frame < 1200 && Busy; frame++)
        {
            alpha = (float)flow.GetType().GetProperty("CoverAlpha").GetValue(flow);
            observedBlack |= alpha >= .999f;
            if (SceneManager.GetActiveScene() == original)
                Assert.That(Time.timeScale, Is.Zero);
            else
            {
                Component course = UnityEngine.Object.FindAnyObjectByType(Runtime("BalloonDogEndlessRunner")) as Component;
                Component ui = UnityEngine.Object.FindAnyObjectByType(Runtime("BalloonDogModernUI")) as Component;
                if (course == null || !(bool)course.GetType().GetProperty("IsInitialized").GetValue(course)
                    || ui == null || !(bool)ui.GetType().GetProperty("IsReady").GetValue(ui))
                    Assert.That(alpha, Is.EqualTo(1f), "Scene setup must stay fully covered.");
            }
            yield return null;
        }
        Assert.That(Busy, Is.False);
        Assert.That(observedBlack, Is.True);
        Assert.That(SceneManager.GetActiveScene(), Is.Not.EqualTo(original));
        Assert.That(GameObject.Find("ModernMainScreen"), Is.Not.Null);
        Assert.That(UnityEngine.Object.FindObjectsByType(Runtime("BalloonDogSceneFlow"), FindObjectsSortMode.None).Length, Is.EqualTo(1));
        Assert.That((bool)Runtime("MainMenuController").GetProperty("StartImmediatelyOnNextSceneLoad").GetValue(null), Is.False);
        Assert.That(Time.timeScale, Is.Zero);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator ReplayDefersItsCountdownUntilTheCoverIsGone()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return BalloonDogSceneFlowTestUtility.WaitForReady();
        Component manager = UnityEngine.Object.FindAnyObjectByType(Runtime("GameManager")) as Component;
        Call(manager.GetType(), manager, "RestartGame");
        Scene original = SceneManager.GetActiveScene();
        bool observedPreparedReplay = false;
        for (int frame = 0; frame < 1200 && Busy; frame++)
        {
            if (SceneManager.GetActiveScene() != original)
            {
                observedPreparedReplay = true;
                Component countdown = UnityEngine.Object.FindAnyObjectByType(Runtime("StartCountdownController")) as Component;
                if (countdown != null)
                    Assert.That((bool)countdown.GetType().GetField("running", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(countdown), Is.False);
            }
            yield return null;
        }
        Assert.That(Busy, Is.False);
        Assert.That(observedPreparedReplay, Is.True);
        yield return null;
        Assert.That((bool)Runtime("MainMenuController").GetProperty("StartImmediatelyOnNextSceneLoad").GetValue(null), Is.False);
        Assert.That(GameObject.Find("ModernMainScreen"), Is.Null);
        yield return new ExitPlayMode();
    }
}
