using UnityEngine;

namespace Lumen.Core
{
    public enum GameState
    {
        Boot,
        Intro,
        Playing,
        Paused,
        Challenge
    }

    /// <summary>
    /// Estado macro do jogo. O jogo comeca em Intro (abertura da historia,
    /// controle travado) e so passa para Playing quando o IntroSequenceController
    /// termina de mostrar o texto. O PhaseManager orientado a dados entra
    /// quando implementarmos a Fase 2 (Netuno) em diante.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameState CurrentState { get; private set; } = GameState.Boot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            CurrentState = GameState.Intro;
        }

        public void SetState(GameState newState)
        {
            CurrentState = newState;
        }
    }
}
