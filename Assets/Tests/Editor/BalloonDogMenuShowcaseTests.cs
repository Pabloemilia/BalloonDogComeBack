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
    public void MenuKeepsADisplayCameraAndRestoresItsOriginalState()
    {
        Camera displayCamera = new GameObject("GameplayCamera", typeof(Camera)).GetComponent<Camera>();
        displayCamera.tag = "MainCamera";
        displayCamera.enabled = true;
        int originalMask = displayCamera.cullingMask;

        showcase.GetType().GetMethod("PrepareWorldCamera", Private).Invoke(showcase, null);
        Assert.That(displayCamera.enabled, Is.True, "The menu must keep a camera rendering to Display 1.");
        Assert.That(displayCamera.targetTexture, Is.Null, "The display camera must not render only to a texture.");
        Assert.That(displayCamera.cullingMask, Is.Zero, "Gameplay geometry should stay hidden behind the menu.");
        Camera studio = (Camera)Field(showcase, "studioCamera").GetValue(showcase);
        Assert.That(studio.targetTexture, Is.Not.Null, "The live skin preview still uses its separate texture.");

        System.Collections.IEnumerator leave = (System.Collections.IEnumerator)
            showcase.GetType().GetMethod("LeaveMenu").Invoke(showcase, null);
        leave.MoveNext();
        Assert.That(displayCamera.enabled, Is.True);
        Assert.That(displayCamera.cullingMask, Is.EqualTo(originalMask));

        // A later disable must not overwrite changes made after PLAY restored the camera.
        displayCamera.cullingMask = 123;
        menu.SetActive(false);
        Assert.That(displayCamera.cullingMask, Is.EqualTo(123));
        ((IDisposable)leave).Dispose();
    }

    [Test]
    public void TheCloudPlateAndStudioShaderAreAvailableInPlayerResources()
    {
        Assert.That(Resources.Load<Sprite>("MenuRefresh/MenuCloudSky"), Is.Not.Null);
        Assert.That(Resources.Load<Shader>("MenuRefresh/MenuStudioBalloon"), Is.Not.Null);
        Assert.That(Resources.Load<Texture2D>("MenuRefresh/MenuPlatform"), Is.Not.Null);
        Assert.That(Resources.Load<Shader>("MenuRefresh/MenuPlatformArtwork"), Is.Not.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ApprovedPlatformReplacesTheCylindersAndItsSurfaceAlignsWithTheDogFeet(bool compact)
    {
        menu.SetActive(false);
        showcase.GetType().GetProperty("CompactPreview").SetValue(showcase, compact);
        menu.SetActive(true);
        Tick();
        GameObject stage = (GameObject)Field(showcase, "stage").GetValue(showcase);
        Camera camera = (Camera)Field(showcase, "studioCamera").GetValue(showcase);
        Transform platform = stage.transform.Find("ApprovedPlatformArtwork");
        Assert.That(platform, Is.Not.Null);
        Assert.That(stage.transform.Find("WhitePedestal"), Is.Null);
        Assert.That(stage.transform.Find("CyanRim"), Is.Null);
        Assert.That(stage.transform.Find("CyanTop"), Is.Null);
        Assert.That(stage.GetComponentsInChildren<Collider>(), Is.Empty);
        Material material = platform.GetComponent<Renderer>().sharedMaterial;
        Texture2D artwork = Resources.Load<Texture2D>("MenuRefresh/MenuPlatform");
        Assert.That(material.mainTexture, Is.SameAs(artwork));
        Assert.That(material.shader.name, Is.EqualTo("BalloonDog/MenuPlatformArtwork"));
        Mesh mesh = platform.GetComponent<MeshFilter>().sharedMesh;
        Assert.That(mesh.bounds.size.x, Is.EqualTo(4.5f * (compact ? .52f : 1f)).Within(.001f));
        Assert.That(mesh.bounds.size.y / mesh.bounds.size.x,
            Is.EqualTo((float)artwork.height / artwork.width).Within(.001f));
        Vector3 feet = stage.transform.position + Vector3.up * .075f;
        Vector3 surface = platform.TransformPoint(Vector3.up * (mesh.bounds.size.y * .10f));
        Vector3 feetView = camera.WorldToViewportPoint(feet);
        Vector3 surfaceView = camera.WorldToViewportPoint(surface);
        Assert.That(surfaceView.x, Is.EqualTo(feetView.x).Within(.002f));
        Assert.That(surfaceView.y, Is.EqualTo(feetView.y).Within(.002f));
        Assert.That(surfaceView.z, Is.GreaterThan(feetView.z + 5f),
            "The painted plate must stay behind the live dog instead of covering its feet.");
        foreach (Vector3 vertex in mesh.vertices)
        {
            Vector3 screen = camera.WorldToViewportPoint(platform.TransformPoint(vertex));
            Assert.That(screen.x, Is.InRange(0f, 1f));
            Assert.That(screen.y, Is.InRange(0f, 1f));
        }
        Quaternion rotation = platform.rotation;
        Tick();
        Assert.That(platform.rotation, Is.EqualTo(rotation), "The drawing must not rotate with the dog.");
        menu.SetActive(false);
        Assert.That(mesh == null, Is.True, "Closing the preview must release its generated plate mesh.");
    }
}
