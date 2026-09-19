using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Lumen.Core;

namespace Lumen.Narrative
{
    /// <summary>
    /// Mostra a abertura da historia (tempestade solar, perda de contato com a
    /// Terra, satelite sem combustivel) como texto estilo log de sistema, antes
    /// de liberar o controle do jogador. Roda automaticamente ao iniciar a cena
    /// e deixa o GameManager em GameState.Intro ate terminar.
    ///
    /// Enquanto nao existir o sistema de dialogo da NOVA (isso vem com a
    /// Fase 2), esse texto e fixo aqui no codigo. Quando o sistema de dados
    /// (ScriptableObjects) existir, essas linhas devem migrar para la.
    /// </summary>
    public class IntroSequenceController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text logText;
        [SerializeField] private float secondsPerLine = 3.5f;

        [TextArea(2, 4)]
        [SerializeField]
        private string[] lines =
        {
            "SINAL PERDIDO...",
            "Uma tempestade solar atingiu o satelite LUMEN e interrompeu a conexao com a Terra.",
            "Arrastado para longe da rota original, LUMEN ficou sem combustivel no meio do caminho de volta.",
            "BASE TERRA: sinal fraco reestabelecido. Orientando os primeiros passos...",
            "Reative os propulsores e prossiga.",
        };

        private void Start()
        {
            StartCoroutine(RunSequence());
        }

        private IEnumerator RunSequence()
        {
            if (panelRoot != null) panelRoot.SetActive(true);

            foreach (string line in lines)
            {
                if (logText != null) logText.text = line;

                float elapsed = 0f;
                while (elapsed < secondsPerLine)
                {
                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        EndSequence();
                        yield break;
                    }

                    if (Input.anyKeyDown)
                    {
                        break; // avanca para a proxima linha na hora
                    }

                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            EndSequence();
        }

        private void EndSequence()
        {
            if (panelRoot != null) panelRoot.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Playing);
        }

        /// <summary>Usado pelo TutorialSceneBuilder para ligar as referencias de UI sem reflection.</summary>
        public void Configure(GameObject panel, Text text)
        {
            panelRoot = panel;
            logText = text;
        }
    }
}
