using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BalloonDogObstacleDamageTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly int[] LethalIds = { 1, 3, 5, 6, 7, 10, 11, 13, 14, 16, 17, 18, 19, 20, 21 };
    private static readonly int[] HeavyIds = { 4, 8, 22, 25 };
    private static readonly int[] ClassifiedIds = { 1, 3, 4, 5, 6, 7, 8, 10, 11, 13, 14, 16, 17, 18, 19, 20, 21, 22, 25 };
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(Component target, string name, params object[] args) =>
        target.GetType().GetMethod(name, BindingFlags.Public | Private).Invoke(target, args);
    private static T Get<T>(Component target, string name) => (T)target.GetType().GetProperty(name).GetValue(target);
    private GameObject player;
    private GameObject trap;
    private Component air;
    private Component runner;
    private Component contact;
    private Collider collider;
    private float previousTimeScale;

    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        previousTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        player = new GameObject("ObstacleDamagePlayer", typeof(Rigidbody), typeof(CapsuleCollider));
        player.GetComponent<Rigidbody>().useGravity = false;
        collider = player.GetComponent<Collider>();
        air = player.AddComponent(Runtime("AirController"));
        runner = player.AddComponent(Runtime("PlayerRunner"));
        Call(air, "Awake");
        Call(runner, "Awake");
        trap = new GameObject("PersistentObstacle");
        contact = trap.AddComponent(Runtime("BalloonDogEndlessContact"));
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(trap);
        UnityEngine.Object.DestroyImmediate(player);
        Time.timeScale = previousTimeScale;
    }

    [TestCaseSource(nameof(LethalIds))]
    public void EveryLethalIdKillsImmediatelyAndKeepsTheMechanism(int id)
    {
        Call(contact, "ConfigureObstacle", id);
        LogAssert.Expect(LogType.Error, "AirController: GameManager bulunamadı.");
        Call(contact, "ApplyVisibleHit", collider);
        Assert.That(Get<bool>(air, "IsEmpty"), Is.True, "Obstacle " + id);
        Assert.That(trap.activeSelf, Is.True);
        Call(contact, "ApplyVisibleHit", collider);
        LogAssert.NoUnexpectedReceived();
    }

    [TestCaseSource(nameof(HeavyIds))]
    public void EveryHeavyIdTakesEightyPercentAndAppliesTwoMetresOfRecoil(int id)
    {
        Call(contact, "ConfigureObstacle", id);
        Call(air, "RemoveAir", 50f);
        Call(contact, "ApplyVisibleHit", collider);
        Assert.That(Get<float>(air, "CurrentAir"), Is.EqualTo(10f).Within(.0001f));
        Assert.That(trap.activeSelf, Is.True);
        float distance = 0f;
        for (int frame = 0; frame < 100; frame++)
        {
            Call(runner, "FixedUpdate");
            float speed = player.GetComponent<Rigidbody>().linearVelocity.z;
            if (speed < 0f) distance -= speed * Time.fixedDeltaTime;
            Call(contact, "ApplyVisibleHit", collider);
        }
        Assert.That(distance, Is.EqualTo(2f).Within(.0001f));
        Assert.That(Get<float>(air, "CurrentAir"), Is.EqualTo(10f).Within(.0001f),
            "Multipart hits, stay callbacks and returning through the obstacle must not stack damage.");
        Call(contact, "ResetContact", false);
        Call(contact, "ResetContact", true);
        Call(contact, "ApplyVisibleHit", collider);
        Assert.That(Get<float>(air, "CurrentAir"), Is.EqualTo(2f).Within(.0001f));
        Assert.That(trap.activeSelf, Is.True);
    }

    [TestCaseSource(nameof(ClassifiedIds))]
    public void ProtectedContinueDoesNotConsumeAnyClassifiedObstacle(int id)
    {
        Call(contact, "ConfigureObstacle", id);
        Call(air, "RestoreAfterContinue");
        Call(contact, "ApplyVisibleHit", collider);
        Assert.That(Get<float>(air, "CurrentAir"), Is.EqualTo(100f));
        Assert.That((bool)contact.GetType().GetField("consumed", Private).GetValue(contact), Is.False);
        Assert.That(trap.activeSelf, Is.True);
        air.GetType().GetField("continueProtectionUntil", Private).SetValue(air, Time.time - 1f);
        bool lethal = Array.IndexOf(LethalIds, id) >= 0;
        if (lethal) LogAssert.Expect(LogType.Error, "AirController: GameManager bulunamadı.");
        Call(contact, "ApplyVisibleHit", collider);
        Assert.That(Get<float>(air, "CurrentAir"), Is.EqualTo(lethal ? 0f : 20f).Within(.0001f));
        Assert.That(trap.activeSelf, Is.True);
    }

    [Test]
    public void UnclassifiedNozzleStaysInTheWorldWhileAirPickupsStillDisappear()
    {
        Call(contact, "ConfigureObstacle", 28);
        UnityEngine.Object.DestroyImmediate(runner); // No edit-mode slow coroutine is needed here.
        Call(contact, "ApplyVisibleHit", collider);
        Assert.That(Get<float>(air, "CurrentAir"), Is.EqualTo(92f));
        Assert.That(trap.activeSelf, Is.True);
        Call(contact, "ResetContact", true);
        contact.GetType().GetField("IsPickup").SetValue(contact, true);
        Call(contact, "OnTriggerEnter", collider);
        Assert.That(trap.activeSelf, Is.False);
        Assert.That(Get<float>(air, "CurrentAir"), Is.EqualTo(100f));
    }
}
