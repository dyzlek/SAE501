using UnityEditor;
using UnityEngine;

namespace SAE.EditorTools
{
    // Construit une version Windows du prototype dans Unity/Builds/Demo (ignoré par Git).
    // Lancer ensuite : Builds/Demo/Prototype.exe -demo <dossier>  → captures d'écran automatiques.
    public static class DemoBuild
    {
        [MenuItem("SAE/Construire la démo Windows")]
        public static void Build()
        {
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Project/Scenes/Jeu.unity" },
                locationPathName = "Builds/Demo/Prototype.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            Debug.Log("Build démo : " + report.summary.result);
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
