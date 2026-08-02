using System.Collections;
using UnityEngine;

public sealed class UiAutoHide : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float visibleDuration = 4f;
    [SerializeField, Min(0.1f)] private float fadeDuration = 0.5f;
    private CanvasGroup canvasGroup;

    private void OnEnable()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f;
        StartCoroutine(HideRoutine());
    }

    private IEnumerator HideRoutine()
    {
        yield return new WaitForSeconds(visibleDuration);
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
