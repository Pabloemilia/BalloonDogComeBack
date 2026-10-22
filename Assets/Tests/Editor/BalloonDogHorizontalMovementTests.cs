using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BalloonDogHorizontalMovementTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject player;
    private Rigidbody body;
    private Component movement;
    private Type type;
    private float previousTimeScale;

    [SetUp]
    public void SetUp()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        player = new GameObject("HorizontalMovementRegression", typeof(Rigidbody));
        body = player.GetComponent<Rigidbody>();
        body.useGravity = false;
        type = Type.GetType("PlayerHorizontalController, Assembly-CSharp", true);
        movement = player.AddComponent(type);
        type.GetMethod("Awake", Private).Invoke(movement, null);
        type.GetMethod("ConfigureRoadLimit").Invoke(movement, new object[] { 2.4f });
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(player);
        Time.timeScale = previousTimeScale;
    }

    private void SetTarget(float x) => type.GetField("targetX", Private).SetValue(movement, x);
    private float Speed(float x, float velocity, float dt) => (float)type
        .GetMethod("CalculateHorizontalSpeed", Private).Invoke(movement, new object[] { x, velocity, dt });

    [TestCase(30)]
    [TestCase(50)]
    [TestCase(120)]
    public void FastApproachesAndRepeatedTurnsStayWithinTargetAndRoad(int frequency)
    {
        float dt = 1f / frequency;
        float x = 1.19f;
        float velocity = 17f;
        foreach (float target in new[] { 1.2f, -1.2f, 2.4f, -2.4f, 0f })
        {
            SetTarget(target);
            float from = x;
            for (int frame = 0; frame < frequency * 2; frame++)
            {
                float before = x;
                velocity = Speed(x, velocity, dt);
                x += velocity * dt;
                Assert.That(x, Is.InRange(Mathf.Min(from, target) - .0001f, Mathf.Max(from, target) + .0001f),
                    "The dog must never slide beyond its destination.");
                Assert.That(x, Is.InRange(-2.4001f, 2.4001f));
                Assert.That(Mathf.Abs(target - x), Is.LessThanOrEqualTo(Mathf.Abs(target - before) + .0001f));
            }
            Assert.That(x, Is.EqualTo(target).Within(.0011f));
            Assert.That(velocity, Is.Zero);
        }
    }

    [Test]
    public void ReversingDirectionDoesNotKeepDriftingTheOldWay()
    {
        SetTarget(-1f);
        Assert.That(Speed(0f, 17f, .02f), Is.LessThan(0f));
        SetTarget(1f);
        Assert.That(Speed(0f, -17f, .02f), Is.GreaterThan(0f));
        SetTarget(0f);
        Assert.That(Speed(0f, 17f, .02f), Is.Zero);
    }

    [Test]
    public void ReleasingInputDiscardsTheQueuedTargetWithoutChangingForwardOrVerticalSpeed()
    {
        body.position = new Vector3(1.25f, 1f, 100f);
        body.linearVelocity = new Vector3(12f, -2f, 13f);
        SetTarget(2.4f);
        type.GetMethod("StopHorizontalMotion", Private).Invoke(movement, null);
        Assert.That((float)type.GetField("targetX", Private).GetValue(movement), Is.EqualTo(1.25f));
        Assert.That(body.linearVelocity, Is.EqualTo(new Vector3(0f, -2f, 13f)));
        Assert.That(Speed(body.position.x, 0f, .02f), Is.Zero);
    }

    [Test]
    public void PauseFocusLossAndReenableDoNotReplayOldDragInput()
    {
        body.position = new Vector3(-1f, 1f, 100f);
        foreach (string transition in new[] { "pause", "focus", "disable", "enable" })
        {
            SetTarget(2.4f);
            body.linearVelocity = new Vector3(12f, 0f, 13f);
            type.GetField("wasPointerPressed", Private).SetValue(movement, true);
            type.GetField("wasKeyboardPressed", Private).SetValue(movement, true);
            if (transition == "pause")
            {
                Time.timeScale = 0f;
                type.GetMethod("Update", Private).Invoke(movement, null);
                Time.timeScale = 1f;
            }
            else if (transition == "focus")
                type.GetMethod("OnApplicationFocus", Private).Invoke(movement, new object[] { false });
            else
                type.GetMethod(transition == "disable" ? "OnDisable" : "OnEnable", Private).Invoke(movement, null);
            Assert.That(body.linearVelocity.x, Is.Zero, transition);
            Assert.That(body.linearVelocity.z, Is.EqualTo(13f), transition);
            Assert.That((float)type.GetField("targetX", Private).GetValue(movement), Is.EqualTo(-1f), transition);
            Assert.That((bool)type.GetField("wasPointerPressed", Private).GetValue(movement), Is.False, transition);
            Assert.That((bool)type.GetField("wasKeyboardPressed", Private).GetValue(movement), Is.False, transition);
        }
    }
}
