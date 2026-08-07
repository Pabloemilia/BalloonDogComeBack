using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the selected store skin to the BalloonDog model without duplicating
/// materials. The MaterialPropertyBlock keeps the scene's shared materials safe.
/// </summary>
public sealed class BalloonDogSkinSystem : MonoBehaviour
{
    private const string RuntimeObjectName = "__BalloonDogSkinSystem";
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeSystem()
    {
        GameObject previous = GameObject.Find(RuntimeObjectName);
        if (previous != null)
        {
            return;
        }

        GameObject runtimeObject = new GameObject(RuntimeObjectName);
        DontDestroyOnLoad(runtimeObject);
        runtimeObject.AddComponent<BalloonDogSkinSystem>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private IEnumerator Start()
    {
        BalloonDogEconomy.Changed += ApplySelectedSkin;

        yield return ApplyAfterSceneSetup();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ApplyAfterSceneSetup());
    }

    private static IEnumerator ApplyAfterSceneSetup()
    {

        for (int i = 0; i < 4; i++)
        {
            yield return null;
        }

        ApplySelectedSkin();
    }

    public static void ApplySelectedSkin()
    {
        PlayerRunner player = FindAnyObjectByType<PlayerRunner>();
        if (player == null)
        {
            return;
        }

        Transform modelRoot = FindChildRecursive(player.transform, "BalloonDogModel");
        if (modelRoot == null)
        {
            modelRoot = player.transform;
        }

        Color color = BalloonDogEconomy.EquippedSkin.PrimaryColor;
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        foreach (Renderer modelRenderer in modelRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (modelRenderer == null)
            {
                continue;
            }

            modelRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            modelRenderer.SetPropertyBlock(block);
        }
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        BalloonDogEconomy.Changed -= ApplySelectedSkin;
    }
}
