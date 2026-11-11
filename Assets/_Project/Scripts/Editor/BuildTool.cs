#if UNITY_EDITOR
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Lumen.EditorTools
{
    /// <summary>
    /// Gera a build para Windows (64 bits) na pasta Downloads do usuario e compacta em .zip
    /// (para entregar ao professor). Menu: LUMEN > Gerar Build Windows (Downloads).
    /// </summary>
    public static class BuildTool
    {
        private const string ScenePath = "Assets/Scenes/Game.unity";
        private const string BuildName = "OrbitaPerdida_Build";

        /// <summary>Abre o .exe da ultima build gerada (para testar fora do Editor).</summary>
        [MenuItem("LUMEN/Abrir Build Gerada")]
        public static void OpenBuiltGame()
        {
            string exe = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                "Downloads", BuildName, "OrbitaPerdida.exe");
            if (!File.Exists(exe))
            {
                Debug.LogWarning("[LUMEN] Build não encontrada em " + exe + ". Rode 'Gerar Build Windows' antes.");
                return;
            }
            System.Diagnostics.Process.Start(exe);
        }

        [MenuItem("LUMEN/Gerar Build Windows (Downloads)")]
        public static void BuildWindowsToDownloads()
        {
            string downloads = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads");
            string folder = Path.Combine(downloads, BuildName);
            string zipPath = Path.Combine(downloads, BuildName + ".zip");

            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            if (File.Exists(zipPath)) File.Delete(zipPath);
            Directory.CreateDirectory(folder);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(folder, "OrbitaPerdida.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[LUMEN] Build falhou: {report.summary.result} ({report.summary.totalErrors} erros).");
                return;
            }

            ZipFile.CreateFromDirectory(folder, zipPath, System.IO.Compression.CompressionLevel.Optimal, true);
            long mb = new FileInfo(zipPath).Length / (1024 * 1024);
            Debug.Log($"[LUMEN] BUILD OK. Pasta: {folder} | ZIP: {zipPath} ({mb} MB)");
        }
    }
}
#endif
