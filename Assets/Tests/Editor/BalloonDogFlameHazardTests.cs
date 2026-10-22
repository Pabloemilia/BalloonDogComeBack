using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

public sealed class BalloonDogFlameHazardTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(Component target, string name, params object[] args) =>
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);
    private GameObject player;
    private GameObject trap;
    private Component air;
    private Component hazard;
    private float previousTimeScale;

    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        previousTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        player = new GameObject("FlameContactPlayer", typeof(CapsuleCollider));
        air = player.AddComponent(Runtime("AirController"));
        Call(air, "Awake");
        trap = new GameObject("FlameContactTest");
        hazard = trap.AddComponent(Runtime("BalloonDogFlameHazard"));
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(trap);
        UnityEngine.Object.DestroyImmediate(player);
        Time.timeScale = previousTimeScale;
    }

    [TestCase(100f)]
    [TestCase(50f)]
    [TestCase(1f)]
    public void FlameContactEmptiesAllRemainingAirWithoutRemovingTheTrap(float remaining)
    {
        Call(air, "RemoveAir", 100f - remaining);
        LogAssert.Expect(LogType.Error, "AirController: GameManager bulunamadı.");
        Call(hazard, "CheckContact", player.GetComponent<Collider>());
        Assert.That((bool)air.GetType().GetProperty("IsEmpty").GetValue(air), Is.True);
        Assert.That(trap.activeSelf, Is.True);
        // Repeated overlap cannot trigger another death.
        Call(hazard, "CheckContact", player.GetComponent<Collider>());
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void ProtectionPauseAndSensorTriggersAreSafe()
    {
        var collider = player.GetComponent<Collider>();
        collider.isTrigger = true;
        Call(hazard, "CheckContact", collider);
        collider.isTrigger = false;
        Time.timeScale = 0f;
        Call(hazard, "CheckContact", collider);
        Time.timeScale = 1f;
        Call(air, "RestoreAfterContinue");
        Call(hazard, "CheckContact", collider);
        Assert.That((float)air.GetType().GetProperty("CurrentAir").GetValue(air), Is.EqualTo(100f));
    }

    [TestCase(0f, 0f, false)]
    [TestCase(.1f, 0f, true)]
    [TestCase(3f, 0f, true)]
    [TestCase(3f, .2f, true)]
    [TestCase(3f, .45f, false)]
    [TestCase(3f, 2f, false)]
    public void JetGrowsThenItsTailClearsInsteadOfLeavingInvisibleDamage(float oldest, float youngest, bool active)
    {
        object[] args = { oldest, youngest, 0f, 0f };
        bool result = (bool)Runtime("BalloonDogFlameHazard").GetMethod("TryGetAgeRange", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, args);
        Assert.That(result, Is.EqualTo(active));
        if (!result) return;
        Assert.That((float)args[2], Is.GreaterThanOrEqualTo(youngest));
        Assert.That((float)args[3], Is.LessThanOrEqualTo(.45f));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void EveryFlamethrowerVariantGetsItsOwnHazardAndPoolReuseResetsEmission(int variant)
    {
        UnityEngine.Object.DestroyImmediate(trap);
        string resource = "Endless/Trap28" + (variant == 1 ? "" : "V" + variant);
        trap = UnityEngine.Object.Instantiate(Resources.Load<GameObject>(resource));
        Runtime("BalloonDogFlameHazard").GetMethod("Install").Invoke(null, new object[] { trap });
        Component timer = trap.GetComponentInChildren(Runtime("FlameControl"), true);
        Assert.That(timer, Is.Not.Null);
        Call(timer, "OnEnable");
        Assert.That((bool)timer.GetType().GetProperty("IsEmitting").GetValue(timer), Is.True);
        // The first 1s callback preserves the original startup emission; then 2s alternation.
        Call(timer, "TurnOnOffFire");
        Call(timer, "TurnOnOffFire");
        Assert.That((bool)timer.GetType().GetProperty("IsEmitting").GetValue(timer), Is.False);
        Call(timer, "OnDisable");
        Call(timer, "OnEnable");
        Assert.That((bool)timer.GetType().GetProperty("IsEmitting").GetValue(timer), Is.True);
        Assert.That((float)timer.GetType().GetProperty("EmissionStartedAt").GetValue(timer), Is.EqualTo(Time.time));
        foreach (VisualEffect flame in trap.GetComponentsInChildren<VisualEffect>(true))
        {
            Assert.That(flame.visualEffectAsset.name, Is.EqualTo("Flamethrower"));
            Component installed = flame.GetComponent(Runtime("BalloonDogFlameHazard"));
            Assert.That(installed, Is.Not.Null);
            Assert.That(installed.GetType().GetField("source", Private).GetValue(installed), Is.SameAs(timer));
            timer.GetType().GetField("<EmissionStartedAt>k__BackingField", Private).SetValue(timer, Time.time - .3f);
            trap.transform.SetPositionAndRotation(new Vector3(-2.762f, 0f, 1100f), Quaternion.Euler(0f, 90f, 0f));
            trap.transform.localScale = new Vector3(1.5f, 1.3f, 2f);
            object[] jet = { Vector3.zero, Vector3.zero, 0f };
            MethodInfo geometry = installed.GetType().GetMethod("TryGetJet", Private);
            Assert.That((bool)geometry.Invoke(installed, jet), Is.True);
            Vector3 beforeShift = (Vector3)jet[0];
            Vector3 direction = flame.transform.TransformVector(Vector3.forward).normalized;
            Assert.That(Vector3.Dot(((Vector3)jet[1] - beforeShift).normalized, direction), Is.GreaterThan(.9999f));
            Assert.That((float)jet[2], Is.GreaterThan(0f));
            trap.transform.position -= Vector3.forward * 896f;
            Assert.That((bool)geometry.Invoke(installed, jet), Is.True);
            Assert.That(Vector3.Distance((Vector3)jet[0], beforeShift - Vector3.forward * 896f), Is.LessThan(.001f));
            flame.enabled = false;
            Assert.That((bool)geometry.Invoke(installed, jet), Is.False, "Hidden fire must never leave an active jet.");
        }
    }
}
