using System.Collections;
using TMPro;
using UnityEngine;

public sealed class SectionBannerController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private TMP_Text bannerText;
    [SerializeField] private float[] sectionZ = { 35f, 78f, 120f, 158f };
    [SerializeField] private string[] sectionNames = { "ISINMA", "HIZLAN", "USTALIK", "FİNAL" };
    [SerializeField, Min(0.1f)] private float displayDuration = 1.2f;

    private int nextSection;
    private Coroutine displayRoutine;

    public void Configure(Transform target, TMP_Text text, float[] positions, string[] names)
    {
        player = target;
        bannerText = text;
        sectionZ = positions;
        sectionNames = names;
        nextSection = 0;
        if (bannerText != null)
        {
            bannerText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (player == null || sectionZ == null || sectionNames == null)
        {
            return;
        }

        if (nextSection >= sectionZ.Length || nextSection >= sectionNames.Length)
        {
            return;
        }

        if (player.position.z < sectionZ[nextSection])
        {
            return;
        }

        Show(sectionNames[nextSection]);
        nextSection++;
    }

    private void Show(string message)
    {
        if (bannerText == null)
        {
            return;
        }

        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
        }
        displayRoutine = StartCoroutine(DisplayRoutine(message));
    }

    private IEnumerator DisplayRoutine(string message)
    {
        bannerText.gameObject.SetActive(true);
        bannerText.text = message;
        bannerText.transform.localScale = Vector3.one * 1.25f;

        float elapsed = 0f;
        while (elapsed < displayDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / displayDuration);
            bannerText.transform.localScale = Vector3.Lerp(Vector3.one * 1.25f, Vector3.one, t);
            Color color = bannerText.color;
            color.a = Mathf.Sin(t * Mathf.PI);
            bannerText.color = color;
            yield return null;
        }

        bannerText.gameObject.SetActive(false);
        displayRoutine = null;
    }
}
