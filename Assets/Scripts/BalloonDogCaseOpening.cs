using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Artwork-backed case UI. Uses the existing deterministic ownership economy.</summary>
public sealed class BalloonDogCaseOpening : MonoBehaviour
{
    private const string Art = "MarketUI/SkinCase/";
    private const int Cost = 300;
    private const float Step = 184f;
    private static readonly string[] Rarities = { "COMMON", "UNCOMMON", "RARE", "EPIC", "LEGENDARY" };
    private readonly List<Sprite> ownedSprites = new List<Sprite>();
    private readonly List<GameObject> cells = new List<GameObject>();
    private Sprite[] cards;
    private RectTransform reel;
    private RectTransform selector;
    private Button open;
    private TMP_Text status;
    private bool spinning;
    private bool built;
    private string resultMessage;
    private Sprite silhouette;

    public void Build()
    {
        if (built) return;
        built = true;
        cards = LoadCards();
        silhouette = Resources.Load<Sprite>("Market/MysteryDog");
        RectTransform machine = CreateRect("CaseMachine", transform, Vector2.zero, new Vector2(910f, 1240f));
        Artwork(machine, "Backplate", Load("CaseMachine"), Vector2.zero, new Vector2(910f, 1240f));
        Artwork(machine, "ReelHousing", Load("CaseMachine"), new Vector2(0f, 225f), new Vector2(870f, 475f));

        RectTransform viewport = CreateRect("ReelViewport", machine, new Vector2(0f, 215f), new Vector2(800f, 334f));
        viewport.gameObject.AddComponent<RectMask2D>();
        reel = CreateRect("MovingCards", viewport, Vector2.zero, new Vector2(800f, 314f));
        selector = Artwork(machine, "GoldSelector", Load("CaseSelector"), new Vector2(0f, 215f),
            new Vector2(228f, 406f)).rectTransform;

        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 166f;
            Artwork(machine, Rarities[i] + "Swatch", cards[i], new Vector2(x, -85f), new Vector2(61f, 67f));
            Label(machine, Rarities[i], new Vector2(x, -139f), new Vector2(164f, 34f), i == 1 || i == 4 ? 19f : 22f);
        }

