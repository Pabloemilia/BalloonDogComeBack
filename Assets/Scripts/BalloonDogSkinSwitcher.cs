using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Balloon Dog/Skin Switcher")]
[DefaultExecutionOrder(32000)]
public sealed class BalloonDogSkinSwitcher : MonoBehaviour
{
    private const string SkinRootName = "BalloonDog_SkinRoot";
    private const string ActivePrefix = "BalloonDog_ActiveSkin_";
    private const string PreviewPrefix = "BalloonDog_Preview_";
    private const string StarterSavedId = "__balloon_dog_default__";
    private const string DefaultPrefsKey = "BalloonDog.SelectedSkin.V5";

    [Header("Otomatik Kurulum")]
    [Tooltip("Kirmizi/orijinal baslangic modeli: BalloonDogModel > default")]
    [SerializeField] private GameObject starterVisual;
    [Tooltip("Dogru prefab pozunu koruyan, dogrudan player altindaki kok")]
    [SerializeField] private Transform skinRoot;
    [SerializeField] private GameObject legacyVisual;
    [SerializeField] private List<GameObject> skinPrefabs = new List<GameObject>();

    [Header("Secimi Hatirla")]
    [SerializeField] private bool rememberSelection = true;
    [SerializeField] private string playerPrefsKey = DefaultPrefsKey;

    [Header("Calisma Bilgisi")]
    [SerializeField, HideInInspector] private int currentSkinIndex = -1;

    private GameObject activeSkinInstance;
    private Bounds starterLocalBounds;
    private bool starterBoundsReady;

    private bool HasStarterVisual => starterVisual != null;
    private int PrefabIndexOffset => HasStarterVisual ? 1 : 0;

    public int CurrentSkinIndex => currentSkinIndex;
    public int SkinCount => PrefabIndexOffset +
                            (skinPrefabs != null ? skinPrefabs.Count : 0);
    public GameObject ActiveSkinInstance => activeSkinInstance;
    public string CurrentSkinName => GetSkinName(currentSkinIndex);

    public event Action<int, GameObject> SkinChanged;

    private void Awake()
    {
        LockSkinRootToPlayer();
        starterBoundsReady = TryGetBoundsRelativeToPlayer(
            starterVisual,
            out starterLocalBounds) && starterLocalBounds.size.y > 0.0001f;

        if (legacyVisual != null && legacyVisual != starterVisual)
        {
            legacyVisual.SetActive(false);
        }

        if (SkinCount == 0)
        {
            Debug.LogError(
                "Balloon Dog: Skin bulunamadi. Play modunu kapatip " +
                "Tools > Balloon Dog > Skin Sistemi V5 > Sabit Konum Kur menusuyle tekrar kur.",
                this);
            return;
        }

        int startIndex = 0;
        if (rememberSelection && PlayerPrefs.HasKey(playerPrefsKey))
        {
            int savedIndex = FindSkinIndex(
                PlayerPrefs.GetString(playerPrefsKey, string.Empty));
            if (savedIndex >= 0)
            {
                startIndex = savedIndex;
            }
        }

        ApplySkinInternal(startIndex, false, true);
    }

    private void LateUpdate()
    {
        // Kosu/juice kodlari ne yaparsa yapsin prefab pozunun referansi degismez.
        // Skin player ile hareket eder; BalloonDogModel'in bob/lean transformunu almaz.
        LockSkinRootToPlayer();
    }

    // Cark veya UI Button OnClick olayindan cagrilir.
    // 0 = kirmizi default, 1 = Caramel, sonrasi diger prefablar.
    public void SelectSkin(int index)
    {
        ApplySkinInternal(index, true, false);
    }

    public void SelectSkinByName(string skinName)
    {
        int index = FindSkinIndex(skinName);
        if (index < 0)
        {
            Debug.LogWarning("Balloon Dog: Skin bulunamadi: " + skinName, this);
            return;
        }

        ApplySkinInternal(index, true, false);
    }

    [ContextMenu("Sonraki Skin")]
    public void NextSkin()
    {
        if (SkinCount == 0)
        {
            return;
        }

        int nextIndex = currentSkinIndex < 0
            ? 0
            : (currentSkinIndex + 1) % SkinCount;

        ApplySkinInternal(nextIndex, true, false);
    }

    [ContextMenu("Onceki Skin")]
    public void PreviousSkin()
    {
        if (SkinCount == 0)
        {
            return;
        }

        int previousIndex = currentSkinIndex <= 0
            ? SkinCount - 1
            : currentSkinIndex - 1;

        ApplySkinInternal(previousIndex, true, false);
    }

    [ContextMenu("Kayitli Secimi Sil ve Kirmizi Default'a Don")]
    public void ResetSavedSelection()
    {
        PlayerPrefs.DeleteKey(playerPrefsKey);
        PlayerPrefs.Save();

        if (Application.isPlaying && SkinCount > 0)
        {
            ApplySkinInternal(0, false, true);
        }
    }

