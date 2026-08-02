using System.Collections;
using TMPro;
using UnityEngine;

public sealed class WorldLabelDirectionFix : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntime()
    {
        GameObject runtime = new GameObject("__WorldLabelDirectionFixV15");
        runtime.AddComponent<WorldLabelDirectionFix>();
    }

    private IEnumerator Start()
    {
        // Sahne ve bootstrap tamamen kurulsun.
        for (int i = 0; i < 8; i++)
        {
            yield return null;
        }

        FixLabels();
        Destroy(gameObject);
    }

    private static void FixLabels()
    {
        TextMeshPro[] labels = Resources.FindObjectsOfTypeAll<TextMeshPro>();

        foreach (TextMeshPro label in labels)
        {
            if (label == null || !label.gameObject.scene.IsValid())
            {
                continue;
            }

            if (!ShouldFix(label))
            {
                continue;
            }

            Transform t = label.transform;
            Vector3 euler = t.localEulerAngles;

            // Bunlar sahnede 180 derece ters bakıyordu.
            t.localEulerAngles = new Vector3(0f, 0f, 0f);

            // Hafif stil dokunuşu: daha okunaklı olsun.
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.outlineWidth = 0.18f;
            label.outlineColor = new Color32(7, 34, 58, 255);
            label.color = Color.white;
        }
    }

    private static bool ShouldFix(TextMeshPro label)
    {
        string objectName = label.gameObject.name != null
            ? label.gameObject.name.ToUpperInvariant()
            : string.Empty;

        string text = label.text != null
            ? label.text.Trim().ToUpperInvariant()
            : string.Empty;

        if (objectName.EndsWith("_WORLDLABEL"))
        {
            return true;
        }

        switch (text)
        {
            case "START":
            case "FLOW":
            case "BOOST":
            case "CHAOS":
            case "FINISH":
                return true;
            default:
                return false;
        }
    }
}
