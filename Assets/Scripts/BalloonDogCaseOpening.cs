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
    private readonly List<float> capsuleAngles = new List<float>();
    private RectTransform machineVisual;
    private RectTransform crank;
    private RectTransform droppedCapsule;
    private RectTransform rewardPanel;
    private TMP_Text rewardName;
    private RectTransform rewardContent;
    private CanvasGroup marketInput;
    private bool marketWasInteractable;
    private bool marketInputLocked;
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
        Vector2[] positions = {
            new Vector2(-183f, -92f), new Vector2(-56f, -104f), new Vector2(78f, -87f), new Vector2(192f, -66f),
            new Vector2(-234f, 13f), new Vector2(-119f, 27f), new Vector2(12f, -2f), new Vector2(129f, 42f), new Vector2(239f, 18f),
            new Vector2(-210f, 119f), new Vector2(-87f, 137f), new Vector2(32f, 100f), new Vector2(140f, 146f), new Vector2(217f, 115f),
            new Vector2(-147f, 197f), new Vector2(-35f, 222f), new Vector2(68f, 193f), new Vector2(161f, 208f)
        };
        // Decorative mixture only: 8 uncommon, 5 common, 3 rare, 1 epic, 1 legendary.
        // This does not change the existing skin-unlock economy or introduce drop probabilities.
        int[] tiers = { 1, 0, 1, 2, 1, 3, 0, 1, 2, 1, 0, 1, 4, 0, 2, 1, 0, 1 };
        for (int i = 0; i < positions.Length; i++)
        {
            float size = 148f + (i * 11 % 27);
            Image capsule = Artwork(machineVisual, "Capsule" + i, Load("Capsule"), positions[i], new Vector2(size, size * .9625f));
            TintCapsule(capsule, Tints[tiers[i]]);
            float angle = (i * 29 % 47) - 23f;
            capsule.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            capsuleAngles.Add(angle);
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

        BuildRewardPanel();
        ShowIdle();
        Refresh();
    }

    private void BuildRewardPanel()
    {
        // A separate overlay canvas covers the whole display, outside the market's shelf and animation scale.
        rewardPanel = CreateRect("CapsuleRewardScreen", null, Vector2.zero, new Vector2(1080f, 1920f));
        rewardPanel.gameObject.SetActive(false);
        Canvas pageCanvas = rewardPanel.gameObject.AddComponent<Canvas>();
        pageCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Canvas marketCanvas = GetComponentInParent<Canvas>();
        pageCanvas.sortingOrder = marketCanvas != null ? marketCanvas.rootCanvas.sortingOrder + 100 : 100;
        CanvasScaler scaler = rewardPanel.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        rewardPanel.gameObject.AddComponent<GraphicRaycaster>();
        Image background = Artwork(rewardPanel, "Backdrop", Load("Background"), Vector2.zero, Vector2.zero);
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.offsetMin = background.rectTransform.offsetMax = Vector2.zero;
        background.preserveAspect = false;
        background.color = new Color(.30f, .43f, .83f, 1f);
        background.raycastTarget = true;
        RectTransform effects = CreateRect("Celebration", rewardPanel, Vector2.zero, new Vector2(1080f, 1920f));
        effects.gameObject.AddComponent<BalloonDogSkinRewardEffects>().raycastTarget = false;

        rewardContent = CreateRect("RewardContent", rewardPanel, Vector2.zero, new Vector2(1080f, 1920f));
        TMP_Text heading = Label(rewardContent, "NEW SKIN!", new Vector2(0f, 685f), new Vector2(990f, 160f), 122f);
        heading.outlineColor = new Color(.12f, .12f, .45f, 1f);
        heading.outlineWidth = .18f;
        Label(rewardContent, "LOOK WHO YOU UNLOCKED", new Vector2(0f, 560f), new Vector2(950f, 60f), 33f);
        RectTransform preview = CreateRect("EquippedSkinPreview", rewardContent, new Vector2(0f, 40f), new Vector2(980f, 960f));
        preview.gameObject.AddComponent<BalloonDogMenuShowcase>().CompactPreview = true;
        rewardName = Label(rewardContent, "", new Vector2(0f, -505f), new Vector2(980f, 125f), 90f);
        rewardName.outlineColor = new Color(.12f, .12f, .45f, 1f);
        rewardName.outlineWidth = .16f;
        Label(rewardContent, "UNLOCKED + EQUIPPED", new Vector2(0f, -610f), new Vector2(950f, 65f), 37f);

        RectTransform buttonRoot = CreateRect("RewardContinue", rewardContent, new Vector2(0f, -775f), new Vector2(790f, 160f));
        RectTransform face = CreateRect("Face", buttonRoot, Vector2.zero, buttonRoot.sizeDelta);
        Image faceArt = Artwork(face, "Artwork", Load("OpenButton"), Vector2.zero, buttonRoot.sizeDelta);
        faceArt.preserveAspect = false;
        Label(face, "CONTINUE", Vector2.zero, new Vector2(720f, 120f), 64f);
        Button button = buttonRoot.gameObject.AddComponent<Button>();
        button.targetGraphic = faceArt;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(DismissReward);
        buttonRoot.gameObject.AddComponent<BalloonDogDirectPressButton>()
            .Configure(button, face, new Vector2(.1f, .49f), true);
    }

    private void SetMarketInputLocked(bool locked)
    {
        if (locked == marketInputLocked) return;
        if (locked)
        {
            Transform market = transform;
            for (Transform ancestor = transform; ancestor != null; ancestor = ancestor.parent)
                if (ancestor.name == "ModernMarketScreen") { market = ancestor; break; }
            marketInput = market.GetComponent<CanvasGroup>();
            if (marketInput == null) marketInput = market.gameObject.AddComponent<CanvasGroup>();
            marketWasInteractable = marketInput.interactable;
            marketInput.interactable = false;
        }
        else if (marketInput != null) marketInput.interactable = marketWasInteractable;
        marketInputLocked = locked;
    }

    private void DismissReward()
    {
        if (spinning) return;
        resultMessage = null;
        ShowIdle();
        Refresh();
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
        bool showingReward = rewardPanel != null && rewardPanel.gameObject.activeSelf;
        open.interactable = !spinning && !showingReward;
        priceLabel.transform.parent.gameObject.SetActive(true);
        actionLabel.rectTransform.anchoredPosition = new Vector2(143f, 0f);
        actionLabel.text = "OPEN CASE";
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
        SetMarketInputLocked(false);
        for (int i = 0; i < capsules.Count; i++)
        {
            capsules[i].anchoredPosition = capsuleOrigins[i];
            capsules[i].localRotation = Quaternion.Euler(0f, 0f, capsuleAngles[i]);
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
            crank.localRotation = Quaternion.Euler(0f, 0f, CrankAngle(t));
            float amplitude = Mathf.Sin(t * Mathf.PI);
            for (int i = 0; i < capsules.Count; i++)
            {
                float phase = elapsed * 24f + i * 1.7f;
                capsules[i].anchoredPosition = capsuleOrigins[i] +
                    new Vector2(Mathf.Sin(phase) * 9f, Mathf.Cos(phase * .8f) * 13f) * amplitude;
                capsules[i].localRotation = Quaternion.Euler(0f, 0f, capsuleAngles[i] + Mathf.Sin(phase) * 12f * amplitude);
            }
            yield return null;
        }
        crank.localRotation = Quaternion.identity;
        for (int i = 0; i < capsules.Count; i++)
        {
            capsules[i].anchoredPosition = capsuleOrigins[i];
            capsules[i].localRotation = Quaternion.Euler(0f, 0f, capsuleAngles[i]);
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
            SetMarketInputLocked(true);
            rewardPanel.gameObject.SetActive(true);
            resultMessage = "ADDED TO YOUR COLLECTION";
            elapsed = 0f;
            while (elapsed < .28f)
            {
                elapsed += Time.unscaledDeltaTime;
                rewardContent.localScale = Vector3.one * Mathf.Lerp(.88f, 1f, Mathf.SmoothStep(0f, 1f, elapsed / .28f));
                yield return null;
            }
            rewardContent.localScale = Vector3.one;
        }
        else
        {
            ShowIdle();
            resultMessage = "CASE CANCELLED - TRY AGAIN";
        }
        spinning = false;
        Refresh();
    }

    // One short down/up stroke, followed by a stationary handle during dispensing.
    public static float CrankAngle(float progress)
    {
        if (progress <= 0f || progress >= .64f) return 0f;
        float stroke = Mathf.SmoothStep(0f, 1f, progress / .64f);
        return -55f * Mathf.Sin(stroke * Mathf.PI);
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
        SetMarketInputLocked(false);
        if (rewardPanel != null)
        {
            rewardPanel.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(rewardPanel.gameObject);
            else DestroyImmediate(rewardPanel.gameObject);
        }
        foreach (Material material in materials)
            if (material != null) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
        foreach (Sprite sprite in generatedSprites)
            if (sprite != null) { if (Application.isPlaying) Destroy(sprite); else DestroyImmediate(sprite); }
    }
}
