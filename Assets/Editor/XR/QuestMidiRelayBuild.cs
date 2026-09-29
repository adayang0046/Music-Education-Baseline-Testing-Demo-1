using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class QuestMidiRelayBuild
{
    private static void RequireCopy()
    {
        if (!Application.isBatchMode || !Environment.GetCommandLineArgs().Contains("-questGestureBuildCopy") ||
            !File.Exists(Path.Combine(Application.dataPath, "../QUEST_GESTURE_BUILD_COPY")))
            throw new InvalidOperationException("Use the explicitly marked disposable build copy.");
    }
    public static void ConfigureCopy()
    {
        RequireCopy(); QuestLessonBuild.ConfigureCopy(); QuestMidiRelaySceneBuilder.Create();
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.xrmidi.questrelay");
        PlayerSettings.productName = "XR MIDI Wi-Fi Lesson";
        PlayerSettings.Android.forceInternetPermission = true;
        AssetDatabase.SaveAssets();
    }
    public static void BuildCopy()
    {
        RequireCopy();
        // Asset synchronization or tests may have restored source-project XR assets.
        // Reapply the isolated Quest configuration immediately before every build.
        ConfigureCopy();
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/QuestMidiRelayDemo.apk"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        if (File.Exists(output)) throw new IOException("Existing APK must be preserved: " + output);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { QuestMidiRelaySceneBuilder.ScenePath },
            locationPathName = output, target = BuildTarget.Android, options = BuildOptions.Development });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Relay build failed: " + report.summary.result);
        Debug.Log("QUEST_MIDI_RELAY_APK=" + output);
    }
}
