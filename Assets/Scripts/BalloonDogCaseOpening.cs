using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Layered capsule machine presentation backed by the existing ownership economy.</summary>
public sealed class BalloonDogCaseOpening : MonoBehaviour
{
    private const string Art = "MarketUI/CapsuleMachine/";
    private static int Cost => BalloonDogEconomy.CasePrice;
    private static readonly string[] Rarities = { "COMMON", "UNCOMMON", "RARE", "EPIC", "LEGENDARY" };
    private static readonly Color[] Tints = {
        new Color(.68f, .74f, .82f), new Color(.25f, .95f, .22f),
        new Color(.08f, .75f, 1f), new Color(.72f, .15f, 1f), new Color(1f, .68f, .06f)
    };
    private readonly List<Material> materials = new List<Material>();
    private readonly List<Sprite> generatedSprites = new List<Sprite>();
    private readonly List<RectTransform> capsules = new List<RectTransform>();
    private readonly List<Vector2> capsuleOrigins = new List<Vector2>();
    private RectTransform machineVisual;
    private RectTransform crank;
    private RectTransform droppedCapsule;
    private RectTransform rewardPanel;
    private TMP_Text rewardName;
    private TMP_Text status;
    private TMP_Text priceLabel;
    private TMP_Text actionLabel;
    private Button open;
    private int openingCost;
    private bool spinning;
    private bool built;
    private string resultMessage;

    public void Build()
    {
        if (built) return;
        built = true;
        RectTransform machine = CreateRect("CaseMachine", transform, Vector2.zero, new Vector2(910f, 1240f));
        machineVisual = CreateRect("MachineVisual", machine, new Vector2(0f, 135f), new Vector2(744f, 930f));

        // Capsules are behind the semi-transparent glass sprite, never baked into it.
        Vector2[] positions = { new Vector2(-143f, 69f), new Vector2(-146f, 230f),
            new Vector2(0f, 304f), new Vector2(143f, 223f), new Vector2(111f, 61f) };
        for (int i = 0; i < positions.Length; i++)
        {
            Image capsule = Artwork(machineVisual, "Capsule" + i, Load("Capsule"), positions[i], new Vector2(244f, 235f));
            TintCapsule(capsule, Tints[i]);
            capsules.Add(capsule.rectTransform);
            capsuleOrigins.Add(positions[i]);
        }
        Artwork(machineVisual, "BodyAndGlass", Load("Machine"), Vector2.zero, new Vector2(744f, 930f));
        Image handle = Artwork(machineVisual, "Crank", Load("Crank"), new Vector2(116f, -279f), new Vector2(390f, 260f));
        crank = handle.rectTransform;
        // Pivot measured at the round mounting hub, rather than the PNG centre.
        crank.pivot = new Vector2(.255f, .395f);

        droppedCapsule = Artwork(machineVisual, "DispensedCapsule", Load("Capsule"),
            new Vector2(-65f, -365f), new Vector2(156f, 150f)).rectTransform;
        droppedCapsule.gameObject.SetActive(false);

        for (int i = 0; i < Rarities.Length; i++)
        {
            float x = (i - 2) * 164f;
            Image swatch = Artwork(machine, Rarities[i] + "Swatch", Load("Capsule"),
                new Vector2(x, -377f), new Vector2(65f, 63f));
            TintCapsule(swatch, Tints[i]);
            Label(machine, Rarities[i], new Vector2(x, -425f), new Vector2(164f, 35f), i == 1 || i == 4 ? 18f : 21f);
        }

        // The entire visible orange pill depresses; decorative transparent padding is not a hit area.
        RectTransform buttonRoot = CreateRect("OpenCase", machine, new Vector2(0f, -533f), new Vector2(850f, 152f));
        RectTransform face = CreateRect("OrangeFace", buttonRoot, Vector2.zero, buttonRoot.sizeDelta);
        Image buttonArt = Artwork(face, "Artwork", Load("OpenButton"), Vector2.zero, buttonRoot.sizeDelta);
        buttonArt.preserveAspect = false;
        open = buttonRoot.gameObject.AddComponent<Button>();
        open.targetGraphic = buttonArt;
        open.navigation = new Navigation { mode = Navigation.Mode.None };
        open.onClick.AddListener(OpenCase);
        RectTransform price = CreateRect("CasePrice", face, new Vector2(-233f, 0f), new Vector2(305f, 110f));
        Artwork(price, "CoinIcon", Resources.Load<Sprite>("MarketUI/Extras/Icon_Coins"),
            new Vector2(-88f, 0f), new Vector2(74f, 74f));
        priceLabel = Label(price, Cost.ToString("N0"), new Vector2(42f, 0f), new Vector2(180f, 100f), 62f);
        priceLabel.name = "Amount";
        actionLabel = Label(face, "OPEN CASE", new Vector2(143f, 0f), new Vector2(435f, 105f), 49f);
        buttonRoot.gameObject.AddComponent<BalloonDogDirectPressButton>()
            .Configure(open, face, new Vector2(.09f, .49f), true);
        status = Label(machine, "", new Vector2(0f, -633f), new Vector2(860f, 37f), 22f);

        BuildRewardPanel(machine);
        ShowIdle();
        Refresh();
    }

