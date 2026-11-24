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
    /// Gera a voz da NOVA com a voz neural pt-BR da Microsoft (Francisca), via pacote edge-tts
    /// do Python. Na primeira vez instala o edge-tts (pip). Precisa de internet so ao gerar.
    /// Um MP3 por fala em Assets/_Project/Resources/NovaVoice, nomeado pelo hash do texto
    /// (ver NovaVoice). Refaz todos a cada execucao e apaga audios de falas que nao existem mais.
    /// </summary>
    public static class NovaVoiceGenerator
    {
        private const string OutFolder = "Assets/_Project/Resources/NovaVoice";
        private const string Voice = "pt-BR-FranciscaNeural"; // alternativa masculina: pt-BR-AntonioNeural
        private const string Rate = "-4%";                     // um pouco mais devagar: soa mais natural

        [MenuItem("LUMEN/Gerar Vozes da NOVA")]
        public static void Generate()
        {
            var lines = CollectLines();
            if (lines.Count == 0)
            {
                Debug.LogWarning("[LUMEN] Nenhuma fala encontrada. Rode LUMEN > Montar Cena de Tutorial primeiro.");
                return;
            }

            string python = FindPython();
            if (python == null)
            {
                Debug.LogError("[LUMEN] Python 3 nao encontrado no PC. Instale o Python (python.org) e rode o menu de novo.");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("LUMEN", "Instalando/atualizando o edge-tts...", 0.05f);
                var pip = Run(python, "-m pip install --user --upgrade edge-tts", 300000);
                if (pip.exit != 0)
                {
                    Debug.LogError("[LUMEN] Falha ao instalar o edge-tts: " + pip.err);
                    return;
                }

                Directory.CreateDirectory(OutFolder);
                string absFolder = Path.GetFullPath(OutFolder);

                // Apaga audios antigos (de outras vozes ou de falas que mudaram).
                var keep = new HashSet<string>();
                foreach (string line in lines) keep.Add(NovaVoice.Key(line) + ".mp3");
                foreach (string file in Directory.GetFiles(absFolder))
                {
                    string name = Path.GetFileName(file);
                    bool audio = name.EndsWith(".wav") || name.EndsWith(".mp3") || name.EndsWith(".wav.meta") || name.EndsWith(".mp3.meta");
                    if (!audio) continue;
                    if (keep.Contains(name) || keep.Contains(name.Replace(".meta", ""))) continue;
                    File.Delete(file);
                }

                string tmp = Path.Combine(Path.GetTempPath(), "lumen_nova_voice");
                Directory.CreateDirectory(tmp);
                string textFile = Path.Combine(tmp, "fala.txt");

                int done = 0;
                for (int i = 0; i < lines.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("LUMEN", $"Gerando voz da NOVA ({i + 1}/{lines.Count})...", 0.1f + 0.9f * i / lines.Count);
                    string mp3 = Path.Combine(absFolder, NovaVoice.Key(lines[i]) + ".mp3");
                    File.WriteAllText(textFile, lines[i], new UTF8Encoding(false));

                    // Ate 3 tentativas: o servico online pode falhar de vez em quando.
                    for (int attempt = 0; attempt < 3 && !IsValid(mp3); attempt++)
                    {
                        var r = Run(python, $"-m edge_tts --voice {Voice} --rate={Rate} --file \"{textFile}\" --write-media \"{mp3}\"", 90000);
                        if (!IsValid(mp3) && attempt == 2) Debug.LogWarning("[LUMEN] edge-tts falhou em: " + lines[i] + "\n" + r.err);
                    }
                    if (IsValid(mp3)) done++;
                }

                Debug.Log($"[LUMEN] Vozes da NOVA ({Voice}): {done}/{lines.Count} falas geradas.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
        }

        /// <summary>Audio valido: existe e nao esta vazio (o edge-tts as vezes deixa um arquivo de 0 bytes).</summary>
        private static bool IsValid(string path)
        {
            if (!File.Exists(path)) return false;
            if (new FileInfo(path).Length > 1000) return true;
            File.Delete(path);
            return false;
        }

        /// <summary>Acha um Python 3: tenta o launcher "py" e depois "python".</summary>
        private static string FindPython()
        {
            foreach (string candidate in new[] { "py", "python", "python3" })
            {
                try
                {
                    var r = Run(candidate, "--version", 15000);
                    if (r.exit == 0 && (r.output + r.err).Contains("Python 3")) return candidate;
                }
                catch (System.ComponentModel.Win32Exception) { /* nao esta no PATH */ }
            }
            return null;
        }

        private static (int exit, string output, string err) Run(string file, string args, int timeoutMs)
        {
            var psi = new ProcessStartInfo(file, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            using (var proc = Process.Start(psi))
            {
                // Le a saida de erro em paralelo para o buffer nunca encher e travar o processo.
                string err = "";
                var errTask = System.Threading.Tasks.Task.Run(() => err = proc.StandardError.ReadToEnd());
                string output = proc.StandardOutput.ReadToEnd();
                if (!proc.WaitForExit(timeoutMs)) { try { proc.Kill(); } catch { } }
                errTask.Wait(2000);
                return (proc.HasExited ? proc.ExitCode : -1, output, err);
            }
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
