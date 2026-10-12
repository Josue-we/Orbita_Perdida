using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Lumen.Core;
using Lumen.Data;

namespace Lumen.Gameplay.Challenges
{
    /// <summary>
    /// Desafio de quiz: sorteia uma pergunta do pool do planeta (PhaseData.PickQuiz),
    /// embaralha a ordem das alternativas e so chama onCompleted quando o jogador
    /// acerta (pode tentar de novo em caso de erro, sem penalidade). E a MESMA
    /// classe usada por todos os planetas - o que muda e o pool de cada PhaseData.
    /// </summary>
    public class QuizChallengeController : MonoBehaviour, IPhaseChallenge
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text questionText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private Text[] optionLabels;

        private QuizQuestionData _data;
        private Action _onCompleted;

        // Alternativas na ordem EMBARALHADA desta rodada (o asset guarda a certa
        // sempre na posicao CorrectIndex; aqui ela muda de lugar a cada vez).
        private string[] _options;
        private int _correctIndex;

        private void Awake()
        {
            ApplyTextTuning();
        }

        /// <summary>Fontes e caixa maiores, aplicadas em runtime (valem em cena ja montada).</summary>
        private void ApplyTextTuning()
        {
            if (panelRoot != null)
                UiTextTuning.SetAnchors(panelRoot.transform as RectTransform,
                    GameTuning.QuizPanelAnchorMin, GameTuning.QuizPanelAnchorMax);

            // Layout interno maior (pergunta no topo, 3 alternativas altas, feedback embaixo).
            if (questionText != null)
                UiTextTuning.SetAnchors(questionText.rectTransform, GameTuning.QuizQuestionMin, GameTuning.QuizQuestionMax);
            if (feedbackText != null)
                UiTextTuning.SetAnchors(feedbackText.rectTransform, GameTuning.QuizFeedbackMin, GameTuning.QuizFeedbackMax);

            if (optionButtons != null)
            {
                for (int i = 0; i < optionButtons.Length && i < GameTuning.QuizOptionTops.Length; i++)
                {
                    if (optionButtons[i] == null) continue;
                    float top = GameTuning.QuizOptionTops[i];
                    UiTextTuning.SetAnchors(optionButtons[i].transform as RectTransform,
                        new Vector2(0.06f, top - GameTuning.QuizOptionHeight), new Vector2(0.94f, top));
                }
            }

            UiTextTuning.Apply(questionText, GameTuning.QuizQuestionFontSize);
            UiTextTuning.Apply(feedbackText, GameTuning.QuizFeedbackFontSize);

            if (optionLabels == null) return;
            foreach (var label in optionLabels)
                UiTextTuning.Apply(label, GameTuning.QuizOptionFontSize);
        }

        public void StartChallenge(PhaseData phase, Action onCompleted)
        {
            _data = phase.PickQuiz();
            _onCompleted = onCompleted;

            if (_data == null || _data.Options == null || _data.Options.Length == 0)
            {
                Debug.LogWarning($"[LUMEN] {phase.PlanetName} não tem pergunta válida (Quiz Pool/Quiz) em PhaseData.");
                onCompleted?.Invoke();
                return;
            }

            ShuffleOptions();

            panelRoot.SetActive(true);
            questionText.text = _data.Question;
            feedbackText.text = "";
            SetupOptions();

            // Quiz em tela: a trilha sonora abaixa (e volta ao fechar).
            EventBus.RaiseQuizOpened();
        }

        /// <summary>Embaralha as alternativas (Fisher-Yates) e descobre onde a certa foi parar.</summary>
        private void ShuffleOptions()
        {
            int n = _data.Options.Length;
            var order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;

            for (int i = n - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int tmp = order[i];
                order[i] = order[j];
                order[j] = tmp;
            }

            _options = new string[n];
            _correctIndex = 0;
            for (int i = 0; i < n; i++)
            {
                _options[i] = _data.Options[order[i]];
                if (order[i] == _data.CorrectIndex) _correctIndex = i;
            }
        }

        private void SetupOptions()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool hasOption = i < _options.Length;
                optionButtons[i].gameObject.SetActive(hasOption);
                if (!hasOption) continue;

                optionLabels[i].text = _options[i];

                int index = i; // captura local - evita o bug classico de closure em loop
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionClicked(index));
                optionButtons[i].interactable = true;
            }
        }

        private void OnOptionClicked(int index)
        {
            EventBus.RaiseOptionSelected();

            if (index == _correctIndex)
            {
                feedbackText.text = _data.CorrectFeedback;
                EventBus.RaiseAnswerCorrect();
                foreach (var btn in optionButtons) btn.interactable = false;
                StartCoroutine(FinishAfterDelay(2.5f)); // um pouco mais de tempo para ler a curiosidade maior
            }
            else
            {
                feedbackText.text = _data.IncorrectFeedback;
                EventBus.RaiseAnswerIncorrect();
            }
        }

        private IEnumerator FinishAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            panelRoot.SetActive(false);

            // Tela do quiz fechada: a trilha volta ao volume que tinha antes.
            EventBus.RaiseQuizClosed();

            _onCompleted?.Invoke();
        }

        /// <summary>Usado pelo TutorialSceneBuilder para ligar as referencias de UI sem reflection.</summary>
        public void Configure(GameObject panel, Text question, Text feedback, Button[] buttons, Text[] labels)
        {
            panelRoot = panel;
            questionText = question;
            feedbackText = feedback;
            optionButtons = buttons;
            optionLabels = labels;
        }
    }
}
