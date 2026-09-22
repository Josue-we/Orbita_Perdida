using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Lumen.Core;
using Lumen.Data;

namespace Lumen.Gameplay.Challenges
{
    /// <summary>
    /// Desafio de quiz: mostra a pergunta e as alternativas de PhaseData.Quiz,
    /// e so chama onCompleted quando o jogador acerta (pode tentar de novo em
    /// caso de erro, sem penalidade). Esta e a MESMA classe usada por Netuno,
    /// Urano e Marte - a unica coisa que muda entre eles e o QuizQuestionData
    /// atribuido ao PhaseData de cada um.
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

        public void StartChallenge(PhaseData phase, Action onCompleted)
        {
            _data = phase.Quiz;
            _onCompleted = onCompleted;

            if (_data == null)
            {
                Debug.LogWarning($"[LUMEN] {phase.PlanetName} nao tem QuizQuestionData atribuido em PhaseData.");
                onCompleted?.Invoke();
                return;
            }

            panelRoot.SetActive(true);
            questionText.text = _data.Question;
            feedbackText.text = "";
            SetupOptions();
        }

        private void SetupOptions()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool hasOption = i < _data.Options.Length;
                optionButtons[i].gameObject.SetActive(hasOption);
                if (!hasOption) continue;

                optionLabels[i].text = _data.Options[i];

                int index = i; // captura local - evita o bug classico de closure em loop
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionClicked(index));
                optionButtons[i].interactable = true;
            }
        }

        private void OnOptionClicked(int index)
        {
            EventBus.RaiseOptionSelected();

            if (index == _data.CorrectIndex)
            {
                feedbackText.text = _data.CorrectFeedback;
                EventBus.RaiseAnswerCorrect();
                foreach (var btn in optionButtons) btn.interactable = false;
                StartCoroutine(FinishAfterDelay(1.5f));
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
