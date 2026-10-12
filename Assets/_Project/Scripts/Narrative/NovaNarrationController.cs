using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Lumen.Core;

namespace Lumen.Narrative
{
    /// <summary>
    /// Mostra falas da NOVA (as curiosidades de cada planeta) como legenda na
    /// parte de baixo da tela, sem cobrir o jogo (diferente do
    /// IntroSequenceController, que e tela cheia).
    /// Chame Play(linhas, callback) quando o LUMEN se aproxima de um planeta.
    /// </summary>
    public class NovaNarrationController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text captionText;
        [SerializeField] private float secondsPerLine = 3.5f;

        private float _fitHeight; // altura (unidades de canvas) que o texto das falas precisa

        private void Awake()
        {
            UiTextTuning.Apply(captionText, GameTuning.NovaCaptionFontSize);
            LayoutPanel(GameTuning.NovaPanelPadding * 2f + GameTuning.NovaCaptionFontSize * 1.3f);
        }

        /// <summary>
        /// Coloca a caixa centralizada, acima do objetivo do HUD, com a altura EXATA do texto
        /// (mais um respiro) - nunca maior que NovaPanelMaxHeight. Assim a faixa preta nao
        /// cobre o resto da interface.
        /// </summary>
        private void LayoutPanel(float heightUnits)
        {
            var rt = panelRoot != null ? panelRoot.transform as RectTransform : null;
            if (rt == null) return;

            float halfW = GameTuning.NovaPanelWidth * 0.5f;
            rt.anchorMin = new Vector2(GameTuning.NovaPanelCenterX - halfW, GameTuning.NovaPanelBottom);
            rt.anchorMax = new Vector2(GameTuning.NovaPanelCenterX + halfW, GameTuning.NovaPanelBottom);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;

            float maxHeight = GameTuning.NovaPanelMaxHeight * GameTuning.UiReferenceResolution.y;
            rt.sizeDelta = new Vector2(0f, Mathf.Min(heightUnits, maxHeight));

            if (captionText != null)
            {
                var textRt = captionText.rectTransform;
                float pad = GameTuning.NovaPanelPadding;
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(pad, pad * 0.5f);
                textRt.offsetMax = new Vector2(-pad, -pad * 0.5f);
            }
        }

        /// <summary>Altura que a frase ocupa na largura atual da caixa (sem encolher a fonte).</summary>
        private float MeasureHeight(string line)
        {
            if (captionText == null) return 0f;

            float width = GameTuning.NovaPanelWidth * GameTuning.UiReferenceResolution.x - GameTuning.NovaPanelPadding * 2f;
            var settings = captionText.GetGenerationSettings(new Vector2(width, 0f));
            settings.resizeTextForBestFit = false;
            float h = captionText.cachedTextGeneratorForLayout.GetPreferredHeight(line, settings) / captionText.pixelsPerUnit;
            return h;
        }

        public void Play(string[] lines, Action onFinished)
        {
            StartCoroutine(RunSequence(lines, onFinished));
        }

        private IEnumerator RunSequence(string[] lines, Action onFinished)
        {
            // Altura da caixa = a da MAIOR fala (nao pula de tamanho de uma fala para outra).
            float tallest = 0f;
            foreach (string line in lines) tallest = Mathf.Max(tallest, MeasureHeight(line));
            LayoutPanel(tallest + GameTuning.NovaPanelPadding * 2f);

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
