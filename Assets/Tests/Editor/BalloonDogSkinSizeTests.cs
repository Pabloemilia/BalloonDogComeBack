using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class BalloonDogSkinSizeTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject player;
    private GameObject starter;
    private Transform skinRoot;
    private Component size;
    private Component air;
    private Component switcher;
    private CapsuleCollider capsule;
    private float savedTimeScale;

    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(Component target, string name, params object[] args) =>
        target.GetType().GetMethod(name, Private | BindingFlags.Public).Invoke(target, args);
    private static void Set(Component target, string name, object value) =>
        target.GetType().GetField(name, Private).SetValue(target, value);

    private static IEnumerable<string> ModelSkins()
    {
        Array skins = (Array)Runtime("BalloonDogEconomy").GetProperty("Skins").GetValue(null);
        foreach (object skin in skins)
        {
            string id = (string)skin.GetType().GetProperty("Id").GetValue(skin);
            if (id != "classic") yield return id;
        }
    }

    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        player = new GameObject("SkinResizePlayer", typeof(Rigidbody), typeof(CapsuleCollider));
        player.GetComponent<Rigidbody>().isKinematic = true;
        capsule = player.GetComponent<CapsuleCollider>();
        capsule.height = 1.62f;
        capsule.radius = .46f;
        Transform visual = new GameObject("BalloonDogModel").transform;
        visual.SetParent(player.transform, false);
        visual.localPosition = Vector3.down;
        visual.localScale = Vector3.one * .495f;
        visual.localRotation = Quaternion.Euler(0f, -90f, 0f);
        starter = GameObject.CreatePrimitive(PrimitiveType.Cube);
        starter.name = "default";
        UnityEngine.Object.DestroyImmediate(starter.GetComponent<Collider>());
        starter.transform.SetParent(visual, false);
        starter.transform.localPosition = Vector3.up * 1.6f;
        starter.transform.localScale = new Vector3(.9f, 3.2f, 2f);

        air = player.AddComponent(Runtime("AirController"));
        Call(air, "Awake");
        size = player.AddComponent(Runtime("BalloonSizeController"));
        Call(size, "Awake");
        switcher = player.AddComponent(Runtime("BalloonDogSkinSwitcher"));
        skinRoot = new GameObject("BalloonDog_SkinRoot").transform;
        skinRoot.SetParent(player.transform, false);
        Set(switcher, "skinRoot", skinRoot);
        Set(switcher, "starterVisual", starter);
        Set(switcher, "sizeController", size);
        Set(switcher, "playerCollider", capsule);
        object[] bounds = { starter, null };
        Assert.That((bool)Call(switcher, "TryGetBoundsRelativeToPlayer", bounds), Is.True);
        Set(switcher, "starterLocalBounds", bounds[1]);
        Set(switcher, "starterBoundsReady", true);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(player);
        Time.timeScale = savedTimeScale;
    }

    private GameObject AttachSkin(string id)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Skins/" + id + ".prefab");
        Assert.That(prefab, Is.Not.Null, id);
        Call(switcher, "LockSkinRootToPlayer");
        GameObject instance = UnityEngine.Object.Instantiate(prefab, skinRoot, false);
        instance.SetActive(true);
        Call(switcher, "FitInstanceToStarter", instance, prefab);
        Set(switcher, "activeSkinInstance", instance);
        Set(switcher, "activeSkinRenderers", instance.GetComponentsInChildren<Renderer>(true));
        starter.SetActive(false);
        return instance;
    }

    private void SetAir(float normalized)
    {
        Call(air, "ResetToFull");
        Call(air, "RemoveAir", (1f - normalized) * 100f);
        Call(size, "SnapToCurrentAir");
        // Model the physics body's lower resting height as its capsule shrinks.
        player.transform.position = new Vector3(1f, capsule.height * .5f, 10f);
        Physics.SyncTransforms();
    }

    private void Tick()
    {
        Call(switcher, "LateUpdate");
        Physics.SyncTransforms();
    }

    private static Bounds GeometryBounds(GameObject visual)
    {
        bool found = false;
        Bounds result = default;
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer) continue;
            if (!found) { result = renderer.bounds; found = true; }
            else result.Encapsulate(renderer.bounds);
        }
        Assert.That(found, Is.True, visual.name);
        return result;
    }

    private void AssertFeetGrounded(GameObject instance)
    {
        Assert.That(GeometryBounds(instance).min.y, Is.EqualTo(capsule.bounds.min.y + .03f).Within(.002f));
    }

    [TestCaseSource(nameof(ModelSkins))]
    public void EveryModelShrinksAndRefillsAtTheRedDogsRatioWithoutSinkingOrAccumulatingDrift(string id)
    {
        SetAir(1f);
        GameObject instance = AttachSkin(id);
        Tick();
        Vector3 fullSize = GeometryBounds(instance).size;
        Vector3 fittedScale = instance.transform.localScale;
        Vector3 fittedPosition = instance.transform.localPosition;
        Quaternion fittedRotation = instance.transform.localRotation;
        foreach (float airAmount in new[] { .8f, .5f, .1f, 1f, .1f, 1f })
        {
            SetAir(airAmount);
            Tick();
            float visible = (float)size.GetType().GetProperty("VisibleSizeMultiplier").GetValue(size);
            float maximum = (float)size.GetType().GetProperty("MaximumSizeMultiplier").GetValue(size);
            Vector3 expected = fullSize * (visible / maximum);
            Assert.That(Vector3.Distance(GeometryBounds(instance).size, expected), Is.LessThan(.005f), id);
            AssertFeetGrounded(instance);
            Vector3 stablePosition = skinRoot.position;
            for (int frame = 0; frame < 5; frame++) Tick();
            Assert.That(Vector3.Distance(skinRoot.position, stablePosition), Is.LessThan(.002f));
            Assert.That(instance.transform.localScale, Is.EqualTo(fittedScale));
            Assert.That(instance.transform.localPosition, Is.EqualTo(fittedPosition));
            Assert.That(instance.transform.localRotation, Is.EqualTo(fittedRotation));
        }
    }

    [TestCaseSource(nameof(ModelSkins))]
    public void EquippingAtLowAirAndPressingShrinkAlsoResizeAndGroundEveryModel(string id)
    {
        SetAir(.1f);
        GameObject instance = AttachSkin(id);
        Vector3 fittedFullSize = GeometryBounds(instance).size;
        Tick();
        Assert.That(GeometryBounds(instance).size.y, Is.EqualTo(fittedFullSize.y * .55f).Within(.005f));
        AssertFeetGrounded(instance);

        SetAir(1f);
        Tick();
        float fullHeight = GeometryBounds(instance).size.y;
        Assert.That((bool)Call(size, "TryActivateShrink"), Is.True);
        Tick();
        Assert.That(GeometryBounds(instance).size.y, Is.LessThan(fullHeight));
        AssertFeetGrounded(instance);
        SetAir(1f);
        Tick();
        Assert.That(GeometryBounds(instance).size.y, Is.EqualTo(fullHeight).Within(.005f));
        AssertFeetGrounded(instance);
    }

    [Test]
    public void HiddenMeshesAndEffectsCannotPullTheGroundedModelUpward()
    {
        SetAir(.5f);
        GameObject instance = AttachSkin("skin_caramel");
        GameObject hidden = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hidden.transform.SetParent(instance.transform, false);
        hidden.transform.localPosition = Vector3.down * 1000f;
        hidden.SetActive(false);
        LineRenderer line = new GameObject("EffectLine", typeof(LineRenderer)).GetComponent<LineRenderer>();
        line.transform.SetParent(instance.transform, false);
        line.positionCount = 2;
        line.SetPositions(new[] { Vector3.down * 1000f, Vector3.down * 999f });
        Set(switcher, "activeSkinRenderers", instance.GetComponentsInChildren<Renderer>(true));
        Tick();
        AssertFeetGrounded(instance);
    }
}
