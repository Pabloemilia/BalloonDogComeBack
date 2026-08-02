using System;
using System.Collections;
using TMPro;
using UnityEngine;

public sealed class StartCountdownController : MonoBehaviour
{
    [SerializeField] private TMP_Text countdownText;
    [SerializeField, Min(0.1f)] private float stepDuration = 0.55f;
    private bool running;

    public void Configure(TMP_Text text)
    {
        countdownText = text;
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    public void Begin(Action onComplete)
    {
        if (running)
        {
            return;
        }

        StartCoroutine(CountdownRoutine(onComplete));
    }

    private IEnumerator CountdownRoutine(Action onComplete)
    {
        running = true;
        Time.timeScale = 0f;

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        string[] steps = { "3", "2", "1", "BAŞLA!" };
        foreach (string step in steps)
        {
            if (countdownText != null)
            {
                countdownText.text = step;
                countdownText.transform.localScale = Vector3.one * 1.4f;
            }

            float elapsed = 0f;
            while (elapsed < stepDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (countdownText != null)
                {
                    float t = Mathf.Clamp01(elapsed / stepDuration);
                    countdownText.transform.localScale = Vector3.Lerp(
                        Vector3.one * 1.4f,
                        Vector3.one * 0.85f,
                        t);
                }
                yield return null;
            }
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        Time.timeScale = 1f;
        running = false;
        onComplete?.Invoke();
    }
}
