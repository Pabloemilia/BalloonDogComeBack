using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>The authored obstacle course ends with an air-powered x1–x10 bonus flight.</summary>
public sealed class BalloonDogMapFinale : MonoBehaviour
{
    public const float FinishZ = 175f;
    private const float BonusStartZ = 180f;
    private const float ZoneLength = 9f;
    private PlayerRunner runner;
    private bool finishing;
    private float groundPlayerY;
    private Material[] palette;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindAnyObjectByType<PlayerRunner>() == null ||
            FindAnyObjectByType<BalloonDogMapFinale>() != null) return;
        new GameObject("BalloonDog_MapFinale").AddComponent<BalloonDogMapFinale>();
    }

    private void Start()
    {
        runner = FindAnyObjectByType<PlayerRunner>();
        groundPlayerY = runner.transform.position.y;
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
        // Only the finish uses a frame; the obstacle road stays white and uncluttered.
        Block("Finish_Left", new Vector3(-2.8f, 2.3f, FinishZ), new Vector3(.45f, 4.6f, .55f), 0);
        Block("Finish_Right", new Vector3(2.8f, 2.3f, FinishZ), new Vector3(.45f, 4.6f, .55f), 0);
        Block("Finish_Top", new Vector3(0f, 4.6f, FinishZ), new Vector3(6.05f, .6f, .55f), 1);
        Label("FINISH", new Vector3(0f, 4.6f, FinishZ - .32f), Quaternion.identity, 5.4f, 2.4f);
        for (int i = 0; i < 10; i++)
        {
            float centerZ = BonusStartZ + (i + .5f) * ZoneLength;
            Block("Bonus_x" + (i + 1), new Vector3(0f, .035f, centerZ),
                new Vector3(5.8f, .07f, ZoneLength - .15f), i % palette.Length);
            Label("x" + (i + 1), new Vector3(0f, .085f, centerZ),
                Quaternion.Euler(90f, 0f, 0f), 5.4f, 4.8f,
                i % palette.Length == 2 || i % palette.Length == 3);
        }
    }

    private void Update()
    {
        if (finishing || runner == null || !runner.MovementEnabled ||
            (GameManager.Instance != null && GameManager.Instance.IsGameOver)) return;
        if (runner.transform.position.z >= FinishZ) StartCoroutine(FlyToBonus());
    }

    private IEnumerator FlyToBonus()
    {
        finishing = true;
        ScoreController score = runner.GetComponent<ScoreController>();
        int baseScore = score != null ? score.CurrentScore : 0;
        AirController air = runner.GetComponent<AirController>();
        float remainingAir = air != null ? air.NormalizedAir : 0f;
        int multiplier = Mathf.Clamp(1 + Mathf.FloorToInt(remainingAir * 9f), 1, 10);
        runner.SetMovementEnabled(false);
        PlayerHorizontalController horizontal = runner.GetComponent<PlayerHorizontalController>();
        if (horizontal != null) horizontal.enabled = false;
        BalloonSizeController size = runner.GetComponent<BalloonSizeController>();
        if (size != null) { size.CancelShrinkImmediately(); size.enabled = false; }
        PlayerFormController form = runner.GetComponent<PlayerFormController>();
        if (form != null) { form.ForceBalloonForm(); form.enabled = false; }
        Rigidbody body = runner.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }
        Vector3 start = runner.transform.position;
        Vector3 target = new Vector3(0f, groundPlayerY + .07f,
            BonusStartZ + (multiplier - .5f) * ZoneLength);
        float duration = Mathf.Lerp(1.8f, 4.5f, remainingAir);
        GameAudioController.PlayFinish();
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) yield break;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            position.y += Mathf.Sin(t * Mathf.PI) * Mathf.Lerp(2f, 5f, remainingAir);
            runner.transform.position = position;
            yield return null;
        }
        runner.transform.position = target;
        yield return new WaitForSeconds(.55f);
        GameManager manager = GameManager.Instance ?? FindAnyObjectByType<GameManager>();
        manager?.TriggerLevelComplete(baseScore, target.z - FinishZ, multiplier, baseScore * multiplier);
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
        if (palette == null) return;
        foreach (Material material in palette) if (material != null) Destroy(material);
    }
}
