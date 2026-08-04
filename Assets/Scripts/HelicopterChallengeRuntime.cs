using System.Collections;
using TMPro;
using UnityEngine;

public sealed class HelicopterChallengeRuntime : MonoBehaviour
{
    private const string RuntimeObjectName = "__HelicopterChallengesV12";
    private const string ChallengeRootName = "HelicopterRequiredChallenges";

    private static Material cyanBalloonMaterial;
    private static Material orangeBalloonMaterial;
    private static Material darkMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateChallengeRuntime()
    {
        if (GameObject.Find(RuntimeObjectName) != null)
        {
            return;
        }

        GameObject runtimeObject = new GameObject(RuntimeObjectName);
        runtimeObject.AddComponent<HelicopterChallengeRuntime>();
    }

    private IEnumerator Start()
    {
        // Bootstrap haritayı kurduktan sonra ekle.
        for (int index = 0; index < 5; index++)
        {
            yield return null;
        }

        if (FindAnyObjectByType<PlayerRunner>() == null ||
            GameObject.Find(ChallengeRootName) != null)
        {
            yield break;
        }

        BuildChallenges();
    }

    private static void BuildChallenges()
    {
        EnsureMaterials();

        GameObject root = new GameObject(ChallengeRootName);

        // İlk görev: yolun alt kısmını tamamen kapatan yüksek balon duvar.
        CreateRequiredFlightBlock(
            root.transform,
            "HelicopterWall_78",
            new Vector3(0f, 1.30f, 78f),
            new Vector3(6.15f, 2.60f, 1.25f),
            cyanBalloonMaterial,
            false);

        CreateHelicopterWarningSign(
            root.transform,
            new Vector3(0f, 0f, 73.5f));

        // İkinci görev: uzun, üzerinden uçulması gereken kapalı platform.
        CreateRequiredFlightBlock(
            root.transform,
            "HelicopterPlatform_126",
            new Vector3(0f, 1.35f, 126f),
            new Vector3(6.15f, 2.70f, 5.40f),
            orangeBalloonMaterial,
            true);

        CreateHelicopterWarningSign(
            root.transform,
            new Vector3(0f, 0f, 120.5f));
    }

    private static void CreateRequiredFlightBlock(
        Transform parent,
        string objectName,
        Vector3 center,
        Vector3 size,
        Material balloonMaterial,
        bool longPlatform)
    {
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(parent, false);
        root.transform.position = center;

        BoxCollider solidCollider = root.AddComponent<BoxCollider>();
        solidCollider.center = Vector3.zero;
        solidCollider.size = size;
        solidCollider.isTrigger = false;

        HelicopterBarrierKnockback knockback =
            root.AddComponent<HelicopterBarrierKnockback>();

        knockback.Configure(
            longPlatform ? 8.2f : 7.2f,
            1.8f,
            0.44f);

        int depthRows = longPlatform ? 3 : 1;
        int heightRows = 4;

        for (int depthIndex = 0; depthIndex < depthRows; depthIndex++)
        {
            float localZ = depthRows == 1
                ? 0f
                : Mathf.Lerp(
                    -size.z * 0.36f,
                    size.z * 0.36f,
                    depthIndex / (float)(depthRows - 1));

            for (int heightIndex = 0; heightIndex < heightRows; heightIndex++)
            {
                float localY = Mathf.Lerp(
                    -size.y * 0.38f,
                    size.y * 0.38f,
                    heightIndex / (float)(heightRows - 1));

                GameObject balloonLog =
                    GameObject.CreatePrimitive(PrimitiveType.Sphere);

                balloonLog.name = "InflatedBarrier";
                balloonLog.transform.SetParent(root.transform, false);
                balloonLog.transform.localPosition =
                    new Vector3(0f, localY, localZ);

                balloonLog.transform.localScale =
                    new Vector3(
                        size.x * 0.94f,
                        size.y * 0.22f,
                        longPlatform
                            ? size.z * 0.28f
                            : size.z * 0.86f);

                Renderer logRenderer =
                    balloonLog.GetComponent<Renderer>();

                if (logRenderer != null)
                {
                    logRenderer.sharedMaterial =
                        heightIndex % 2 == 0
                            ? balloonMaterial
                            : cyanBalloonMaterial;
                }

                Collider logCollider =
                    balloonLog.GetComponent<Collider>();

                if (logCollider != null)
                {
                    logCollider.enabled = false;
                    Destroy(logCollider);
                }
            }
        }

        // Üst kenar, oyuncuya üzerinden uçulacağını gösteren parlak balon şerit.
        GameObject topRail =
            GameObject.CreatePrimitive(PrimitiveType.Sphere);

        topRail.name = "FlyOverTopRail";
        topRail.transform.SetParent(root.transform, false);
        topRail.transform.localPosition =
            new Vector3(0f, size.y * 0.51f, 0f);
        topRail.transform.localScale =
            new Vector3(size.x, 0.23f, size.z * 0.72f);

        Renderer railRenderer = topRail.GetComponent<Renderer>();
        if (railRenderer != null)
        {
            railRenderer.sharedMaterial = orangeBalloonMaterial;
        }

        Collider railCollider = topRail.GetComponent<Collider>();
        if (railCollider != null)
        {
            railCollider.enabled = false;
            Destroy(railCollider);
        }
    }

