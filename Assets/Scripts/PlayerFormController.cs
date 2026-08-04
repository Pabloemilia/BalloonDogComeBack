using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AirController))]
public sealed class PlayerFormController : MonoBehaviour
{
    [Header("Double Tap")]
    [SerializeField, Min(0.1f)] private float doubleTapWindow = 0.32f;
    [SerializeField, Min(1f)] private float maximumTapDistance = 140f;

    [Header("Helicopter Settings")]
    [SerializeField, Min(1f)] private float flightHeight = 3.8f;
    [SerializeField, Min(0.1f)] private float verticalResponsiveness = 6f;
    [SerializeField, Min(0.1f)] private float maximumVerticalSpeed = 8f;
    [SerializeField, Min(0f)] private float airDrainPerSecond = 18f;
    [SerializeField] private Transform rotorVisual;

    private Rigidbody playerRigidbody;
    private AirController airController;
    private float lastTapTime = -10f;
    private Vector2 lastTapPosition;

    private static Material cyanBalloonMaterial;
    private static Material pinkBalloonMaterial;
    private static Material yellowBalloonMaterial;
    private static Material stringMaterial;

    public bool IsHelicopterActive { get; private set; }

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        airController = GetComponent<AirController>();

        if (rotorVisual != null)
        {
            BuildBalloonRotor();
            PositionRotorOnHead();
            rotorVisual.gameObject.SetActive(false);
        }
    }

    public void ConfigureRotor(Transform rotor)
    {
        rotorVisual = rotor;

        if (rotorVisual == null)
        {
            return;
        }

        BuildBalloonRotor();
        PositionRotorOnHead();
        rotorVisual.gameObject.SetActive(IsHelicopterActive);
    }

    private void Update()
    {
        DetectDoubleTap();

        if (!IsHelicopterActive)
        {
            return;
        }

        bool gameRunning =
            GameManager.Instance == null ||
            !GameManager.Instance.IsGameOver;

        if (!gameRunning ||
            airController == null ||
            airController.IsEmpty)
        {
            SetHelicopterActive(false);
            return;
        }

        // Eski davranış: helikopter saniyede 18 hava tüketir.
        airController.RemoveAir(airDrainPerSecond * Time.deltaTime);

        if (rotorVisual != null)
        {
            PositionRotorOnHead();

            rotorVisual.Rotate(
                0f,
                620f * Time.deltaTime,
                0f,
                Space.Self);
        }
    }

    private void FixedUpdate()
    {
        if (!IsHelicopterActive)
        {
            return;
        }

        Vector3 velocity = playerRigidbody.linearVelocity;
        float heightDifference =
            flightHeight - playerRigidbody.position.y;

        velocity.y = Mathf.Clamp(
            heightDifference * verticalResponsiveness,
            -maximumVerticalSpeed,
            maximumVerticalSpeed);

        playerRigidbody.linearVelocity = velocity;
    }

    public void ToggleHelicopter()
    {
        bool gameRunning =
            GameManager.Instance == null ||
            !GameManager.Instance.IsGameOver;

        if (!gameRunning)
        {
            return;
        }

        if (!IsHelicopterActive &&
            (airController == null || airController.IsEmpty))
        {
            return;
        }

        SetHelicopterActive(!IsHelicopterActive);
    }

    public void ForceBalloonForm()
    {
        SetHelicopterActive(false);
    }

    public void SetHelicopterRequested(bool requested)
    {
        if (requested != IsHelicopterActive)
        {
            SetHelicopterActive(requested);
        }
    }

    private void DetectDoubleTap()
    {
        if (!TryGetPointerDown(
                out Vector2 pointerPosition,
                out int pointerId))
        {
            return;
        }

        if (IsPointerOverInterface(pointerId))
        {
            lastTapTime = -10f;
            return;
        }

        float now = Time.unscaledTime;
        bool withinTime =
            now - lastTapTime <= doubleTapWindow;

        bool withinDistance =
            Vector2.Distance(
                pointerPosition,
                lastTapPosition) <= maximumTapDistance;

        if (withinTime && withinDistance)
        {
            lastTapTime = -10f;
            ToggleHelicopter();
            return;
        }

        lastTapTime = now;
        lastTapPosition = pointerPosition;
    }

    private static bool TryGetPointerDown(
        out Vector2 pointerPosition,
        out int pointerId)
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            pointerPosition =
                Touchscreen.current.primaryTouch.position.ReadValue();

            pointerId =
                Touchscreen.current.primaryTouch.touchId.ReadValue();

            return true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            pointerPosition =
                Mouse.current.position.ReadValue();

            pointerId = -1;
            return true;
        }

        pointerPosition = default;
        pointerId = -1;
        return false;
    }

    private static bool IsPointerOverInterface(int pointerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return pointerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(pointerId)
            : EventSystem.current.IsPointerOverGameObject();
    }

    private void SetHelicopterActive(bool active)
    {
        if (IsHelicopterActive != active)
        {
            GameAudioController.PlayTransform();

            RuntimeVfx.SpawnBurst(
                transform.position + Vector3.up * 1.1f,
                active
                    ? new Color(0.12f, 0.86f, 1f, 1f)
                    : new Color(1f, 0.72f, 0.16f, 1f),
                14,
                2.6f,
                0.12f,
                0.5f);
        }

        IsHelicopterActive = active;
        playerRigidbody.useGravity = !active;

        if (active)
        {
            PositionRotorOnHead();
        }
        else
        {
            Vector3 velocity = playerRigidbody.linearVelocity;
            velocity.y = Mathf.Min(velocity.y, 0f);
            playerRigidbody.linearVelocity = velocity;
        }

        if (rotorVisual != null)
        {
            rotorVisual.gameObject.SetActive(active);
        }
    }

    private void BuildBalloonRotor()
    {
        if (rotorVisual == null)
        {
            return;
        }

        for (int index = rotorVisual.childCount - 1;
             index >= 0;
             index--)
        {
            Transform child = rotorVisual.GetChild(index);
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        EnsureBalloonMaterials();

        rotorVisual.localRotation = Quaternion.identity;
        rotorVisual.localScale = Vector3.one;

        // Şişirilmiş yuvarlak göbek.
        CreateBalloonSphere(
            rotorVisual,
            "BalloonRotorHub",
            Vector3.zero,
            new Vector3(0.46f, 0.30f, 0.46f),
            yellowBalloonMaterial);

        // Dört ayrı uzun kapsül: tek parça çapraz çubuk yerine gerçek balon kolları.
        CreateBalloonCapsule(
            rotorVisual,
            "FrontBalloonBlade",
            new Vector3(0f, 0f, 0.76f),
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(0.28f, 0.72f, 0.28f),
            cyanBalloonMaterial);

        CreateBalloonCapsule(
            rotorVisual,
            "BackBalloonBlade",
            new Vector3(0f, 0f, -0.76f),
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(0.28f, 0.72f, 0.28f),
            cyanBalloonMaterial);

        CreateBalloonCapsule(
            rotorVisual,
            "RightBalloonBlade",
            new Vector3(0.76f, 0f, 0f),
            Quaternion.Euler(0f, 0f, 90f),
            new Vector3(0.28f, 0.72f, 0.28f),
            pinkBalloonMaterial);

        CreateBalloonCapsule(
            rotorVisual,
            "LeftBalloonBlade",
            new Vector3(-0.76f, 0f, 0f),
            Quaternion.Euler(0f, 0f, 90f),
            new Vector3(0.28f, 0.72f, 0.28f),
            pinkBalloonMaterial);

        // Kafaya bağlanan balon düğümü ve kısa ip.
        CreateBalloonSphere(
            rotorVisual,
            "BalloonKnot",
            new Vector3(0f, -0.25f, 0f),
            new Vector3(0.18f, 0.22f, 0.18f),
            yellowBalloonMaterial);

        CreateBalloonCapsule(
            rotorVisual,
            "RotorString",
            new Vector3(0f, -0.60f, 0f),
            Quaternion.identity,
            new Vector3(0.055f, 0.30f, 0.055f),
            stringMaterial);
    }

    private void PositionRotorOnHead()
    {
        if (rotorVisual == null)
        {
            return;
        }

        Transform model =
            transform.Find("BalloonDogModel");

        float topY = 1.40f;
        float centerX = 0f;
        float centerZ = 0f;

        if (model != null)
        {
            Renderer[] renderers =
                model.GetComponentsInChildren<Renderer>(true);

            bool hasBounds = false;
            Bounds combinedBounds = default;

            foreach (Renderer modelRenderer in renderers)
            {
                if (modelRenderer == null ||
                    !modelRenderer.enabled ||
                    modelRenderer is TrailRenderer)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = modelRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(modelRenderer.bounds);
                }
            }

            if (hasBounds)
            {
                Vector3 worldHeadTop = new Vector3(
                    combinedBounds.center.x,
                    combinedBounds.max.y,
                    combinedBounds.center.z);

                Vector3 localHeadTop =
                    transform.InverseTransformPoint(worldHeadTop);

                centerX = localHeadTop.x;
                centerZ = localHeadTop.z;
                topY = localHeadTop.y + 0.26f;
            }
        }

        rotorVisual.localPosition =
            new Vector3(centerX, topY, centerZ);
    }

    private static void CreateBalloonSphere(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject part =
            GameObject.CreatePrimitive(PrimitiveType.Sphere);

        ConfigurePart(
            part,
            parent,
            objectName,
            localPosition,
            Quaternion.identity,
            localScale,
            material);
    }

    private static void CreateBalloonCapsule(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Material material)
    {
        GameObject part =
            GameObject.CreatePrimitive(PrimitiveType.Capsule);

        ConfigurePart(
            part,
            parent,
            objectName,
            localPosition,
            localRotation,
            localScale,
            material);
    }

    private static void ConfigurePart(
        GameObject part,
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Material material)
    {
        part.name = objectName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = localScale;

        Renderer renderer = part.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        Collider collider = part.GetComponent<Collider>();

        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }
    }

    private static void EnsureBalloonMaterials()
    {
        if (cyanBalloonMaterial != null &&
            pinkBalloonMaterial != null &&
            yellowBalloonMaterial != null &&
            stringMaterial != null)
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

        cyanBalloonMaterial =
            CreateGlossyMaterial(
                shader,
                "BalloonRotor_Cyan",
                new Color(0.06f, 0.82f, 1f, 1f));

        pinkBalloonMaterial =
            CreateGlossyMaterial(
                shader,
                "BalloonRotor_Pink",
                new Color(1f, 0.30f, 0.60f, 1f));

        yellowBalloonMaterial =
            CreateGlossyMaterial(
                shader,
                "BalloonRotor_Yellow",
                new Color(1f, 0.72f, 0.10f, 1f));

        stringMaterial =
            CreateGlossyMaterial(
                shader,
                "BalloonRotor_String",
                new Color(0.92f, 0.92f, 1f, 1f));
    }

    private static Material CreateGlossyMaterial(
        Shader shader,
        string materialName,
        Color color)
    {
        Material material = new Material(shader);
        material.name = materialName;
        material.color = color;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.92f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0.02f);
        }

        return material;
    }

    private void OnDisable()
    {
        IsHelicopterActive = false;

        if (playerRigidbody != null)
        {
            playerRigidbody.useGravity = true;
        }

        if (rotorVisual != null)
        {
            rotorVisual.gameObject.SetActive(false);
        }
    }
}
