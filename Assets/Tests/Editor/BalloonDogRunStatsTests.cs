using System;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public sealed class BalloonDogRunStatsTests
{
    private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
    private GameObject root;
    private Type ui;

    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        root = new GameObject("RunStatsTest", typeof(RectTransform));
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(640f, 200f);
        ui = Type.GetType("BalloonDogModernUI, Assembly-CSharp", true);
    }

    [TearDown]
    public void TearDown() => UnityEngine.Object.DestroyImmediate(root);

    private Component Counter(string resource = "run_score_counter", float top = 0f) =>
        (Component)ui.GetMethod("CreateRunStatCard", PrivateStatic).Invoke(null,
            new object[] { root.transform, resource, "MenuRefresh/" + resource, top });

    private void Set(Component label, int value) =>
        ui.GetMethod("SetRunStatValue", PrivateStatic).Invoke(null, new object[] { label, value });

    private static T Get<T>(Component label, string property) =>
        (T)label.GetType().GetProperty(property).GetValue(label);

    [TestCase(0)]
    [TestCase(9)]
    [TestCase(999)]
    [TestCase(9999)]
    [TestCase(999999)]
    [TestCase(999999999)]
    [TestCase(int.MaxValue)]
    public void FullNumbersFitWithoutShrinkingTheFontOrLeavingTheSafeArea(int value)
    {
        Component label = Counter();
        Set(label, value);
        string text = Get<string>(label, "text");
        Assert.That(text, Is.EqualTo(value.ToString("N0", CultureInfo.InvariantCulture)));
        Assert.That(Get<float>(label, "fontSize"), Is.EqualTo(38f));
        Assert.That(Get<bool>(label, "enableAutoSizing"), Is.False);
        var preferred = (Vector2)label.GetType().GetMethod("GetPreferredValues",
            new[] { typeof(string), typeof(float), typeof(float) })
            .Invoke(label, new object[] { text, float.PositiveInfinity, 68f });
        Assert.That(((RectTransform)label.transform).rect.width, Is.GreaterThanOrEqualTo(preferred.x));
        var card = (RectTransform)label.transform.parent;
        Assert.That(card.rect.width, Is.LessThanOrEqualTo(640f));
        Assert.That(card.rect.height, Is.EqualTo(68f));
        Assert.That(card.pivot.x, Is.Zero);
        Assert.That(card.anchoredPosition.x, Is.Zero);
    }

    [Test]
    public void StackedCountersGrowIndependentlyAndDigitChangesDoNotMakeTheBarJitter()
    {
        Component score = Counter();
        Component coins = Counter("run_coin_counter", -82f);
        var scoreCard = (RectTransform)score.transform.parent;
        var coinCard = (RectTransform)coins.transform.parent;
        float previous = 0f;
        foreach (int value in new[] { 0, 9, 10, 999, 9999, 1111, int.MaxValue })
        {
            Set(score, value);
            Assert.That(scoreCard.rect.width, Is.GreaterThanOrEqualTo(previous));
            previous = scoreCard.rect.width;
        }
        Assert.That(coinCard.rect.width, Is.EqualTo(170f));
        Assert.That(coinCard.anchoredPosition.y + coinCard.rect.height,
            Is.LessThan(scoreCard.anchoredPosition.y));
        Set(coins, int.MaxValue);
        Assert.That(scoreCard.anchoredPosition.x, Is.EqualTo(coinCard.anchoredPosition.x));
        foreach (var card in new[] { scoreCard, coinCard })
        {
            Image image = card.GetComponent<Image>();
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(image.sprite.border.x, Is.EqualTo(232f));
            Assert.That(image.sprite.border.z, Is.EqualTo(84f));
            Assert.That(image.pixelsPerUnitMultiplier, Is.EqualTo(192f / 68f).Within(.001f));
            Assert.That(image.raycastTarget, Is.False);
        }
    }
}
