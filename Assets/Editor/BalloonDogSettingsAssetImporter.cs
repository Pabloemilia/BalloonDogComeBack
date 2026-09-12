using UnityEditor;
using UnityEngine;

/// <summary>Imports the supplied settings artwork with stable UI settings.</summary>
public sealed class BalloonDogSettingsAssetImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/SettingsUI/")) return;
        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.spritePixelsPerUnit = 100f;
        if (assetPath.EndsWith("Blue.png") || assetPath.EndsWith("Mint.png"))
            importer.spriteBorder = new Vector4(170f, 0f, 170f, 0f);
    }
}
