using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

// Batch entry points deliberately restricted to a disposable copy. See documentation.
// Running a headset experiment must not silently switch the piano project's input backend.
public static class QuestGestureBuild
{
    private static void RequireCopy()
    {
        if (!Application.isBatchMode || !Environment.GetCommandLineArgs().Contains("-questGestureBuildCopy") ||
            !File.Exists(Path.Combine(Application.dataPath, "../QUEST_GESTURE_BUILD_COPY")))
            throw new InvalidOperationException("Use a disposable project copy with QUEST_GESTURE_BUILD_COPY marker and -questGestureBuildCopy.");
    }
    public static void ConfigureCopy()
    {
        RequireCopy(); QuestGestureSceneBuilder.Create();
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget settings);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(settings, "Assets/QuestGestureXRSettings.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
        }
        if (!settings.HasSettingsForBuildTarget(BuildTargetGroup.Android)) settings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
        if (!settings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android)) settings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        var general = settings.SettingsForBuildTarget(BuildTargetGroup.Android);
        general.InitManagerOnStart = true;
        if (!XRPackageMetadataStore.AssignLoader(general.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android))
            throw new InvalidOperationException("Could not assign Android OpenXR loader.");
        // This installed SDK exposes settings lookup publicly, but creation only on
        // an internal editor type. Keep the version-sensitive factory in this build helper.
        var settingsType = typeof(UnityEditor.XR.OpenXR.Features.FeatureHelpers).Assembly.GetType("UnityEditor.XR.OpenXR.OpenXRPackageSettings", true);
        settingsType.GetMethod("GetOrCreateInstance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(null, null);
        var openxr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
        var handTracking = openxr.GetFeature<HandTracking>();
        var quest = openxr.GetFeature<MetaQuestFeature>();
        if (handTracking == null || quest == null) throw new InvalidOperationException("OpenXR hand/Quest features are unavailable.");
        handTracking.enabled = true; quest.enabled = true;
        openxr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        // Reproducible sideload diagnostic with the API 34 SDK already installed.
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.xrmidi.gesturetest");
        PlayerSettings.productName = "XR MIDI Gesture Test";
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        // OpenXR requires the new input backend. This change stays in the copy.
        var player = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        player.FindProperty("activeInputHandler").intValue = 1; player.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings); EditorUtility.SetDirty(general); EditorUtility.SetDirty(general.Manager);
        EditorUtility.SetDirty(handTracking); EditorUtility.SetDirty(quest); EditorUtility.SetDirty(openxr);
        AssetDatabase.SaveAssets();
        Debug.Log("Configured isolated Quest gesture build: Android OpenXR, Meta Quest Support, Hand Tracking Subsystem, ARM64 IL2CPP.");
    }
    public static void BuildCopy()
    {
        RequireCopy();
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/QuestGestureTest.apk"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        if (File.Exists(output)) throw new IOException("Refusing to overwrite existing APK: " + output);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { QuestGestureSceneBuilder.ScenePath }, locationPathName = output,
            target = BuildTarget.Android, options = BuildOptions.Development });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Quest build failed: " + report.summary.result);
        Debug.Log("QUEST_GESTURE_APK=" + output);
    }
}
