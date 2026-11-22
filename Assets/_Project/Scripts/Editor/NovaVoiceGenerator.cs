using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Lumen.Data;
using Lumen.Gameplay;
using Lumen.Narrative;
using Debug = UnityEngine.Debug;

namespace Lumen.EditorTools
{
    /// <summary>
    /// Gera a voz da NOVA offline com a voz pt-BR do Windows (System.Speech, via PowerShell).
    /// Um WAV por fala em Assets/_Project/Resources/NovaVoice, nomeado pelo hash do texto
    /// (ver NovaVoice). Falas que ja tem audio sao puladas; audios de falas que nao existem
    /// mais sao apagados.
    /// </summary>
    public static class NovaVoiceGenerator
    {
        private const string OutFolder = "Assets/_Project/Resources/NovaVoice";

        [MenuItem("LUMEN/Gerar Vozes da NOVA")]
        public static void Generate()
        {
            var lines = CollectLines();
            if (lines.Count == 0)
            {
                Debug.LogWarning("[LUMEN] Nenhuma fala encontrada. Rode LUMEN > Montar Cena de Tutorial primeiro.");
                return;
            }

            Directory.CreateDirectory(OutFolder);
            string absFolder = Path.GetFullPath(OutFolder);

            // Apaga audios de falas que mudaram ou saíram.
            var keep = new HashSet<string>();
            foreach (string line in lines) keep.Add(NovaVoice.Key(line) + ".wav");
            foreach (string file in Directory.GetFiles(absFolder, "*.wav"))
            {
                if (!keep.Contains(Path.GetFileName(file)))
                {
                    File.Delete(file);
                    string meta = file + ".meta";
                    if (File.Exists(meta)) File.Delete(meta);
                }
            }

            // Lista "arquivo|texto" em UTF-8 para o PowerShell (acentos preservados).
            var todo = new StringBuilder();
            int pending = 0;
            foreach (string line in lines)
            {
                string wav = Path.Combine(absFolder, NovaVoice.Key(line) + ".wav");
                if (File.Exists(wav)) continue;
                todo.Append(wav).Append('|').Append(line.Replace("\r", " ").Replace("\n", " ")).Append('\n');
                pending++;
            }

            if (pending == 0)
            {
                Debug.Log($"[LUMEN] Vozes da NOVA já estão em dia ({lines.Count} falas).");
                return;
            }

            string tmp = Path.Combine(Path.GetTempPath(), "lumen_nova_voice");
            Directory.CreateDirectory(tmp);
            string listPath = Path.Combine(tmp, "falas.txt");
            string scriptPath = Path.Combine(tmp, "gerar.ps1");
            File.WriteAllText(listPath, todo.ToString(), new UTF8Encoding(false));
            File.WriteAllText(scriptPath, PowerShellScript, new UTF8Encoding(true)); // BOM: PowerShell 5 le UTF-8

            var psi = new ProcessStartInfo("powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" \"{listPath}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
            };

            try
            {
                EditorUtility.DisplayProgressBar("LUMEN", "Gerando vozes da NOVA...", 0.5f);
                using (var proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    string error = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(180000);
                    Debug.Log("[LUMEN] Vozes da NOVA: " + output.Trim());
                    if (!string.IsNullOrWhiteSpace(error)) Debug.LogWarning("[LUMEN] PowerShell: " + error.Trim());
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
        }

        private static List<string> CollectLines()
        {
            var result = new List<string>();
            var seen = new HashSet<string>();

            void Add(string line)
            {
                if (string.IsNullOrWhiteSpace(line)) return;
                if (seen.Add(NovaVoice.Key(line))) result.Add(line.Trim());
            }

            foreach (string guid in AssetDatabase.FindAssets("t:PhaseData"))
            {
                var phase = AssetDatabase.LoadAssetAtPath<PhaseData>(AssetDatabase.GUIDToAssetPath(guid));
                if (phase == null || phase.NarrationLines == null) continue;
                foreach (string line in phase.NarrationLines) Add(line);
            }

            foreach (string line in PlanetApproachTrigger.CelebrationLines) Add(line);
            return result;
        }

        private const string PowerShellScript = @"
param([string]$ListPath)
Add-Type -AssemblyName System.Speech
$s = New-Object System.Speech.Synthesis.SpeechSynthesizer
$voices = $s.GetInstalledVoices() | Where-Object { $_.Enabled } | ForEach-Object { $_.VoiceInfo }
$pt = $voices | Where-Object { $_.Culture.Name -eq 'pt-BR' }
if (-not $pt) { Write-Output ('ERRO: nenhuma voz pt-BR instalada. Vozes: ' + (($voices | ForEach-Object { $_.Name }) -join ', ')); exit 1 }
$pick = $pt | Where-Object { $_.Gender -eq 'Female' } | Select-Object -First 1
if (-not $pick) { $pick = $pt | Select-Object -First 1 }
$s.SelectVoice($pick.Name)
$s.Rate = -1
$count = 0
foreach ($row in [System.IO.File]::ReadAllLines($ListPath, [System.Text.Encoding]::UTF8)) {
  if ([string]::IsNullOrWhiteSpace($row)) { continue }
  $i = $row.IndexOf('|')
  $path = $row.Substring(0, $i)
  $text = $row.Substring($i + 1)
  $s.SetOutputToWaveFile($path)
  $s.Speak($text)
  $s.SetOutputToNull()
  $count++
}
$s.Dispose()
Write-Output ('Voz: ' + $pick.Name + ' - ' + $count + ' falas geradas.')
";
    }

    /// <summary>Audios da NOVA: mono e comprimidos (Vorbis), para o build ficar leve.</summary>
    public class NovaVoiceImportSettings : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (!assetPath.Contains("Resources/NovaVoice/")) return;

            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
        }
    }
}