    public string GetSkinName(int index)
    {
        if (!IsValidIndex(index))
        {
            return string.Empty;
        }

        if (HasStarterVisual && index == 0)
        {
            return "default";
        }

        return skinPrefabs[index - PrefabIndexOffset].name;
    }

    private void ApplySkinInternal(int index, bool saveSelection, bool force)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Balloon Dog: Skin degistirmeyi Play modunda dene.", this);
            return;
        }

        if (!IsValidIndex(index))
        {
            Debug.LogWarning("Balloon Dog: Gecersiz skin sirasi: " + index, this);
            return;
        }

        if (!force && currentSkinIndex == index && activeSkinInstance != null)
        {
            return;
        }

        LockSkinRootToPlayer();

        if (starterVisual != null)
        {
            starterVisual.SetActive(false);
        }

        if (legacyVisual != null && legacyVisual != starterVisual)
        {
            legacyVisual.SetActive(false);
        }

        RemoveGeneratedInstances();

        if (HasStarterVisual && index == 0)
        {
            starterVisual.SetActive(true);
            activeSkinInstance = starterVisual;
        }
        else
        {
            int prefabIndex = index - PrefabIndexOffset;
            GameObject prefab = skinPrefabs[prefabIndex];
            activeSkinInstance = Instantiate(prefab, skinRoot, false);
            activeSkinInstance.name = ActivePrefix + CleanSkinName(prefab.name);
            activeSkinInstance.SetActive(true);

            // Kirmizi default'un gorunen boyutunu ve ayak merkezini referans al.
            // Prefabin kayitli rotation'i yon icin korunur; hatali position/scale
            // yerine goruntu sinirlarindan otomatik ve tek tip poz hesaplanir.
            FitInstanceToStarter(activeSkinInstance, prefab);
        }

        currentSkinIndex = index;

        if (rememberSelection && saveSelection)
        {
            string savedId = HasStarterVisual && index == 0
                ? StarterSavedId
                : skinPrefabs[index - PrefabIndexOffset].name;

            PlayerPrefs.SetString(playerPrefsKey, savedId);
            PlayerPrefs.Save();
        }

        SkinChanged?.Invoke(currentSkinIndex, activeSkinInstance);
    }

    private void LockSkinRootToPlayer()
    {
        FindOrCreateSkinRoot();

        if (skinRoot.parent != transform)
        {
            skinRoot.SetParent(transform, false);
        }

        // Bu degerler bilincli olarak her kare kilitlenir. Prefablarin kendi local
        // transformu yalnizca bu sabit referansa gore calisir.
        skinRoot.localPosition = Vector3.zero;
        skinRoot.localRotation = Quaternion.identity;
        skinRoot.localScale = Vector3.one;
    }

    private void FindOrCreateSkinRoot()
    {
        if (skinRoot != null)
        {
            return;
        }

        Transform directRoot = transform.Find(SkinRootName);
        if (directRoot != null)
        {
            skinRoot = directRoot;
            return;
        }

        Transform[] descendants = transform.GetComponentsInChildren<Transform>(true);
        foreach (Transform candidate in descendants)
        {
            if (candidate != transform &&
                string.Equals(candidate.name, SkinRootName, StringComparison.OrdinalIgnoreCase))
            {
                skinRoot = candidate;
                return;
            }
        }

        GameObject rootObject = new GameObject(SkinRootName);
        skinRoot = rootObject.transform;
        skinRoot.SetParent(transform, false);
    }

    private void FitInstanceToStarter(GameObject instance, GameObject prefab)
    {
        Transform instanceTransform = instance.transform;
        Transform prefabTransform = prefab.transform;

        if (!starterBoundsReady)
        {
            instanceTransform.localPosition = prefabTransform.localPosition;
            instanceTransform.localRotation = prefabTransform.localRotation;
            instanceTransform.localScale = prefabTransform.localScale;
            return;
        }

        instanceTransform.localPosition = Vector3.zero;
        instanceTransform.localRotation = prefabTransform.localRotation;
        instanceTransform.localScale = NormalizeScaleShape(prefabTransform.localScale);

        if (!TryGetBoundsRelativeToPlayer(instance, out Bounds sourceBounds) ||
            sourceBounds.size.y <= 0.0001f)
        {
            instanceTransform.localPosition = prefabTransform.localPosition;
            instanceTransform.localScale = prefabTransform.localScale;
            return;
        }

        float uniformScale = starterLocalBounds.size.y / sourceBounds.size.y;
        instanceTransform.localScale *= uniformScale;

        if (!TryGetBoundsRelativeToPlayer(instance, out sourceBounds))
        {
            return;
        }

        Vector3 targetFeetCenter = new Vector3(
            starterLocalBounds.center.x,
            starterLocalBounds.min.y,
            starterLocalBounds.center.z);
        Vector3 sourceFeetCenter = new Vector3(
            sourceBounds.center.x,
            sourceBounds.min.y,
            sourceBounds.center.z);

        instanceTransform.localPosition += targetFeetCenter - sourceFeetCenter;
    }

    private static Vector3 NormalizeScaleShape(Vector3 value)
    {
        float largest = Mathf.Max(
            Mathf.Abs(value.x),
            Mathf.Abs(value.y),
            Mathf.Abs(value.z));

        if (largest <= 0.0001f)
        {
            return Vector3.one;
        }

        Vector3 normalized = value / largest;
        if (Mathf.Abs(normalized.x) <= 0.0001f)
        {
            normalized.x = 1f;
        }

        if (Mathf.Abs(normalized.y) <= 0.0001f)
        {
            normalized.y = 1f;
        }

        if (Mathf.Abs(normalized.z) <= 0.0001f)
        {
            normalized.z = 1f;
        }

        return normalized;
    }

    private bool TryGetBoundsRelativeToPlayer(GameObject visual, out Bounds result)
    {
        result = default;
        if (visual == null)
        {
            return false;
        }

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        bool hasPoint = false;

        foreach (Renderer visualRenderer in renderers)
        {
            Bounds worldBounds = visualRenderer.bounds;
            Vector3 center = worldBounds.center;
            Vector3 extents = worldBounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldCorner = center + Vector3.Scale(
                            extents,
                            new Vector3(x, y, z));
                        Vector3 localCorner = transform.InverseTransformPoint(worldCorner);

                        if (!hasPoint)
                        {
                            result = new Bounds(localCorner, Vector3.zero);
                            hasPoint = true;
                        }
                        else
                        {
                            result.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }

        return hasPoint;
    }

    private void RemoveGeneratedInstances()
    {
        HashSet<GameObject> objectsToRemove = new HashSet<GameObject>();

        if (activeSkinInstance != null && activeSkinInstance != starterVisual)
        {
            objectsToRemove.Add(activeSkinInstance);
        }

        if (skinRoot != null)
        {
            for (int index = 0; index < skinRoot.childCount; index++)
            {
                objectsToRemove.Add(skinRoot.GetChild(index).gameObject);
            }
        }

        Transform[] allChildren = transform.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            bool generated = child.name.StartsWith(ActivePrefix, StringComparison.Ordinal) ||
                             child.name.StartsWith(PreviewPrefix, StringComparison.Ordinal);

            if (generated)
            {
                objectsToRemove.Add(child.gameObject);
            }
        }

        foreach (GameObject objectToRemove in objectsToRemove)
        {
            if (objectToRemove == null ||
                objectToRemove == starterVisual ||
                objectToRemove == legacyVisual)
            {
                continue;
            }

            objectToRemove.SetActive(false);
            DestroyGeneratedObject(objectToRemove);
        }

        activeSkinInstance = null;
    }

    private static void DestroyGeneratedObject(GameObject target)
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPaused)
        {
            UnityEngine.Object.DestroyImmediate(target);
            return;
        }
