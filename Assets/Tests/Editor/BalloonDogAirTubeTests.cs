using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public sealed class BalloonDogAirTubeTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject root;
    private Component hud;
    private RectTransform card;
    private Image fill;
    private Image trail;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);

    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        root = new GameObject("AirTubeTestCanvas", typeof(RectTransform));
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(1080f, 2348f);
        Runtime("BalloonDogModernUI").GetMethod("BuildAirMeter", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { root.transform });
        card = root.transform.Find("GameplayAirHud") as RectTransform;
        hud = card.GetComponent(Runtime("BalloonDogAirHud"));
        fill = card.Find("AirTrack/AirFill").GetComponent<Image>();
        trail = card.Find("AirTrack/AirDamageTrail").GetComponent<Image>();
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(root);
    }

    private void Set(string name, float value) => hud.GetType().GetField(name, Private).SetValue(hud, value);
    private void Draw() => hud.GetType().GetMethod("Draw", Private).Invoke(hud, null);

    [Test]
    public void AirHudRestoresItsOriginalSizeAndLoadsOnlyTheTubeArtwork()
    {
        Assert.That(card.localScale, Is.EqualTo(Vector3.one));
        Assert.That(card.rect.width, Is.EqualTo(896f).Within(.01f));
        Assert.That(card.rect.height, Is.EqualTo(128f).Within(.01f));
        Assert.That(fill.sprite, Is.Not.Null);
        Assert.That(fill.sprite.rect.size, Is.EqualTo(new Vector2(1654f, 130f)),
            "Only the tube is sampled; the preview's baked 95% label must never appear in game.");
        Assert.That(fill.sprite.texture.width, Is.EqualTo(2172), "Importer must preserve native atlas crop coordinates.");
        Assert.That(fill.raycastTarget, Is.False);
        Assert.That(trail.raycastTarget, Is.False);
        Assert.That(card.Find("AirWindIcon").GetComponent<Image>().raycastTarget, Is.False);
    }

    [TestCase(.95f)]
    [TestCase(.501f)]
    [TestCase(.499f)]
    [TestCase(.1429f)]
    [TestCase(.1428f)]
    [TestCase(.01f)]
    public void FractionalAirClipsTheActualMeshWithoutResizingOrRoundingToChambers(float amount)
    {
        Set("target", amount);
        Set("displayed", amount);
        Set("trailing", amount);
        float fullWidth = fill.rectTransform.rect.width;
        Draw();
        Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));
        Assert.That(fill.fillMethod, Is.EqualTo(Image.FillMethod.Horizontal));
        Assert.That(fill.fillOrigin, Is.EqualTo((int)Image.OriginHorizontal.Left));
        Assert.That(fill.fillAmount, Is.EqualTo(amount).Within(.000001f));
        Assert.That(fill.rectTransform.rect.width, Is.EqualTo(fullWidth), "The chamber artwork must not compress.");
        using (var mesh = new VertexHelper())
        {
            typeof(Image).GetMethod("OnPopulateMesh", Private, null, new[] { typeof(VertexHelper) }, null)
                .Invoke(fill, new object[] { mesh });
            Assert.That(mesh.currentVertCount, Is.GreaterThan(0));
            float right = float.MinValue;
            UIVertex vertex = default;
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                right = Mathf.Max(right, vertex.position.x);
            }
            Assert.That(right, Is.EqualTo(fill.rectTransform.rect.xMin + fullWidth * amount).Within(.02f));
        }
    }

    [Test]
    public void DamageTrailAndLiveLowAirPercentageRemainIndependentOfTheArtwork()
    {
        Set("target", .2f);
        Set("displayed", .22f);
        Set("trailing", .6f);
        Draw();
        Assert.That(fill.fillAmount, Is.EqualTo(.22f).Within(.0001f));
        Assert.That(trail.fillAmount, Is.EqualTo(.6f).Within(.0001f));
        Component value = (Component)hud.GetType().GetField("percentage", Private).GetValue(hud);
        Component status = (Component)hud.GetType().GetField("status", Private).GetValue(hud);
        Assert.That(value.GetType().GetProperty("text").GetValue(value), Is.EqualTo("20%"));
        Assert.That(status.GetType().GetProperty("text").GetValue(status), Is.EqualTo("LOW AIR"));
        Set("target", 0f);
        Set("displayed", 0f);
        Set("trailing", 0f);
        Draw();
        Assert.That(fill.enabled, Is.False);
        Assert.That(trail.enabled, Is.False);
        Assert.That(value.GetType().GetProperty("text").GetValue(value), Is.EqualTo("0%"));
    }
}
