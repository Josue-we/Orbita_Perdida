using UnityEngine;

namespace Lumen.Core
{
    public enum GameState
    {
        Boot,
        Menu,      // tela inicial (botao "Jogar")
        Intro,
        Playing,
        Paused,
        Challenge
    }

    /// <summary>
    /// Estado macro do jogo. O jogo comeca em Menu (tela inicial), vai para Intro
    /// quando o jogador clica em "Jogar" e so passa para Playing quando o
    /// IntroSequenceController termina de mostrar o texto.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameState CurrentState { get; private set; } = GameState.Boot;

        /// <summary>
        /// Ultimo ponto seguro do LUMEN: o planeta cujo quiz ja foi respondido
        /// (ou o inicio da rota). Se o combustivel zerar no meio de um trecho,
        /// e para onde o resgate traz a nave de volta.
        /// </summary>
        public Vector3 RescuePosition { get; private set; }
        public string RescueLabel { get; private set; } = "a base";
        public bool HasRescuePoint { get; private set; }

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
            CurrentState = GameState.Menu; // espera o botao "Jogar"
        }

        public void SetState(GameState newState)
        {
            CurrentState = newState;
        }

        public void SetRescuePoint(Vector3 position, string label)
        {
            RescuePosition = position;
            RescueLabel = label;
            HasRescuePoint = true;
        }
    }
}
