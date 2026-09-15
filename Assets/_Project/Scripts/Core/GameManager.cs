using UnityEngine;

namespace Lumen.Core
{
    public enum GameState
    {
        Boot,
        Playing,
        Paused
    }

    /// <summary>
    /// Estado macro do jogo. Nesta primeira versão é propositalmente minimalista
    /// (só liga o jogo para "Playing"). O PhaseManager orientado a dados
    /// (PhaseData / ScriptableObjects) entra quando implementarmos a Fase 2 (Netuno)
    /// em diante — não é necessário para o Tutorial, que não tem progressão de fases.
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
            CurrentState = GameState.Playing;
        }
    }
}
