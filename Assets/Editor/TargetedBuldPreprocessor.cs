using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TG.Editor
{
    public class TargetedBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 1;

        [Obsolete]
        public void OnPreprocessBuild(BuildReport report) {
            PlayerSettings.bundleVersion = TargetedBuildPipeline.ExtractGitVersion();
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC2;
            PlayerSettings.SetVirtualRealitySupported(BuildTargetGroup.Android, true);
            PlayerSettings.stereoRenderingPath = StereoRenderingPath.SinglePass;
        }
    }

    [InitializeOnLoad]
    public class TargetedBuildPipeline
    {
        static TargetedBuildPipeline() {
            PlayerPrefs.SetInt("OVREngineConfigurationUpdater_Enabled", 0);
            SetupAndroidKeystore();
        }

        static void SetupAndroidKeystore() {
            PlayerSettings.Android.keystorePass = "12345678";
            PlayerSettings.Android.keyaliasName = "alias";
            PlayerSettings.Android.keyaliasPass = "12345678";
        }

        public static string GetBuildDirectory(string platform) => $"Builds/{platform}";
        public static string GetPublishRoot(string platform) => "Temp/Publish/" + platform;
        public static string GetSignaturePath(string platform) => GetPublishRoot(platform) + "/latest.sig";

        public static string ExtractGitVersion() {
            var gitPath = Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Git\cmd\git.exe");
            if (!File.Exists(gitPath)) gitPath = "git.exe";

            var process = Process.Start(new ProcessStartInfo {
                FileName = gitPath,
                Arguments = "log -n 1 --format=\"%H %aI\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            });
            var hashTimestamp = process.StandardOutput.ReadLine().Trim().Split(' ');
            var secondsSince2020 = (DateTime.Parse(hashTimestamp[1]) - DateTime.Parse("2020-01-01T00:00:00Z")).Ticks / TimeSpan.TicksPerSecond;
            return secondsSince2020.ToString("x8") + hashTimestamp[0].Substring(0, 8);
        }

        [PostProcessBuild(800)]
        public static void OnPostprocessBuild(BuildTarget target, string path) {
            string pathIn = Application.dataPath + "/Plugins/Android/AndroidManifest.xml";
            string pathOut = Application.dataPath + "/Plugins/Android/pico/AndroidManifest.xml";

            if (File.Exists(pathIn) && !File.Exists(pathOut))
                File.Move(pathIn, pathOut);

            if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64) {
                var buildDir = Path.GetDirectoryName(path);

                Debug.Log("Writing BuildVersion");
                File.WriteAllText(Path.Combine(buildDir, "BuildVersion"), PlayerSettings.bundleVersion);

                foreach (var subdir in new[] { "AkVirtualCamera.plugin", "ScreenShare", "SoundSwitcher" }) {
                    Debug.Log($"Copying {subdir} to {buildDir}");
                    FileUtil.ReplaceDirectory(subdir, Path.Combine(buildDir, subdir));
                }
            }
            PlayerSettings.bundleVersion = "0000000000000000";
            EditorApplication.ExecuteMenuItem("File/Save Project");
        }

        [MenuItem("Build/Quest")]
        public static void BuildQuest() {
            SetupAndroidKeystore();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = Array.ConvertAll(EditorBuildSettings.scenes, s => s.path),
                targetGroup = BuildTargetGroup.Android,
                target = BuildTarget.Android,
                options = BuildOptions.StrictMode | BuildOptions.Development | BuildOptions.ConnectWithProfiler | BuildOptions.AllowDebugging,
                locationPathName = GetBuildDirectory("quest") + "/Inreal.apk",
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Build failed");
        }

        [MenuItem("Install/Quest")]
        public static void InstallQuest() {
            Install("quest");
        }

        public static void Install(string platform) {
            var apkPath = GetBuildDirectory(platform) + "/Inreal.apk";

            if (!File.Exists(apkPath)) {
                return;
            }

            EditorUtility.DisplayProgressBar("Install", "Pushing APK...", 0f);

            var command = $"push \"{apkPath}\" /data/local/tmp/Inreal.apk";
            var process = Process.Start(new ProcessStartInfo {
                FileName = Path.GetFullPath("EditorTools/adb.exe"),
                Arguments = command,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,

            });

            while (!process.HasExited) {
                var line = process.StandardOutput.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;

                var match = Regex.Match(line, @"(\d+)%");
                if (match.Success)
                    EditorUtility.DisplayProgressBar("Install", "Pushing APK...", float.Parse(match.Groups[1].Value) * 0.01f);
            }
            if (process.ExitCode != 0) {
                Debug.LogError($"[Install] adb {command} exited with code: {process.ExitCode}");
                EditorUtility.ClearProgressBar();
                return;
            }

            var sw = Stopwatch.StartNew();
            EditorUtility.DisplayProgressBar("Install", "Installing APK...", 0f);

            command = $"shell pm install -r /data/local/tmp/Inreal.apk";
            process = Process.Start(new ProcessStartInfo {
                FileName = Path.GetFullPath("EditorTools/adb.exe"),
                Arguments = command,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            });

            while (!process.HasExited) {
                var fakeProgress = sw.ElapsedMilliseconds / 4000f;
                EditorUtility.DisplayProgressBar("Install", "Installing APK...", fakeProgress);
                Thread.Sleep(1);
            }
            if (process.ExitCode != 0) {
                Debug.LogError($"[Install] adb {command} exited with code: {process.ExitCode}");
                EditorUtility.ClearProgressBar();
                return;
            }
            EditorUtility.ClearProgressBar();
            Debug.Log("[Install] Success");
        }
    }
}