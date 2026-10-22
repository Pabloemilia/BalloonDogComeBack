using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class BalloonDogMenuShowcaseTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private GameObject menu;
    private GameObject first;
    private GameObject second;
    private Component switcher;
    private Component showcase;
    private Material original;
    private float savedTimeScale;

    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        original = new Material(Resources.Load<Shader>("MenuRefresh/MenuStudioBalloon"));
        original.SetColor("_BaseColor", Color.red);
        first = GameObject.CreatePrimitive(PrimitiveType.Cube);
        first.name = "FirstEquippedSkin";
        first.GetComponent<Renderer>().sharedMaterial = original;
        second = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        second.name = "SecondEquippedSkin";
        second.GetComponent<Renderer>().sharedMaterial = original;
        GameObject player = new GameObject("SkinSelectionSource");
        switcher = player.AddComponent(Runtime("BalloonDogSkinSwitcher"));
        Field(switcher, "currentSkinIndex").SetValue(switcher, 1);
        Field(switcher, "activeSkinInstance").SetValue(switcher, first);
        menu = new GameObject("MenuShowcaseTest", typeof(RectTransform));
        menu.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 2348);
        showcase = menu.AddComponent(Runtime("BalloonDogMenuShowcase"));
        Tick();
    }

    [TearDown]
    public void TearDown()
    {
        if (menu != null) UnityEngine.Object.DestroyImmediate(menu);
        if (original != null) UnityEngine.Object.DestroyImmediate(original);
        Time.timeScale = savedTimeScale;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static FieldInfo Field(Component component, string name) => component.GetType().GetField(name, Private);
    private void Tick()
    {
        Field(showcase, "nextRetry").SetValue(showcase, 0f);
        showcase.GetType().GetMethod("LateUpdate", Private).Invoke(showcase, null);
    }

    [Test]
    public void SkinSelectionChangesTheActualMeshWhileTheMenuIsPaused()
    {
        Assert.That(Field(showcase, "displayedVisual").GetValue(showcase), Is.SameAs(first));
        Field(switcher, "activeSkinInstance").SetValue(switcher, second);
        Action<int, GameObject> changed = (Action<int, GameObject>)Field(switcher, "SkinChanged").GetValue(switcher);
        Assert.That(changed, Is.Not.Null);
        changed(2, second);
        Tick();
        Transform dog = (Transform)Field(showcase, "dog").GetValue(showcase);
        Assert.That(dog.GetComponentInChildren<MeshFilter>().sharedMesh,
            Is.SameAs(second.GetComponent<MeshFilter>().sharedMesh));
        Assert.That(Field(showcase, "displayedVisual").GetValue(showcase), Is.SameAs(second));
        Assert.That(Time.timeScale, Is.Zero);
    }

    [Test]
    public void PreviewCopiesSkinPropertiesWithoutMutatingGameplayMaterialsOrPhysics()
    {
        Vector3 position = first.transform.position;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", Color.green);
        first.GetComponent<Renderer>().SetPropertyBlock(block);
        showcase.GetType().GetMethod("MarkDirty", Private).Invoke(showcase, null);
        Tick();
        Transform dog = (Transform)Field(showcase, "dog").GetValue(showcase);
        Material preview = dog.GetComponentInChildren<Renderer>().sharedMaterial;
        Assert.That(preview, Is.Not.SameAs(original));
        Assert.That(preview.GetColor("_BaseColor"), Is.EqualTo(Color.green));
        Assert.That(original.GetColor("_BaseColor"), Is.EqualTo(Color.red));
        Assert.That(first.transform.position, Is.EqualTo(position));
        Assert.That(first.GetComponent<Collider>().enabled, Is.True);
        Assert.That(dog.GetComponentsInChildren<Collider>(), Is.Empty);
        Assert.That(dog.GetComponentsInChildren<MonoBehaviour>(), Is.Empty);
    }

    [Test]
    public void ClosingTheMenuReleasesThePreviewAndReopeningRestoresIt()
    {
        RenderTexture texture = (RenderTexture)Field(showcase, "target").GetValue(showcase);
        GameObject studio = (GameObject)Field(showcase, "stage").GetValue(showcase);
        Assert.That(texture, Is.Not.Null);
        menu.SetActive(false);
        Assert.That(texture == null, Is.True, "The owned texture must be destroyed on close.");
        Assert.That(studio == null, Is.True, "The extra camera and its 3D scene must be destroyed on close.");
        menu.SetActive(true);
        Tick();
        Assert.That(Field(showcase, "stage").GetValue(showcase), Is.Not.Null);
        Assert.That(Field(showcase, "displayedVisual").GetValue(showcase), Is.SameAs(first));
    }

    [Test]
    public void TheCloudPlateAndStudioShaderAreAvailableInPlayerResources()
    {
        Assert.That(Resources.Load<Sprite>("MenuRefresh/MenuCloudSky"), Is.Not.Null);
        Assert.That(Resources.Load<Shader>("MenuRefresh/MenuStudioBalloon"), Is.Not.Null);
    }
}
