using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>The authored obstacle course ends with an air-powered x1–x10 bonus flight.</summary>
public sealed class BalloonDogMapFinale : MonoBehaviour
{
    public static float FinishZ { get; private set; } = 65f;
    public static float BonusStartZ { get; private set; } = 68f;
    public static float RoadWidth { get; private set; } = 6f;
    private static float roadCenterX;
    private static float roadTop;
    private const float ZoneLength = 6f;
    private PlayerRunner runner;
    private bool finishing;
    private Material balloonMaterial;
    private Material[] palette;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindAnyObjectByType<PlayerRunner>() == null ||
            FindAnyObjectByType<BalloonDogMapFinale>() != null) return;
        RefreshCourseLayout();
        new GameObject("BalloonDog_MapFinale").AddComponent<BalloonDogMapFinale>();
    }

    public static void RefreshCourseLayout()
    {
        // Read the actual scene road; do not change its Transform in Play mode.
        Renderer road = GameObject.Find("Road")?.GetComponent<Renderer>();
        if (road == null)
        {
            FinishZ = 65f;
            BonusStartZ = 68f;
            RoadWidth = 6f;
            roadCenterX = roadTop = 0f;
            return;
        }
        Bounds bounds = road.bounds;
        BonusStartZ = bounds.max.z;
        FinishZ = BonusStartZ - 3f;
        RoadWidth = bounds.size.x;
        roadCenterX = bounds.center.x;
        roadTop = bounds.max.y;
    }

    private void Start()
    {
        runner = FindAnyObjectByType<PlayerRunner>();
        RefreshCourseLayout();
        // This course has a single finish owner; legacy finish triggers cannot cut the bonus short.
        foreach (FinishLine finish in FindObjectsByType<FinishLine>(FindObjectsSortMode.None))
            finish.enabled = false;
        foreach (BalloonDogCampaignFinish finish in
                 FindObjectsByType<BalloonDogCampaignFinish>(FindObjectsSortMode.None))
            finish.enabled = false;
        Renderer road = GameObject.Find("Road")?.GetComponent<Renderer>();
        if (road == null) { enabled = false; return; }
        Color[] colors = { new Color(0.59f, 0.29f, 0.88f),
            new Color(1f, 0.52f, 0.15f), new Color(1f, 0.91f, 0.30f),
            new Color(0.40f, 0.84f, 0.62f), new Color(0.33f, 0.55f, 0.88f) };
        palette = new Material[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            palette[i] = new Material(road.sharedMaterial);
            palette[i].color = colors[i];
            if (palette[i].HasProperty("_BaseColor"))
                palette[i].SetColor("_BaseColor", colors[i]);
        }
        RenderSettings.fog = false;
        foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (camera.GetComponent<RunnerCameraFollow>() == null) continue;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.72f, 0.65f, 0.87f);
        }
        // Match the authored road width, elevation and far edge exactly.
        float postOffset = Mathf.Max(.5f, RoadWidth * .5f - .2f);
        Block("Finish_Left", new Vector3(roadCenterX - postOffset, roadTop + 2.3f, FinishZ),
            new Vector3(.45f, 4.6f, .55f), 0);
        Block("Finish_Right", new Vector3(roadCenterX + postOffset, roadTop + 2.3f, FinishZ),
            new Vector3(.45f, 4.6f, .55f), 0);
        Block("Finish_Top", new Vector3(roadCenterX, roadTop + 4.6f, FinishZ),
            new Vector3(RoadWidth + .05f, .6f, .55f), 1);
        Label("FINISH", new Vector3(roadCenterX, roadTop + 4.6f, FinishZ - .32f),
            Quaternion.identity, RoadWidth * .9f, 2.4f);
        for (int i = 0; i < 10; i++)
        {
            float centerZ = BonusStartZ + (i + .5f) * ZoneLength;
            Block("Bonus_x" + (i + 1), new Vector3(roadCenterX, roadTop - .05f, centerZ),
                new Vector3(RoadWidth, .1f, ZoneLength), i % palette.Length);
            Label((i + 1) + "x", new Vector3(roadCenterX, roadTop + .02f, centerZ),
                Quaternion.Euler(90f, 0f, 0f), RoadWidth * .9f, 4.8f,
                i % palette.Length == 2 || i % palette.Length == 3);
        }
        Block("Bonus_EndCap", new Vector3(roadCenterX, roadTop + .18f, BonusStartZ + 10f * ZoneLength),
            new Vector3(RoadWidth, .36f, .35f), 0);
    }

    private void Update()
    {
        if (finishing || runner == null || !runner.MovementEnabled ||
            (GameManager.Instance != null && GameManager.Instance.IsGameOver)) return;
        if (runner.transform.position.z >= FinishZ) StartCoroutine(FlyToBonus());
    }

    // Ten equal air bands: 0% -> 1x, 50% -> 6x, 100% -> 10x.
    public static int MultiplierForAir(float normalizedAir)
    {
        return Mathf.Clamp(1 + Mathf.FloorToInt(Mathf.Clamp01(normalizedAir) * 10f), 1, 10);
    }

    public static float LandingZForAir(float normalizedAir)
    {
        int multiplier = MultiplierForAir(normalizedAir);
        return BonusStartZ + (multiplier - .5f) * ZoneLength;
    }

    private IEnumerator FlyToBonus()
    {
        finishing = true;
        ScoreController score = runner.GetComponent<ScoreController>();
        int baseScore = score != null ? score.CurrentScore : 0;
        AirController air = runner.GetComponent<AirController>();
        float remainingAir = air != null ? Mathf.Clamp01(air.NormalizedAir) : 0f;
        int multiplier = MultiplierForAir(remainingAir);
        runner.SetMovementEnabled(false);
        PlayerHorizontalController horizontal = runner.GetComponent<PlayerHorizontalController>();
        if (horizontal != null) horizontal.enabled = false;
        PlayerFormController form = runner.GetComponent<PlayerFormController>();
        if (form != null) { form.ForceBalloonForm(); form.enabled = false; }
        BalloonSizeController size = runner.GetComponent<BalloonSizeController>();
        if (size != null) { size.CancelShrinkImmediately(); size.enabled = false; }
        PlayerJuiceController juice = runner.GetComponent<PlayerJuiceController>();
        if (juice != null) juice.enabled = false;
        // The dog stays at the gate throughout the bonus; only the balloon flies.
        Rigidbody body = runner.GetComponent<Rigidbody>();
        Vector3 stop = runner.transform.position;
        stop.z = FinishZ;
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.useGravity = false;
            body.position = stop;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }
        runner.transform.position = stop;
        foreach (Collider collider in runner.GetComponentsInChildren<Collider>())
            collider.enabled = false;
        GameObject.Find("ShrinkButton")?.SetActive(false);
        GameObject.Find("HelicopterButton")?.SetActive(false);

        Vector3 start = stop + new Vector3(0f, .25f, .35f);
        Transform balloon = CreateBonusBalloon(start);
        Vector3 balloonScale = Vector3.one * .85f;
        GameAudioController.PlayFinish();
        // Inflate visibly from the dog before detaching.
        for (float elapsed = 0f; elapsed < .4f; elapsed += Time.deltaTime)
        {
            balloon.localScale = Vector3.Lerp(Vector3.one * .05f, balloonScale, elapsed / .4f);
            yield return null;
        }
        balloon.localScale = balloonScale;
        foreach (RunnerCameraFollow follow in FindObjectsByType<RunnerCameraFollow>(FindObjectsSortMode.None))
            follow.Configure(balloon);
        Vector3 target = new Vector3(roadCenterX, roadTop + .6f, LandingZForAir(remainingAir));
        float duration = Mathf.Lerp(1.8f, 4.5f, remainingAir);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) yield break;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            position.y += Mathf.Sin(t * Mathf.PI) * Mathf.Lerp(2f, 5f, remainingAir);
            // Scripted flight, no physics impulse or bounce beyond the 10x zone.
            position.z = Mathf.Min(position.z, BonusStartZ + 10f * ZoneLength - .5f);
            balloon.position = position;
            yield return null;
        }
        balloon.position = target;
        RuntimeVfx.SpawnBurst(target, new Color(1f, .8f, .2f), 24, 2f, .1f, .6f);
        yield return new WaitForSeconds(.65f);
        GameManager manager = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
        manager?.TriggerLevelComplete(baseScore, target.z - FinishZ, multiplier, baseScore * multiplier);
    }

    private Transform CreateBonusBalloon(Vector3 position)
    {
        balloonMaterial = new Material(palette[2]);
        Color pink = new Color(1f, .24f, .52f);
        balloonMaterial.color = pink;
        if (balloonMaterial.HasProperty("_BaseColor")) balloonMaterial.SetColor("_BaseColor", pink);
        if (balloonMaterial.HasProperty("_Smoothness")) balloonMaterial.SetFloat("_Smoothness", .85f);
        if (balloonMaterial.HasProperty("_Metallic")) balloonMaterial.SetFloat("_Metallic", .25f);
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "AirBonusBalloon";
        ball.transform.SetParent(transform);
        ball.transform.position = position;
        ball.transform.localScale = Vector3.one * .05f;
        ball.GetComponent<Collider>().enabled = false;
        ball.GetComponent<Renderer>().sharedMaterial = balloonMaterial;
        GameObject knot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        knot.name = "BalloonKnot";
        knot.transform.SetParent(ball.transform, false);
        knot.transform.localPosition = new Vector3(0f, -.52f, 0f);
        knot.transform.localScale = new Vector3(.15f, .22f, .15f);
        knot.GetComponent<Collider>().enabled = false;
        knot.GetComponent<Renderer>().sharedMaterial = balloonMaterial;
        return ball.transform;
    }

    private void Block(string name, Vector3 position, Vector3 scale, int color)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(transform);
        block.transform.position = position;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = palette[color];
        block.GetComponent<Collider>().enabled = false;
    }

    private void Label(string text, Vector3 position, Quaternion rotation, float width, float size, bool dark = false)
    {
        GameObject label = new GameObject(text, typeof(TextMeshPro));
        label.transform.SetParent(transform);
        label.transform.SetPositionAndRotation(position, rotation);
        TextMeshPro tmp = label.GetComponent<TextMeshPro>();
        BalloonDogTitanFont.Apply(tmp);
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = dark ? new Color(.23f, .12f, .38f) : Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        tmp.rectTransform.sizeDelta = new Vector2(width, 3f);
    }

    private void OnDestroy()
    {
        if (balloonMaterial != null) Destroy(balloonMaterial);
        if (palette == null) return;
        foreach (Material material in palette) if (material != null) Destroy(material);
    }
}
