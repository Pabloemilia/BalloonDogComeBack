using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
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
