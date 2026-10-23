using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class BalloonDogDeathAnimationTests
{
    private GameObject actor, host, visual, inactiveSkin;
    private Component animation;
    private Renderer source;
    private float previousTimeScale;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(Component target, string method, params object[] args) =>
        target.GetType().GetMethod(method).Invoke(target, args);
    private bool Playing => (bool)animation.GetType().GetProperty("IsPlaying").GetValue(animation);

    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        actor = new GameObject("DeathTestPlayer", typeof(Rigidbody));
        actor.GetComponent<Rigidbody>().isKinematic = true;
        actor.transform.position = new Vector3(1f, 2f, 10f);
        visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "ActiveSkin";
        visual.transform.SetParent(actor.transform, false);
        visual.transform.localPosition = new Vector3(.2f, .5f, 0f);
        visual.transform.localScale = new Vector3(.7f, 1.1f, .5f);
        source = visual.GetComponent<Renderer>();
        var tint = new MaterialPropertyBlock();
        tint.SetColor("_BaseColor", Color.green);
        source.SetPropertyBlock(tint);
        inactiveSkin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        inactiveSkin.transform.SetParent(actor.transform, false);
        inactiveSkin.GetComponent<Renderer>().forceRenderingOff = true;
        inactiveSkin.SetActive(false);
        host = new GameObject("DeathAnimationTest");
        animation = host.AddComponent(Runtime("BalloonDogDeathAnimation"));
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(host);
        UnityEngine.Object.DestroyImmediate(actor);
        Time.timeScale = previousTimeScale;
    }

    [Test]
    public void DeathRunsAtZeroTimeScaleAndRestoresTheUntouchedSkinAfterContinue()
    {
        Vector3 playerPosition = actor.transform.position;
        Vector3 skinPosition = visual.transform.localPosition;
        Vector3 skinScale = visual.transform.localScale;
        Mesh originalMesh = visual.GetComponent<MeshFilter>().sharedMesh;
        int completed = 0;
        Assert.That(Call(animation, "Play", actor.transform, (Action)(() => completed++)), Is.True);
        Assert.That(source.forceRenderingOff, Is.True);
        var effects = GameObject.Find("BalloonDogDeathEffects");
        Assert.That(effects, Is.Not.Null);
        var ghost = effects.GetComponentInChildren<MeshRenderer>();
        Assert.That(ghost.sharedMaterial, Is.SameAs(source.sharedMaterial));
        var tint = new MaterialPropertyBlock();
        ghost.GetPropertyBlock(tint);
        Assert.That(tint.GetColor("_BaseColor"), Is.EqualTo(Color.green));
        Call(animation, "Advance", .1f);
        Assert.That(effects.transform.Find("DeathPose").localScale.x, Is.GreaterThan(1f));
        Call(animation, "Advance", .13f);
        Assert.That(effects.transform.Find("DeathPose").gameObject.activeSelf, Is.False);
        Assert.That(effects.transform.Find("BalloonFragment_0"), Is.Not.Null);
        Assert.That(effects.GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(effects.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
        Call(animation, "Advance", 1f);
        Call(animation, "Advance", 1f);
        Assert.That(Playing, Is.False);
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(source.forceRenderingOff, Is.True, "The dead dog stays hidden until a real revive.");
        Assert.That(actor.transform.position, Is.EqualTo(playerPosition));
        Assert.That(visual.transform.localPosition, Is.EqualTo(skinPosition));
        Assert.That(visual.transform.localScale, Is.EqualTo(skinScale));
        Call(animation, "Restore");
        Assert.That(source.forceRenderingOff, Is.False);
        Assert.That(visual.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(originalMesh));
        Assert.That(inactiveSkin.activeSelf, Is.False);
        Assert.That(inactiveSkin.GetComponent<Renderer>().forceRenderingOff, Is.True);
        Assert.That(GameObject.Find("BalloonDogDeathEffects"), Is.Null);
    }

    [Test]
    public void CancellingTheAnimationCannotOpenAStaleOfferAndASecondDeathWorks()
    {
        int completed = 0;
        Action callback = () => completed++;
        Call(animation, "Play", actor.transform, callback);
        Call(animation, "Advance", .1f);
        Call(animation, "Restore");
        Call(animation, "Advance", 1f);
        Assert.That(completed, Is.Zero);
        Assert.That(source.forceRenderingOff, Is.False);
        Call(animation, "Play", actor.transform, callback);
        Call(animation, "Advance", .65f);
        Assert.That(completed, Is.EqualTo(1));
        Call(animation, "Restore");
        Assert.That(source.forceRenderingOff, Is.False);
    }

    [Test]
    public void MissingVisibleGeometryFallsBackWithoutLeavingThePlayerHidden()
    {
        visual.SetActive(false);
        Assert.That(Call(animation, "Play", actor.transform, (Action)(() => { })), Is.False);
        Assert.That(Playing, Is.False);
        Assert.That(source.forceRenderingOff, Is.False);
        Assert.That(GameObject.Find("BalloonDogDeathEffects"), Is.Null);
    }
}
