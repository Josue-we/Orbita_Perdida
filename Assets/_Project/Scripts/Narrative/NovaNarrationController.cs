using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Lumen.Narrative
{
    /// <summary>
    /// Mostra falas da NOVA como legenda na parte de baixo da tela, sem cobrir
    /// o jogo (diferente do IntroSequenceController, que e tela cheia).
    /// Chame Play(linhas, callback) quando o LUMEN se aproxima de um planeta.
    /// </summary>
    public class NovaNarrationController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text captionText;
        [SerializeField] private float secondsPerLine = 3.5f;

        public void Play(string[] lines, Action onFinished)
        {
            StartCoroutine(RunSequence(lines, onFinished));
        }

        private IEnumerator RunSequence(string[] lines, Action onFinished)
        {
            if (panelRoot != null) panelRoot.SetActive(true);

            foreach (string line in lines)
            {
                if (captionText != null) captionText.text = line;

                float elapsed = 0f;
                while (elapsed < secondsPerLine)
                {
                    if (Input.anyKeyDown) break; // avanca a linha na hora
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            if (panelRoot != null) panelRoot.SetActive(false);
            onFinished?.Invoke();
        }

        /// <summary>Usado pelo TutorialSceneBuilder para ligar as referencias de UI sem reflection.</summary>
        public void Configure(GameObject panel, Text text)
        {
            panelRoot = panel;
            captionText = text;
        }
    }
}
