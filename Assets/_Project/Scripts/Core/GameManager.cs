using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lumen.Core
{
    public enum GameState
    {
        Boot,
        Menu,      // tela inicial (botao "Jogar")
        Intro,
        Playing,
        Paused,
        Challenge,
        Ended      // missao concluida: tela final
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
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        /// <summary>"Jogar novamente": a cena recarrega e o jogo volta para a tela inicial, zerado.</summary>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Instance != this) return;
            Time.timeScale = 1f;
            CurrentState = GameState.Menu;
            HasRescuePoint = false;
            RescueLabel = "a base";
            EventBus.ResetSession();
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