#endif
        UnityEngine.Object.Destroy(target);
    }

    private int FindSkinIndex(string requestedName)
    {
        if (string.IsNullOrWhiteSpace(requestedName))
        {
            return -1;
        }

        if (HasStarterVisual && IsStarterName(requestedName))
        {
            return 0;
        }

        if (skinPrefabs == null)
        {
            return -1;
        }

        string cleanRequestedName = CleanSkinName(requestedName);

        for (int index = 0; index < skinPrefabs.Count; index++)
        {
            GameObject prefab = skinPrefabs[index];
            if (prefab == null)
            {
                continue;
            }

            if (string.Equals(prefab.name, requestedName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    CleanSkinName(prefab.name),
                    cleanRequestedName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return index + PrefabIndexOffset;
            }
        }

        return -1;
    }

    private bool IsValidIndex(int index)
    {
        if (index < 0 || index >= SkinCount)
        {
            return false;
        }

        if (HasStarterVisual && index == 0)
        {
            return true;
        }

        int prefabIndex = index - PrefabIndexOffset;
        return skinPrefabs != null &&
               prefabIndex >= 0 &&
               prefabIndex < skinPrefabs.Count &&
               skinPrefabs[prefabIndex] != null;
    }

    private bool IsStarterName(string value)
    {
        return string.Equals(value, StarterSavedId, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "default", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "starter", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "baslangic", StringComparison.OrdinalIgnoreCase) ||
               (starterVisual != null &&
                string.Equals(value, starterVisual.name, StringComparison.OrdinalIgnoreCase));
    }

    private static string CleanSkinName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string result = value;
        string[] prefixes = { ActivePrefix, PreviewPrefix, "skin_" };

        foreach (string prefix in prefixes)
        {
            if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(prefix.Length);
            }
        }

        return result;
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        GameObject configuredStarterVisual,
        Transform configuredSkinRoot,
        GameObject configuredLegacyVisual,
        List<GameObject> configuredPrefabs)
    {
        starterVisual = configuredStarterVisual;
        skinRoot = configuredSkinRoot;
        legacyVisual = configuredLegacyVisual;
        skinPrefabs = configuredPrefabs ?? new List<GameObject>();
        playerPrefsKey = DefaultPrefsKey;
        currentSkinIndex = -1;
    }
#endif
}
