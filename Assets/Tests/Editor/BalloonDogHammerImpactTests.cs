using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BalloonDogHammerImpactTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject player;
    private GameObject hammer;
    private Component air;
    private Component runner;
    private Component contact;
    private Rigidbody body;
    private CapsuleCollider collider;
    private float previousFixedStep;

    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(Component target, string name, params object[] args) =>
        target.GetType().GetMethod(name, BindingFlags.Public | Private).Invoke(target, args);
    private static void Set(Component target, string name, object value) =>
        target.GetType().GetField(name, Private).SetValue(target, value);
    private static T Get<T>(Component target, string name) =>
        (T)target.GetType().GetProperty(name).GetValue(target);

    [SetUp]
    public void SetUp()
    {
        previousFixedStep = Time.fixedDeltaTime;
        player = new GameObject("HammerImpactPlayer", typeof(Rigidbody), typeof(CapsuleCollider));
        body = player.GetComponent<Rigidbody>();
        body.useGravity = false;
        collider = player.GetComponent<CapsuleCollider>();
        air = player.AddComponent(Runtime("AirController"));
        runner = player.AddComponent(Runtime("PlayerRunner"));
        Call(air, "Awake");
        Call(runner, "Awake");
        hammer = new GameObject("HammerImpactContact");
        contact = hammer.AddComponent(Runtime("BalloonDogEndlessContact"));
        Call(contact, "ConfigureObstacle", 8);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(hammer);
        UnityEngine.Object.DestroyImmediate(player);
        Time.fixedDeltaTime = previousFixedStep;
    }

    [TestCase(100f, 20f)]
    [TestCase(50f, 10f)]
    [TestCase(10f, 2f)]
    public void HammerTakesEightyPercentOfCurrentAirAndStaysInTheWorld(float before, float after)
    {
        Call(air, "RemoveAir", 100f - before);
        body.linearVelocity = new Vector3(2f, -1f, 13f);
        Call(contact, "ApplyVisibleHit", collider);
        Assert.That(Get<float>(air, "CurrentAir"), Is.EqualTo(after).Within(.0001f));
        Assert.That(hammer.activeSelf, Is.True);
        Assert.That(body.linearVelocity.x, Is.EqualTo(2f));
        Assert.That(body.linearVelocity.y, Is.EqualTo(-1f));
        Assert.That(body.linearVelocity.z, Is.EqualTo(-2f / .3f).Within(.0001f));
        Assert.That(Get<bool>(runner, "MovementEnabled"), Is.True,
            "Recoil must keep the endless road and gameplay running.");

        // Multiple player colliders and trigger-stay frames share one owner.
        var child = new GameObject("SecondPlayerCollider", typeof(BoxCollider));
        child.transform.SetParent(player.transform, false);
        Call(contact, "ApplyVisibleHit", child.GetComponent<BoxCollider>());
        for (int i = 0; i < 20; i++) Call(contact, "ApplyVisibleHit", collider);
        Assert.That(Get<float>(air, "CurrentAir"), Is.EqualTo(after).Within(.0001f));
        Call(contact, "ResetContact", false);
        Call(contact, "ResetContact", true);
        Call(contact, "ApplyVisibleHit", collider);
        Assert.That(Get<float>(air, "CurrentAir"), Is.EqualTo(after * .2f).Within(.0001f),
            "Recycling must restore damage for the next pass.");
    }

    [Test]
    public void ContinueProtectionRejectsDamageAndRecoilWithoutConsumingTheContact()
    {
        Call(air, "RestoreAfterContinue");
        body.linearVelocity = Vector3.forward * 13f;
        Call(contact, "ApplyVisibleHit", collider);
        Assert.That(Get<float>(air, "CurrentAir"), Is.EqualTo(100f));
        Assert.That(body.linearVelocity.z, Is.EqualTo(13f));
        Assert.That(hammer.activeSelf, Is.True);
        Set(air, "continueProtectionUntil", Time.time - 1f);
        Call(contact, "ApplyVisibleHit", collider);
        Assert.That(Get<float>(air, "CurrentAir"), Is.EqualTo(20f).Within(.0001f));
        Assert.That(body.linearVelocity.z, Is.EqualTo(-2f / .3f).Within(.0001f));
    }

    [TestCase(30)]
    [TestCase(50)]
    [TestCase(120)]
    public void RecoilSurvivesFixedUpdatesThenResumesTheLatestRunSpeed(int frequency)
    {
        Time.fixedDeltaTime = 1f / frequency;
        body.linearVelocity = new Vector3(2f, -1f, 5f);
        Call(runner, "ApplyKnockbackDistance", 2f, .3f);
        float distance = 0f;
        int backwardsSteps = 0;
        for (int i = 0; i < frequency; i++)
        {
            Call(runner, "ConfigureForwardSpeed", 13f);
            Call(runner, "FixedUpdate");
            if (body.linearVelocity.z < 0f)
            {
                backwardsSteps++;
                distance -= body.linearVelocity.z * Time.fixedDeltaTime;
            }
            Assert.That(body.linearVelocity.x, Is.EqualTo(2f));
            Assert.That(body.linearVelocity.y, Is.EqualTo(-1f));
        }
        Assert.That(backwardsSteps, Is.GreaterThan(1));
        Assert.That(distance, Is.EqualTo(2f).Within(.0001f));
        Assert.That(body.linearVelocity.z, Is.EqualTo(13f));
        Assert.That(Get<bool>(runner, "MovementEnabled"), Is.True);
    }

    [TestCase("stop")]
    [TestCase("continue")]
    [TestCase("disable")]
    public void RestartingMovementDoesNotReplayAnOldKnockback(string transition)
    {
        Call(runner, "ApplyKnockback", 6f, .3f);
        if (transition == "stop")
        {
            Call(runner, "SetMovementEnabled", false);
            Call(runner, "ApplyKnockback", 6f, .3f);
            Call(runner, "SetMovementEnabled", true);
        }
        else Call(runner, transition == "continue" ? "RestoreAfterContinue" : "OnDisable");
        Call(runner, "ConfigureForwardSpeed", 13f);
        Call(runner, "FixedUpdate");
        Assert.That(body.linearVelocity.z, Is.EqualTo(13f));
    }

    [Test]
    public void RecoilAndOriginShiftsCannotLowerScoreOrAwardTheSameMetresTwice()
    {
        Component score = player.AddComponent(Runtime("ScoreController"));
        Call(score, "Awake");
        player.transform.position = Vector3.forward * 1200f;
        Assert.That(Get<int>(score, "CurrentScore"), Is.EqualTo(12000));
        player.transform.position -= Vector3.forward * 2f;
        Assert.That(Get<int>(score, "CurrentScore"), Is.EqualTo(12000));
        player.transform.position -= Vector3.forward * 896f;
        Call(score, "ShiftOrigin", 896f);
        Assert.That(Get<double>(score, "DistanceTravelled"), Is.EqualTo(1200d));
        player.transform.position += Vector3.forward * 2f;
        Assert.That(Get<int>(score, "CurrentScore"), Is.EqualTo(12000));
        Call(score, "AddBonus", 100);
        player.transform.position += Vector3.forward;
        Assert.That(Get<int>(score, "CurrentScore"), Is.EqualTo(12110));
    }
}
