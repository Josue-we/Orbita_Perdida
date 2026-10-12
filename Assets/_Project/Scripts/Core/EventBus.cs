using System;
using UnityEngine;

namespace Lumen.Core
{
    /// <summary>Estado do propulsor da nave, usado pelo audio (ronco normal x turbo).</summary>
    public enum ShipThrustState
    {
        Idle,       // parada, freando, travada (menu/quiz/pausa) ou sem combustivel
        Thrusting,  // propulsores acesos
        Boosting    // propulsores acesos com Shift (turbo)
    }

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
        public static event Action<Transform, string> OnNavigateTo; // (proximo alvo, nome)
        public static event Action OnMissionComplete;
        public static event Action<string> OnRescued; // local para onde a nave voltou
        public static event Action OnQuizOpened;
        public static event Action OnQuizClosed;
        public static event Action OnGameStarted; // jogador clicou "Jogar" no menu inicial
        public static event Action<ShipThrustState> OnShipThrustChanged; // so dispara quando muda

        /// <summary>Verdadeiro depois que o tutorial terminou (pela porta ou ao chegar no 1o planeta).</summary>
        public static bool TutorialCompleted { get; private set; }

        /// <summary>
        /// Proximo passo da rota que o jogador pode resolver (0 = Netuno ... ultimo = Terra).
        /// Planetas fora de ordem ficam bloqueados: nao da para pular direto para a Terra.
        /// </summary>
        public static int RouteProgress { get; private set; }

        public static void AdvanceRoute(int resolvedIndex)
        {
            if (resolvedIndex >= RouteProgress) RouteProgress = resolvedIndex + 1;
        }

        /// <summary>Zera o estado da partida (usado ao reiniciar o jogo pela tela final).</summary>
        public static void ResetSession()
        {
            TutorialCompleted = false;
            RouteProgress = 0;
        }

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

        public static void RaiseNavigateTo(Transform target, string displayName)
        {
            OnNavigateTo?.Invoke(target, displayName);
        }

        public static void RaiseMissionComplete()
        {
            OnMissionComplete?.Invoke();
        }

        public static void RaiseRescued(string place)
        {
            OnRescued?.Invoke(place);
        }

        public static void RaiseQuizOpened()
        {
            OnQuizOpened?.Invoke();
        }

        public static void RaiseQuizClosed()
        {
            OnQuizClosed?.Invoke();
        }

        public static void RaiseGameStarted()
        {
            OnGameStarted?.Invoke();
        }

        public static void RaiseShipThrustChanged(ShipThrustState state)
        {
            OnShipThrustChanged?.Invoke(state);
        }
    }
}