    private void BuildRewardPanel(Transform parent)
    {
        rewardPanel = CreateRect("RewardPanel", parent, new Vector2(0f, 125f), new Vector2(820f, 970f));
        rewardPanel.gameObject.SetActive(false);
        Image background = Artwork(rewardPanel, "Backdrop", Load("Background"), Vector2.zero, rewardPanel.sizeDelta);
        background.preserveAspect = false;
        Label(rewardPanel, "NEW SKIN!", new Vector2(0f, 340f), new Vector2(760f, 95f), 72f);
        RectTransform preview = CreateRect("EquippedSkinPreview", rewardPanel, new Vector2(0f, 25f), new Vector2(720f, 510f));
        // The panel is inactive while this component is configured. It renders the actual equipped model.
        preview.gameObject.AddComponent<BalloonDogMenuShowcase>().CompactPreview = true;
        rewardName = Label(rewardPanel, "", new Vector2(0f, -298f), new Vector2(750f, 98f), 58f);
        Label(rewardPanel, "UNLOCKED + EQUIPPED", new Vector2(0f, -390f), new Vector2(750f, 65f), 34f);
    }

    private void OnEnable()
    {
        BalloonDogEconomy.Changed -= Refresh;
        BalloonDogEconomy.Changed += Refresh;
        if (built) { resultMessage = null; ShowIdle(); Refresh(); }
    }

    private void OnDisable()
    {
        BalloonDogEconomy.Changed -= Refresh;
        StopAllCoroutines();
        spinning = false;
        if (built) ShowIdle();
        // Purchases commit at the reveal. Leaving before then spends nothing.
        // Leaving after the reveal never rolls back an already saved reward.
    }

    private void Refresh()
    {
        if (!built || status == null || open == null) return;
        priceLabel.text = (spinning ? openingCost : Cost).ToString("N0");
        open.interactable = !spinning;
        bool showingReward = rewardPanel != null && rewardPanel.gameObject.activeSelf;
        priceLabel.transform.parent.gameObject.SetActive(!showingReward);
        actionLabel.rectTransform.anchoredPosition = showingReward ? Vector2.zero : new Vector2(143f, 0f);
        actionLabel.text = showingReward ? "CONTINUE" : "OPEN CASE";
        if (spinning) return;
        if (!string.IsNullOrEmpty(resultMessage)) status.text = resultMessage;
        else if (BalloonDogEconomy.AllSkinsOwned) status.text = "COLLECTION COMPLETE";
        else if (BalloonDogEconomy.Coins < Cost) status.text = "NEED " + Cost + " COINS";
        else status.text = "UNLOCK A NEW LOOK!";
    }

    private void ShowIdle()
    {
        machineVisual.gameObject.SetActive(true);
        machineVisual.localScale = Vector3.one;
        crank.localRotation = Quaternion.identity;
        droppedCapsule.gameObject.SetActive(false);
        rewardPanel.gameObject.SetActive(false);
        for (int i = 0; i < capsules.Count; i++)
        {
            capsules[i].anchoredPosition = capsuleOrigins[i];
            capsules[i].localRotation = Quaternion.identity;
        }
    }

    private void OpenCase()
    {
        if (spinning) return;
        if (rewardPanel.gameObject.activeSelf)
        {
            resultMessage = null;
            ShowIdle();
            Refresh();
            return;
        }
        if (BalloonDogEconomy.AllSkinsOwned || BalloonDogEconomy.Coins < Cost) return;
        resultMessage = null;
        openingCost = Cost;
        spinning = true;
        Refresh();
        StartCoroutine(Dispense());
    }

