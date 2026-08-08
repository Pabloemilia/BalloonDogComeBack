#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Applies the non-secret Android release defaults whenever the project opens.
/// Signing credentials intentionally remain user-owned and are never embedded.
/// </summary>
[InitializeOnLoad]
public static class BalloonDogReleaseSettings
{
    private const string AndroidIdentifier = "com.balloondogstudio.runner";

    static BalloonDogReleaseSettings()
    {
        EditorApplication.delayCall += ApplyReleaseDefaults;
    }

    [MenuItem("Balloon Dog/Play Store/Apply Android Release Defaults")]
    public static void ApplyReleaseDefaults()
    {
        PlayerSettings.companyName = "Balloon Dog Studio";
        PlayerSettings.productName = "Balloon Dog";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.SetApplicationIdentifier(
            NamedBuildTarget.Android,
            AndroidIdentifier);

        PlayerSettings.defaultInterfaceOrientation =
            UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.minSdkVersion =
            AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion =
            (AndroidSdkVersions)36;
        PlayerSettings.Android.targetArchitectures =
            AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(
            NamedBuildTarget.Android,
            ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(
            NamedBuildTarget.Android,
            ManagedStrippingLevel.Low);

        EditorUserBuildSettings.buildAppBundle = true;
        AssetDatabase.SaveAssets();
        Debug.Log(
            "Balloon Dog Android release defaults applied. " +
            "Verify the permanent package ID, then configure your private keystore.");
    }
}
#endif
