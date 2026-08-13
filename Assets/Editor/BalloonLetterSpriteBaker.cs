#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class BalloonLetterSpriteBaker
{
    private const string SourceFolder = "Assets/Balloons/prefab/Baloons";
    private const string OutputFolder = "Assets/Resources/BalloonLetterSprites";
    private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int RenderSize = 512;
    private const int RenderLayer = 31;
    private const string SessionKey = "BalloonDog.IsolatedBalloonBakeV5_AFacingFix";

    static BalloonLetterSpriteBaker()
    {
        EditorApplication.delayCall += AutoRebuildOnce;
    }

    private static void AutoRebuildOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += AutoRebuildOnce;
            return;
        }

        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(SourceFolder))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        RebuildAllClean();
    }

    [MenuItem("Tools/Balloon Dog/Rebuild Balloon UI Letters CLEAN")]
    public static void RebuildAllClean()
    {
        if (!AssetDatabase.IsValidFolder(SourceFolder))
        {
            Debug.LogError("Balloon Dog: source folder missing: " + SourceFolder);
            return;
        }

        EnsureOutputFolder();
        DeleteOldPngs();

        int baked = 0;
        for (int i = 0; i < Characters.Length; i++)
        {
            if (BakeCharacter(Characters[i], i))
            {
                baked++;
            }
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("Balloon Dog: mirror-fixed clean bake complete. " + baked + "/" + Characters.Length + " letters created.");
    }

    private static bool BakeCharacter(char character, int index)
    {
        string prefabPath = SourceFolder + "/Balloon_" + character + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("Balloon Dog: missing prefab " + prefabPath);
            return false;
        }

        GameObject instance = null;
        GameObject cameraObject = null;
        GameObject keyLightObject = null;
        GameObject fillLightObject = null;
        RenderTexture renderTexture = null;
        Texture2D capture = null;
        Texture2D trimmed = null;

        AmbientMode oldAmbientMode = RenderSettings.ambientMode;
        Color oldAmbientLight = RenderSettings.ambientLight;

        try
        {
            // IMPORTANT: render far away from the actual game scene. The old baker
            // used world origin, so gameplay objects on the same layer could appear
            // in the letter PNGs (this is where the tiny dogs came from).
            Vector3 bakeOrigin = new Vector3(10000f + index * 25f, 10000f, 10000f);

            instance = Object.Instantiate(prefab);
            instance.name = "__BalloonLetterBake_" + character;
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.transform.position = bakeOrigin;
            // The A mesh faces the opposite direction in the source FBX. Rendering
            // it from the shared camera side produced the nearly black A seen in
            // the menu, so turn only that glyph before baking its sprite.
            instance.transform.rotation = character == 'A'
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            SetLayerRecursively(instance, RenderLayer);

            Bounds bounds = CalculateBounds(instance);
            if (bounds.size.sqrMagnitude <= 0.000001f)
            {
                Debug.LogError("Balloon Dog: no renderer bounds for " + character);
                return false;
            }

            cameraObject = new GameObject("__BalloonLetterBakeCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.cullingMask = 1 << RenderLayer;
            camera.orthographic = true;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;

            float maxHalf = Mathf.Max(bounds.extents.x, bounds.extents.y);
            camera.orthographicSize = Mathf.Max(0.01f, maxHalf * 1.16f);
            float distance = Mathf.Max(4f, bounds.extents.z + 3f);
            camera.transform.position = bounds.center + new Vector3(0f, 0f, -distance);
            camera.transform.rotation = Quaternion.identity;

            // Dedicated lighting for consistent gold balloon rendering.
            keyLightObject = new GameObject("__BalloonLetterKeyLight");
            keyLightObject.hideFlags = HideFlags.HideAndDontSave;
            Light keyLight = keyLightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.15f;
            keyLight.color = Color.white;
            keyLight.cullingMask = 1 << RenderLayer;
            keyLight.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

            fillLightObject = new GameObject("__BalloonLetterFillLight");
            fillLightObject.hideFlags = HideFlags.HideAndDontSave;
            Light fillLight = fillLightObject.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.42f;
            fillLight.color = new Color(0.75f, 0.85f, 1f, 1f);
            fillLight.cullingMask = 1 << RenderLayer;
            fillLight.transform.rotation = Quaternion.Euler(-25f, 150f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.32f, 0.32f, 1f);

            renderTexture = new RenderTexture(RenderSize, RenderSize, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
                name = "__BalloonLetterRT_" + character
            };
            renderTexture.Create();
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            capture = new Texture2D(RenderSize, RenderSize, TextureFormat.RGBA32, false, false);
            capture.ReadPixels(new Rect(0, 0, RenderSize, RenderSize), 0, 0, false);
            capture.Apply(false, false);
            RenderTexture.active = previous;

            trimmed = TrimTransparent(capture, 18);
            if (trimmed == null)
            {
                Debug.LogError("Balloon Dog: transparent/empty capture for " + character);
                return false;
            }

            // The source balloon prefabs render mirrored from the bake camera side.
            // Flip the final PNG pixels horizontally so the UI sprite reads correctly.
            Texture2D corrected = FlipHorizontal(trimmed);
            if (corrected == null)
            {
                Debug.LogError("Balloon Dog: failed horizontal flip for " + character);
                return false;
            }

            string outputPath = OutputFolder + "/Balloon_" + character + ".png";
            File.WriteAllBytes(outputPath, corrected.EncodeToPNG());
            Object.DestroyImmediate(corrected);
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.sRGBTexture = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = 100f;
                importer.SaveAndReimport();
            }

            return true;
        }
        finally
        {
            RenderSettings.ambientMode = oldAmbientMode;
            RenderSettings.ambientLight = oldAmbientLight;

            if (trimmed != null) Object.DestroyImmediate(trimmed);
            if (capture != null) Object.DestroyImmediate(capture);
            if (renderTexture != null)
            {
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }
            if (fillLightObject != null) Object.DestroyImmediate(fillLightObject);
            if (keyLightObject != null) Object.DestroyImmediate(keyLightObject);
            if (cameraObject != null) Object.DestroyImmediate(cameraObject);
            if (instance != null) Object.DestroyImmediate(instance);
        }
    }


    private static Texture2D FlipHorizontal(Texture2D source)
    {
        if (source == null)
        {
            return null;
        }

        int width = source.width;
        int height = source.height;
        Color32[] input = source.GetPixels32();
        Color32[] output = new Color32[input.Length];

        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                output[row + x] = input[row + (width - 1 - x)];
            }
        }

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        result.SetPixels32(output);
        result.Apply(false, false);
        return result;
    }

    private static Texture2D TrimTransparent(Texture2D source, int padding)
    {
        Color32[] pixels = source.GetPixels32();
        int width = source.width;
        int height = source.height;
        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color32 pixel = pixels[y * width + x];
                if (pixel.a <= 4)
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return null;
        }

        minX = Mathf.Max(0, minX - padding);
        minY = Mathf.Max(0, minY - padding);
        maxX = Mathf.Min(width - 1, maxX + padding);
        maxY = Mathf.Min(height - 1, maxY + padding);

        int trimmedWidth = maxX - minX + 1;
        int trimmedHeight = maxY - minY + 1;
        Texture2D result = new Texture2D(trimmedWidth, trimmedHeight, TextureFormat.RGBA32, false, false);
        result.SetPixels(source.GetPixels(minX, minY, trimmedWidth, trimmedHeight));
        result.Apply(false, false);
        return result;
    }

    private static void DeleteOldPngs()
    {
        string absolute = Path.GetFullPath(OutputFolder);
        if (!Directory.Exists(absolute))
        {
            return;
        }

        string[] pngs = Directory.GetFiles(absolute, "*.png", SearchOption.TopDirectoryOnly);
        foreach (string png in pngs)
        {
            string assetPath = png.Replace('\\', '/');
            int assetsIndex = assetPath.IndexOf("/Assets/");
            if (assetsIndex >= 0)
            {
                assetPath = assetPath.Substring(assetsIndex + 1);
                AssetDatabase.DeleteAsset(assetPath);
            }
        }
    }

    private static void EnsureOutputFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "BalloonLetterSprites");
        }
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(root.transform.position, Vector3.zero);
        bool initialized = false;
        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled)
            {
                renderer.enabled = true;
            }

            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return bounds;
    }
}
#endif
