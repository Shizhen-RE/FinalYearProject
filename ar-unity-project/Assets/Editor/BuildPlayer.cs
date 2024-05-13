using System;
using System.IO;
using UnityEditor;

public class BuildPlayer
{
    /*
     * This function can be invoked by passing "-executeMethod BuildPlayer.Do"
     * on the command line. The player can be installed and run on your Android
     * device by passing "-autoRunPlayer" on the command line. To edit
     * Android-specific build settings, go to "File -> Build Settings..." in the
     * GUI. This will update "Library/EditorUserBuildSettings.asset".
     */
    public static void Do()
    {
        string buildLocation = "test.apk";

        bool autoRunPlayer = false;
        bool batchmode = false;
        string[] argv = Environment.GetCommandLineArgs();
        foreach (string arg in argv) {
            if (arg.Equals("-autoRunPlayer")) {
                autoRunPlayer = true;
            } else if (arg.Equals("-batchmode")) {
                batchmode = true;
            }
        }

        // NOTE: assumes all scenes are under Assets/Scenes
        string scenesDir = String.Format("Assets{0}Scenes{0}", Path.DirectorySeparatorChar);
        string[] scenes =  Directory.GetFiles(scenesDir, "*.unity");

        // NOTE: the opening scene must come first
        string firstScene = scenesDir + "LoginScene.unity";
        scenes[Array.IndexOf(scenes, firstScene)] = scenes[0];
        scenes[0] = firstScene;

        if (!batchmode) {
            EditorUserBuildSettings.SetBuildLocation(BuildTarget.Android, buildLocation);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.locationPathName = buildLocation;
        buildPlayerOptions.options = autoRunPlayer ? BuildOptions.AutoRunPlayer : BuildOptions.None;
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.targetGroup = BuildTargetGroup.Android;

        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
}
