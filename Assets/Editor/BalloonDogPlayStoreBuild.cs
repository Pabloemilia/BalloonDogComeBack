#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BalloonDogPlayStoreBuild
{
    private const string OutputPath = "Builds/Android/BalloonDog.aab";

    [MenuItem("Balloon Dog/Play Store/Validate Release Setup")]
    public static void ValidateReleaseSetup()
    {
        BalloonDogReleaseSettings.ApplyReleaseDefaults();
        string[] problems = GetProblems();
        if (problems.Length == 0)
        {
            Debug.Log("Balloon Dog: Play Store project settings are ready. Release keystore is configured.");
            return;
        }

        Debug.LogError("Balloon Dog release validation:\n- " + string.Join("\n- ", problems));
    }

    [MenuItem("Balloon Dog/Play Store/Build Signed AAB")]
    public static void BuildSignedAab()
    {
        BalloonDogReleaseSettings.ApplyReleaseDefaults();
        string[] problems = GetProblems();
        if (problems.Length > 0)
        {
            throw new InvalidOperationException(
                "Release build stopped:\n- " + string.Join("\n- ", problems));
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? "Builds/Android");
        EditorUserBuildSettings.buildAppBundle = true;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.CompressWithLz4HC
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                "AAB build failed: " + report.summary.result +
                " (errors: " + report.summary.totalErrors + ")");
        }

        Debug.Log("Signed Play Store bundle created: " + OutputPath);
        EditorUtility.RevealInFinder(OutputPath);
    }

    private static string[] GetProblems()
    {
        System.Collections.Generic.List<string> problems =
            new System.Collections.Generic.List<string>();

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            problems.Add("Switch the active platform to Android in Build Profiles.");
        }
        if (!PlayerSettings.Android.useCustomKeystore)
        {
            problems.Add("Create/select your private release keystore in Player Settings > Publishing Settings.");
        }
        if (string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName) ||
            string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName))
        {
            problems.Add("Keystore name and key alias must be configured.");
        }
        if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
        {
            problems.Add("Android scripting backend must be IL2CPP.");
        }
        if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
        {
            problems.Add("ARM64 architecture must be enabled.");
        }
        if (PlayerSettings.Android.minSdkVersion != AndroidSdkVersions.AndroidApiLevel26)
        {
            problems.Add("Android minimum SDK must be API 26.");
        }
        if (PlayerSettings.Android.targetSdkVersion != (AndroidSdkVersions)36)
        {
            problems.Add("Android target SDK must be API 36 for the current Play requirement.");
        }
        if (PlayerSettings.Android.bundleVersionCode < 1 ||
            string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion))
        {
            problems.Add("Set a valid semantic version and Android version code.");
        }
        if (Application.identifier.Contains("UnityTechnologies") ||
            Application.identifier.Contains("template"))
        {
            problems.Add("Replace the template package identifier with your permanent unique identifier.");
        }
        if (EditorBuildSettings.scenes.All(scene => !scene.enabled))
        {
            problems.Add("At least one scene must be enabled in Build Settings.");
        }

        return problems.ToArray();
    }
}
#endif