        Image buttonArt = Artwork(machine, "OpenCase", Load("OpenCaseButton"), new Vector2(0f, -347f),
            new Vector2(880f, 418f));
        buttonArt.preserveAspect = true;
        buttonArt.raycastTarget = true;
        open = buttonArt.gameObject.AddComponent<Button>();
        open.targetGraphic = buttonArt;
        open.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = open.colors;
        colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.72f, 0.78f, 0.86f, 1f);
        colors.disabledColor = new Color(0.6f, 0.65f, 0.72f, 0.8f);
        colors.fadeDuration = 0.06f;
        open.colors = colors;
        open.onClick.AddListener(OpenCase);
        buttonArt.gameObject.AddComponent<MenuPressScale>();
        TMP_Text openLabel = Label(buttonArt.transform, "OPEN CASE", new Vector2(0f, -20f),
            new Vector2(650f, 150f), 70f);
        openLabel.outlineColor = new Color32(91, 35, 62, 255);
        openLabel.outlineWidth = 0.18f;
        status = Label(machine, "", new Vector2(0f, -549f), new Vector2(780f, 59f), 25f);
        ShowIdle();
        Refresh();
    }

    private void OnEnable()
    {
        BalloonDogEconomy.Changed += Refresh;
        if (built) { resultMessage = null; ShowIdle(); Refresh(); }
    }

    private void OnDisable()
    {
        BalloonDogEconomy.Changed -= Refresh;
        StopAllCoroutines();
        spinning = false;
        if (selector != null) selector.localScale = Vector3.one;
        // Cancellation before the reveal does not spend coins or grant a reward.
    }

    private void Refresh()
    {
        if (!built || status == null || open == null) return;
        bool complete = BalloonDogEconomy.AllSkinsOwned;
        bool enough = BalloonDogEconomy.Coins >= Cost;
        open.interactable = !spinning && !complete && enough;
        if (spinning) return;
        status.color = Color.white;
        if (!string.IsNullOrEmpty(resultMessage)) status.text = resultMessage;
        else if (complete) status.text = "COLLECTION COMPLETE";
        else if (!enough) status.text = "NEED " + Cost + " COINS";
        else status.text = "OPEN CASE  •  " + Cost + " COINS";
    }

    private void ShowIdle()
    {
        ClearCells();
        reel.anchoredPosition = Vector2.zero;
        for (int i = -3; i <= 3; i++) AddCell(i * Step, (i + 7) % 5, "?", null);
    }

    private void ClearCells()
    {
        foreach (GameObject cell in cells)
        {
            if (cell != null) { cell.SetActive(false); Destroy(cell); }
        }
        cells.Clear();
    }

    private TMP_Text AddCell(float x, int tier, string mark, string skinName)
    {
        RectTransform cell = CreateRect("CaseCard", reel, new Vector2(x, 0f), new Vector2(172f, 302f));
        cells.Add(cell.gameObject);
        Artwork(cell, "Card", cards[tier], Vector2.zero, new Vector2(172f, 302f));
        if (silhouette != null)
        {
            Image dog = Artwork(cell, "Dog", silhouette, new Vector2(0f, 4f), new Vector2(126f, 156f));
            dog.preserveAspect = true;
            dog.color = new Color(0.025f, 0.14f, 0.27f, 0.78f);
        }
        TMP_Text question = Label(cell, mark, new Vector2(0f, 4f), new Vector2(130f, 135f), 96f);
        Label(cell, skinName ?? Rarities[tier], new Vector2(0f, -116f), new Vector2(150f, 34f), 17f);
        return question;
    }

    private void OpenCase()
    {
        if (spinning || BalloonDogEconomy.AllSkinsOwned || BalloonDogEconomy.Coins < Cost) return;
        resultMessage = null;
        spinning = true;
        Refresh();
        StartCoroutine(Spin());
    }

    private IEnumerator Spin()
    {
        // Preserve the existing economy: the next unowned skin is awarded.
        BalloonDogSkinDefinition reward = BalloonDogEconomy.PeekNextLockedSkin();
        ClearCells();
        const int winningIndex = 28;
        TMP_Text winningMark = null;
        for (int i = -3; i <= winningIndex + 3; i++)
        {
            int style = (i + 35) % 5;
            if (i == winningIndex)
            {
                // Card color is a visual swatch, not a new rarity/drop probability rule.
                Color.RGBToHSV(reward.PrimaryColor, out float hue, out float saturation, out float value);
                style = saturation < 0.2f ? 0 : hue < 0.15f ? 4 : hue < 0.48f ? 1 : hue < 0.67f ? 2 : 3;
            }
            TMP_Text mark = AddCell(i * Step, style, "?", i == winningIndex ? reward.DisplayName : null);
            if (i == winningIndex) winningMark = mark;
        }
        status.text = "OPENING...";
        float elapsed = 0f;
        const float duration = 4.2f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // A single continuous strip settles exactly on the reward, with no resets.
            float progress = 1f - Mathf.Pow(1f - t, 4f);
            reel.anchoredPosition = new Vector2(-winningIndex * Step * progress, 0f);
            yield return null;
        }
        reel.anchoredPosition = new Vector2(-winningIndex * Step, 0f);
        bool sameReward = BalloonDogEconomy.PeekNextLockedSkin().Id == reward.Id;
        if (sameReward && BalloonDogEconomy.TryUnlockNextSkin(Cost, out BalloonDogSkinDefinition unlocked))
        {
            BalloonDogSkinSystem.ApplySelectedSkin();
            winningMark.text = "!";
            resultMessage = unlocked.DisplayName + "\nUNLOCKED + EQUIPPED";
        }
        else resultMessage = "CASE CANCELLED — TRY AGAIN";

        elapsed = 0f;
        while (elapsed < 0.45f)
        {
            elapsed += Time.unscaledDeltaTime;
            selector.localScale = Vector3.one * (1f + Mathf.Sin(Mathf.Clamp01(elapsed / 0.45f) * Mathf.PI) * 0.07f);
            yield return null;
        }
        selector.localScale = Vector3.one;
        spinning = false;
        Refresh();
    }

    private Sprite Load(string name)
    {
        Sprite sprite = Resources.Load<Sprite>(Art + name);
        if (sprite == null) Debug.LogError("Missing case sprite: " + Art + name);
        return sprite;
    }

    private Sprite[] LoadCards()
    {
        Texture2D atlas = Resources.Load<Texture2D>(Art + "CaseCards");
        Sprite[] result = new Sprite[5];
        if (atlas == null) { Debug.LogError("Missing CaseCards atlas"); return result; }
        Rect[] regions = { new Rect(33,94,385,539), new Rect(463,94,386,539), new Rect(893,94,386,539),
            new Rect(1323,93,386,540), new Rect(1754,93,385,540) };
        for (int i = 0; i < result.Length; i++)
        {
            // Coordinates are measured on the original 2172 x 724 atlas; handle import scaling.
            Rect r = regions[i];
            r.x *= atlas.width / 2172f; r.width *= atlas.width / 2172f;
            r.y *= atlas.height / 724f; r.height *= atlas.height / 724f;
            result[i] = Sprite.Create(atlas, r, new Vector2(0.5f,0.5f), 100f);
            ownedSprites.Add(result[i]);
        }
        return result;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    private static Image Artwork(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        Image image = CreateRect(name, parent, position, size).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.enabled = sprite != null; // Never render a white rectangle for a missing sprite.
        return image;
    }

    private static TMP_Text Label(Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
    {
        TextMeshProUGUI label = CreateRect("Label", parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        BalloonDogTitanFont.Apply(label);
        label.text = value; label.fontSize = fontSize; label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        return label;
    }

    private void OnDestroy()
    {
        BalloonDogEconomy.Changed -= Refresh;
        foreach (Sprite sprite in ownedSprites) if (sprite != null) Destroy(sprite);
    }
}