    private IEnumerator Dispense()
    {
        BalloonDogSkinDefinition reward = BalloonDogEconomy.PeekNextLockedSkin();
        status.text = "OPENING...";
        float elapsed = 0f;
        const float mixingDuration = 1.25f;
        while (elapsed < mixingDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / mixingDuration);
            crank.localRotation = Quaternion.Euler(0f, 0f, -360f * Mathf.SmoothStep(0f, 1f, t));
            float amplitude = Mathf.Sin(t * Mathf.PI);
            for (int i = 0; i < capsules.Count; i++)
            {
                float phase = elapsed * 24f + i * 1.7f;
                capsules[i].anchoredPosition = capsuleOrigins[i] +
                    new Vector2(Mathf.Sin(phase) * 17f, Mathf.Cos(phase * .8f) * 23f) * amplitude;
                capsules[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * 12f * amplitude);
            }
            yield return null;
        }
        crank.localRotation = Quaternion.identity;
        for (int i = 0; i < capsules.Count; i++)
        {
            capsules[i].anchoredPosition = capsuleOrigins[i];
            capsules[i].localRotation = Quaternion.identity;
        }
        droppedCapsule.gameObject.SetActive(true);
        elapsed = 0f;
        while (elapsed < .65f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / .65f);
            float bounce = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)) * (1f - t) * 26f;
            droppedCapsule.anchoredPosition = new Vector2(-65f, Mathf.Lerp(-286f, -365f, t) + bounce);
            droppedCapsule.localScale = Vector3.one * Mathf.Lerp(.55f, 1f, t);
            yield return null;
        }
        // One atomic purchase, preserving the existing price, saved ownership and auto-equip rules.
        bool sameReward = !BalloonDogEconomy.AllSkinsOwned && BalloonDogEconomy.PeekNextLockedSkin().Id == reward.Id;
        if (sameReward && BalloonDogEconomy.TryUnlockNextSkin(openingCost, out BalloonDogSkinDefinition unlocked))
        {
            BalloonDogSkinSystem.ApplySelectedSkin();
            rewardName.text = unlocked.DisplayName;
            machineVisual.gameObject.SetActive(false);
            rewardPanel.gameObject.SetActive(true);
            resultMessage = "ADDED TO YOUR COLLECTION";
            elapsed = 0f;
            while (elapsed < .28f)
            {
                elapsed += Time.unscaledDeltaTime;
                rewardPanel.localScale = Vector3.one * Mathf.Lerp(.88f, 1f, Mathf.SmoothStep(0f, 1f, elapsed / .28f));
                yield return null;
            }
            rewardPanel.localScale = Vector3.one;
        }
        else
        {
            ShowIdle();
            resultMessage = "CASE CANCELLED - TRY AGAIN";
        }
        spinning = false;
        Refresh();
    }

    private void TintCapsule(Image image, Color tint)
    {
        Shader shader = Resources.Load<Shader>(Art + "CapsuleTint");
        if (shader == null) return;
        Material material = new Material(shader);
        material.SetColor("_CapsuleColor", tint);
        materials.Add(material);
        image.material = material;
    }

    private Sprite Load(string name)
    {
        Sprite sprite = Resources.Load<Sprite>(Art + name);
        if (sprite == null) Debug.LogError("Missing capsule machine sprite: " + Art + name);
        if (sprite != null && name == "OpenButton")
        {
            // Exclude export padding from the UI face and its rounded hit area.
            Texture2D texture = sprite.texture;
            Rect frame = new Rect(50f / 2172f * texture.width, 137f / 724f * texture.height,
                2072f / 2172f * texture.width, 450f / 724f * texture.height);
            sprite = Sprite.Create(texture, frame, new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
            generatedSprites.Add(sprite);
        }
        return sprite;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image Artwork(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        Image image = CreateRect(name, parent, position, size).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = sprite != null;
        return image;
    }

    private static TMP_Text Label(Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
    {
        TextMeshProUGUI label = CreateRect("Label", parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        BalloonDogTitanFont.Apply(label);
        label.text = value;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = fontSize * .65f;
        label.fontSizeMax = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        return label;
    }

    private void OnDestroy()
    {
        BalloonDogEconomy.Changed -= Refresh;
        foreach (Material material in materials) if (material != null) Destroy(material);
        foreach (Sprite sprite in generatedSprites) if (sprite != null) Destroy(sprite);
    }
}
