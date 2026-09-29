using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class QuestLessonBuild
{
    private static void RequireCopy()
    {
        if (!Application.isBatchMode || !Environment.GetCommandLineArgs().Contains("-questGestureBuildCopy") ||
            !File.Exists(Path.Combine(Application.dataPath, "../QUEST_GESTURE_BUILD_COPY")))
            throw new InvalidOperationException("Quest lesson build must run in the explicitly marked disposable copy.");
    }
    public static void ConfigureCopy()
    {
        RequireCopy(); QuestGestureBuild.ConfigureCopy(); QuestLessonSceneBuilder.Create();
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.xrmidi.questlesson");
        PlayerSettings.productName = "XR MIDI Quest Lesson";
        AssetDatabase.SaveAssets();
    }
    public static void BuildCopy()
    {
        RequireCopy();
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/QuestLessonDemo.apk"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        if (File.Exists(output)) throw new IOException("Refusing to overwrite existing APK: " + output);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { QuestLessonSceneBuilder.ScenePath }, locationPathName = output,
            target = BuildTarget.Android, options = BuildOptions.Development });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Quest lesson build failed: " + report.summary.result);
        Debug.Log("QUEST_LESSON_APK=" + output);
    }
}
