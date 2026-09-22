using System;

namespace Lumen.Core
{
    /// <summary>
    /// Canal central de eventos do jogo. Sistemas publicam e assinam eventos aqui
    /// em vez de se referenciarem diretamente — por exemplo, o LUMEN não precisa
    /// conhecer o HUD para atualizar a barra de energia.
    /// </summary>
    public static class EventBus
    {
        public static event Action<float, float> OnEnergyChanged; // (atual, maximo)
        public static event Action OnTutorialCompleted;
        public static event Action OnOptionSelected;
        public static event Action OnAnswerCorrect;
        public static event Action OnAnswerIncorrect;
        public static event Action<string> OnFragmentCollected; // nome do planeta

        /// <summary>Verdadeiro depois que o tutorial terminou (pela porta ou ao chegar no 1o planeta).</summary>
        public static bool TutorialCompleted { get; private set; }

        public static void RaiseEnergyChanged(float current, float max)
        {
            OnEnergyChanged?.Invoke(current, max);
        }

        /// <summary>Idempotente: so dispara uma vez em toda a partida.</summary>
        public static void RaiseTutorialCompleted()
        {
            if (TutorialCompleted) return;
            TutorialCompleted = true;
            OnTutorialCompleted?.Invoke();
        }

        public static void RaiseOptionSelected()
        {
            OnOptionSelected?.Invoke();
        }

        public static void RaiseAnswerCorrect()
        {
            OnAnswerCorrect?.Invoke();
        }

        public static void RaiseAnswerIncorrect()
        {
            OnAnswerIncorrect?.Invoke();
        }

        public static void RaiseFragmentCollected(string planetName)
        {
            OnFragmentCollected?.Invoke(planetName);
        }
    }
}
