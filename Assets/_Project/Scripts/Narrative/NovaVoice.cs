using UnityEngine;

namespace Lumen.Narrative
{
    /// <summary>
    /// Liga cada fala da NOVA ao seu audio. O arquivo fica em Resources/NovaVoice e o nome vem
    /// de um hash do TEXTO da fala: se o texto mudar, o audio antigo deixa de valer e basta
    /// rodar LUMEN > Gerar Vozes da NOVA de novo. Sem audio, a fala segue so com legenda.
    /// </summary>
    public static class NovaVoice
    {
        public const string ResourcesFolder = "NovaVoice";

        /// <summary>Hash estavel (FNV-1a) do texto, usado como nome do arquivo de audio.</summary>
        public static string Key(string line)
        {
            uint hash = 2166136261u;
            string text = (line ?? string.Empty).Trim();
            for (int i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= 16777619u;
            }
            return "nova_" + hash.ToString("x8");
        }

        public static AudioClip Load(string line)
        {
            return Resources.Load<AudioClip>(ResourcesFolder + "/" + Key(line));
        }
    }
}