    private static void CreateHelicopterWarningSign(
        Transform parent,
        Vector3 position)
    {
        GameObject signRoot = new GameObject("HelicopterRequiredSign");
        signRoot.transform.SetParent(parent, false);
        signRoot.transform.position = position;

        CreatePrimitivePart(
            signRoot.transform,
            PrimitiveType.Cylinder,
            "LeftPost",
            new Vector3(-1.65f, 1.15f, 0f),
            new Vector3(0.09f, 1.15f, 0.09f),
            darkMaterial);

        CreatePrimitivePart(
            signRoot.transform,
            PrimitiveType.Cylinder,
            "RightPost",
            new Vector3(1.65f, 1.15f, 0f),
            new Vector3(0.09f, 1.15f, 0.09f),
            darkMaterial);

        GameObject board =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        board.name = "FlightSignBoard";
        board.transform.SetParent(signRoot.transform, false);
        board.transform.localPosition = new Vector3(0f, 2.15f, 0f);
        board.transform.localScale = new Vector3(3.65f, 1.05f, 0.14f);

        Renderer boardRenderer = board.GetComponent<Renderer>();
        if (boardRenderer != null)
        {
            boardRenderer.sharedMaterial = darkMaterial;
        }

        Collider boardCollider = board.GetComponent<Collider>();
        if (boardCollider != null)
        {
            boardCollider.enabled = false;
            Destroy(boardCollider);
        }

        GameObject textObject = new GameObject(
            "FlyText",
            typeof(TextMeshPro));

        textObject.transform.SetParent(signRoot.transform, false);
        textObject.transform.localPosition =
            new Vector3(0f, 2.15f, -0.09f);
        textObject.transform.localRotation =
            Quaternion.Euler(0f, 180f, 0f);

        TextMeshPro text = textObject.GetComponent<TextMeshPro>();
        text.text = "UÇ";
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.fontSize = 5.5f;
        text.color = new Color(0.15f, 0.92f, 1f, 1f);
        text.outlineColor = new Color32(0, 35, 65, 255);
        text.outlineWidth = 0.22f;

        CreateSignRotor(signRoot.transform);
    }

    private static void CreateSignRotor(Transform parent)
    {
        GameObject rotorRoot = new GameObject("BalloonHelicopterIcon");
        rotorRoot.transform.SetParent(parent, false);
        rotorRoot.transform.localPosition =
            new Vector3(0f, 2.92f, -0.12f);

        CreatePrimitivePart(
            rotorRoot.transform,
            PrimitiveType.Sphere,
            "IconBladeX",
            Vector3.zero,
            new Vector3(1.15f, 0.10f, 0.20f),
            cyanBalloonMaterial);

        CreatePrimitivePart(
            rotorRoot.transform,
            PrimitiveType.Sphere,
            "IconBladeZ",
            Vector3.zero,
            new Vector3(0.20f, 0.10f, 1.15f),
            cyanBalloonMaterial);

        CreatePrimitivePart(
            rotorRoot.transform,
            PrimitiveType.Sphere,
            "IconHub",
            Vector3.zero,
            new Vector3(0.24f, 0.16f, 0.24f),
            orangeBalloonMaterial);
    }

    private static void CreatePrimitivePart(
        Transform parent,
        PrimitiveType primitiveType,
        string objectName,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = objectName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }
    }

    private static void EnsureMaterials()
    {
        if (cyanBalloonMaterial != null &&
            orangeBalloonMaterial != null &&
            darkMaterial != null)
        {
            return;
        }

        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit") ??
            Shader.Find("Standard");

        if (shader == null)
        {
            return;
        }

        cyanBalloonMaterial = new Material(shader);
        cyanBalloonMaterial.color =
            new Color(0.08f, 0.78f, 1f, 1f);

        orangeBalloonMaterial = new Material(shader);
        orangeBalloonMaterial.color =
            new Color(1f, 0.55f, 0.06f, 1f);

        darkMaterial = new Material(shader);
        darkMaterial.color =
            new Color(0.025f, 0.14f, 0.24f, 1f);

        SetGloss(cyanBalloonMaterial);
        SetGloss(orangeBalloonMaterial);
        SetGloss(darkMaterial);
    }

    private static void SetGloss(Material material)
    {
        if (material != null &&
            material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.86f);
        }
    }
}
